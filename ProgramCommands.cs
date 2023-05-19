using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YellowDogSoftware.NewDev.Commands;

namespace YellowDogSoftware.NewDev;

public class ProgramCommands
{
    private readonly IReadOnlyList<ICommand> _commands;

    private readonly Dictionary<string, Tuple<ICommand, MethodInfo>> _commandsByName;

    public ProgramCommands()
    {
        var commands = new List<ICommand>();

        //Use reflection to find types in the current assembly that implement the ICommand interface
        foreach ( var type in Assembly.GetExecutingAssembly().GetTypes() )
        {
            if ( !type.IsAbstract && type.IsAssignableTo(typeof(ICommand)) )
            {
                commands.Add(Activator.CreateInstance(type) as ICommand ??
                             throw new Exception(
                                 $"Failed to create ICommand instance with implementation type {type.FullName}."));
            }
        }

        _commands = commands.AsReadOnly();

        //Find the perform method for each command and store it in a dictionary associated with the command's name
        _commandsByName = _commands.ToDictionary(c => c.Name, c =>
        {
            var methods = c.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

            var perform = methods.FirstOrDefault(m => m.Name == "Perform" || m.Name == "PerformAsync") ??
                          throw new Exception($"No \"Perform\" method found for command {c.Name}.");

            return new Tuple<ICommand, MethodInfo>(c, perform);
        });
    }

    public IReadOnlyList<ICommand> All => _commands;

    public async Task PerformAsync(string name, IServiceProvider services)
    {
        var (command, method) = _commandsByName[name];

        using var cts = new CancellationTokenSource();

        //If the command's perform method has parameters, use dependency injection to create an array of arguments to pass to it
        var parameters = method.GetParameters();

        var arguments = new object?[parameters.Length];

        for ( var i = 0; i < arguments.Length; ++i )
        {
            var parameter = parameters[i];

            if ( parameter.ParameterType == typeof(CancellationToken) )
            {
                arguments[i] = cts.Token;
            }

            else
            {
                arguments[i] = services.GetRequiredService(parameter.ParameterType);
            }
        }

        //Invoke the command's perform method
        var result = method.Invoke(command, arguments);

        if ( result is Task task )
        {
            task = task.ContinueWith(t =>
            {
                if ( t.Exception != null )
                {
                    Console.WriteLine();
                    
                    services.GetRequiredService<ILogger<ProgramCommands>>().LogError(t.Exception, "An error occurred performing command \"{Command}\"", name);
                }
                
                Console.WriteLine("Press any key to continue...");
            }, CancellationToken.None);

            while ( !task.IsCompleted )
            {
                var key = Console.ReadKey(true);

                if ( key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.C )
                {
                    cts.Cancel();

                    break;
                }
            }

            await task;
        }
    }
}