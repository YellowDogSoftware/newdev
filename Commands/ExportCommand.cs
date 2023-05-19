using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sharprompt;
using YellowDogSoftware.NewDev.Data;

namespace YellowDogSoftware.NewDev.Commands;

public class ExportCommand : ICommand
{
    public string Name => "Export Users";

    public async Task PerformAsync(AppDb db, ILogger<ExportCommand> logger)
    {
        var count = await db.Users.CountAsync();

        if ( count == 0 )
        {
            logger.LogError("There are no users to export. Try synchronizing first.");

            return;
        }

        string filePath;

        try
        {
            filePath = Prompt.Input<string>("Output File Path (CSV)");
        }

        catch ( PromptCanceledException )
        {
            return;
        }

        if ( string.IsNullOrWhiteSpace(Path.GetExtension(filePath)) )
        {
            filePath += ".csv";
        }

        var users = await db.Users
            .OrderBy(u => u.Id)
            .AsNoTracking()
            .ToListAsync();

        using var streamWriter = new StreamWriter(filePath, false);

        using var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

        csv.WriteRecords(users);

        streamWriter.Flush();

        logger.LogInformation("Output {Count} users to {FilePath} in CSV format", users.Count, filePath);
    }
}