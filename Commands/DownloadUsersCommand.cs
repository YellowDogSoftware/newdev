using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sharprompt;
using YellowDogSoftware.NewDev.Data;
using YellowDogSoftware.NewDev.Models;
using YellowDogSoftware.NewDev.Services;

namespace YellowDogSoftware.NewDev.Commands;

public class DownloadUsersCommand : ICommand
{
    public string Name => "Download Users";

    public async Task PerformAsync(CancellationToken cancellationToken, AdequateShopApiClient client, AppDb db,
        ILogger<DownloadUsersCommand> logger)
    {
        await db.Users.ExecuteDeleteAsync();

        var downloadedPageCount = 0;

        var totalPages = 0;

        var downloadedUserCount = 0;

        var totalUsers = 0;

        var downloadedUsers = new List<User>();

        Console.WriteLine();

        void UpdateProgress()
        {
            var percent = totalPages > 0 ? (double) downloadedPageCount / totalPages * 100.0D : 0.0D;

            var progress = $"{Math.Ceiling(percent)}%";

            var totalPageDisplay = totalPages > 0 ? totalPages.ToString() : "?";

            var totalUserDisplay = totalUsers > 0 ? totalUsers.ToString() : "?";

            Console.Write(
                $"\rDownloaded {downloadedPageCount} / {totalPageDisplay} (Users: {downloadedUserCount} / {totalUserDisplay}; {progress})...");
        }

        async ValueTask DownloadPageAsync(int pageNumber, CancellationToken stoppingToken)
        {
            var page = await client.GetUserPageAsync(pageNumber);

            lock ( downloadedUsers )
            {
                foreach ( var user in page.Data )
                {
                    downloadedUsers.Add(new User(user.Id, user.Name, user.Email)
                    {
                        Location = user.Location,
                        ProfilePicture = user.ProfilePicture,
                        CreatedAt = user.CreatedAt
                    });
                }

                ++downloadedPageCount;

                downloadedUserCount += page.Data.Count;

                totalPages = page.TotalPages;

                totalUsers = page.TotalRecords;

                UpdateProgress();
            }
        }

        async Task StoreDownloadedUsersAsync()
        {
            lock ( downloadedUsers )
            {
                db.Users.AddRange(downloadedUsers);

                downloadedUsers.Clear();
            }

            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Prompt.Input<int>("Concurrency Limit", 10, "10")
        };

        do
        {
            UpdateProgress();

            await DownloadPageAsync(downloadedPageCount + 1, cancellationToken);

            if ( downloadedPageCount >= totalPages )
            {
                break;
            }

            var startPage = downloadedPageCount + 1;

            var pages = Enumerable.Range(startPage,
                Math.Min(Math.Max(100, parallelOptions.MaxDegreeOfParallelism * 4), totalPages - startPage + 1));

            await Parallel.ForEachAsync(pages, parallelOptions, DownloadPageAsync);

            await StoreDownloadedUsersAsync();
        }

        while ( !cancellationToken.IsCancellationRequested && downloadedPageCount < totalPages );

        await StoreDownloadedUsersAsync();

        Console.Write("\r");

        Console.WriteLine();

        if ( cancellationToken.IsCancellationRequested )
        {
            logger.LogError("Synchronization cancelled. Downloaded {Count} users", downloadedPageCount);

            return;
        }

        logger.LogInformation("Downloaded {Count} users", downloadedUserCount);
    }
}