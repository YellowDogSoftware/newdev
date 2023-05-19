using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharprompt;
using YellowDogSoftware.NewDev;
using YellowDogSoftware.NewDev.Data;
using YellowDogSoftware.NewDev.Services;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(options =>
{
    options.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);

    options.AddConsole(o => o.FormatterName = BasicConsoleFormatter.FormatterName)
        .AddConsoleFormatter<BasicConsoleFormatter, BasicConsoleFormatterOptions>();
});

serviceCollection.AddDbContext<AppDb>(options =>
{
    options.UseSqlite(configuration.GetConnectionString("AppDb") ?? "Data Source=app.db")
        .UseSnakeCaseNamingConvention();
});

serviceCollection.Configure<AdequateShopApiClientOptions>(o => o.ApiUrl = configuration["ApiUrl"] ?? o.ApiUrl);

serviceCollection.AddSingleton<AdequateShopApiClient>();

var services = serviceCollection.BuildServiceProvider();

{
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDb>();

    await db.Database.EnsureCreatedAsync();
}

var commands = new ProgramCommands();

var commandNames = commands.All
    .Select(c => c.Name)
    .ToArray();

var logger = services.GetRequiredService<ILogger<Program>>();

var passwordValidators = new[] { Validators.MinLength(8), Validators.MaxLength(32) };

var client = services.GetRequiredService<AdequateShopApiClient>();

Prompt.ThrowExceptionOnCancel = true;

while ( true )
{
    var exitOnCancel = true;

    try
    {
        if ( Prompt.Select("Login or Register (CTRL+C to quit)", new[] { "Log In", "Register" }) == "Log In" )
        {
            var email = Prompt.Input<string>("Email");

            exitOnCancel = false;

            var password = Prompt.Password("Password", validators: passwordValidators);

            logger.LogInformation("Logging in as user {Email}...", email);

            try
            {
                await client.LogInAsync(email, password);
            }

            catch ( Exception exception )
            {
                logger.LogError(exception, "Failed to sign in as user {Email}", email);

                continue;
            }

            logger.LogInformation("Logged in as user {Email}", email);
        }

        else
        {
            var name = Prompt.Input<string>("Name");

            exitOnCancel = false;

            var email = Prompt.Input<string>("Email");

            var password = Prompt.Password("Password", validators: passwordValidators);

            ValidationResult? PasswordMatches(object p)
            {
                if ( p is not string pw || pw != password )
                {
                    return new ValidationResult("Passwords do not match.");
                }

                return ValidationResult.Success;
            }

            var confirmPassword = Prompt.Password("Confirm Password",
                validators: passwordValidators.Concat(new[] { PasswordMatches }).ToArray());

            logger.LogInformation("Registering user {Email}...", email);

            try
            {
                await client.RegisterAsync(name, email, password);
            }

            catch ( Exception exception )
            {
                logger.LogError(exception, "Failed to register user {Email}", email);

                continue;
            }

            //The auth token returned by the registration POST does not work for authorization, so log in now
            try
            {
                await client.LogInAsync(email, password);
            }

            catch ( Exception exception )
            {
                logger.LogError(exception, "Failed to log in newly registered user {Email}", email);

                continue;
            }

            logger.LogInformation("Logged in as newly registered user {Email}", email);
        }
    }

    catch ( PromptCanceledException )
    {
        if ( exitOnCancel )
        {
            return 0;
        }

        continue;
    }

    catch ( Exception exception )
    {
        logger.LogError(exception, "An error occurred, please try again");

        continue;
    }

    while ( true )
    {
        string answer;

        try
        {
            answer = Prompt.Select("Select command (CTRL+C to go back)", commandNames);
        }

        catch ( PromptCanceledException )
        {
            break;
        }

        catch ( Exception exception )
        {
            logger.LogError(exception, "An error occurred, please try again");

            continue;
        }

        using var scope = services.CreateScope();

        try
        {
            await commands.PerformAsync(answer, scope.ServiceProvider);
        }

        catch ( Exception exception )
        {
            logger.LogError(exception, "An error occurred performing command {Command}", answer);
        }
    }
}