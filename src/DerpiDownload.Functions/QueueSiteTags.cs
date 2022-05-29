using Azure.Storage.Queues;
using DerpiDownload.Functions.Messages;
using DerpiDownload.Functions.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DerpiDownload.Functions;

public class QueueSiteTags
{
    private readonly ILogger _logger;

    public QueueSiteTags(ILogger<QueueSiteTags> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Queues all followed tags for each site
    /// </summary>
    /// <param name="myTimer"></param>
    [FunctionName("Timer_QueueSiteTags")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"QueueSiteTagsTimer trigger function started at: {DateTime.UtcNow}");

        // Get configuration
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
        var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";

        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables();

        if (isDevelopment)
        {
            // We're running locally, use user secrets
            builder.AddUserSecrets<AppSettings>();
        }

        var configuration = builder.Build();
        var appSettings = configuration.Get<AppSettings>();

        const string QueueName = "scheduled-tags";
        var storageConnectionString = appSettings.StorageConnectionString;

        // Read the sites table
        var siteRepo = new SiteRepository(storageConnectionString);
        var sites = siteRepo
            .GetSites()
            .Where(s => s.IsEnabled)
            .ToList();

        // For each site, get the followed tags with the following conditions:
        // 1. Is download enabled
        // 2. Has sufficient time passed since the last download?
        var tagRepo = new FollowedTagRepository(storageConnectionString);

        foreach (var site in sites)
        {
            var tags = tagRepo
                .GetTags(site.SiteCode)
                .Where(t =>
                    t.IsDownloadEnabled
                    && t.LastDownloadCheck < DateTime.UtcNow)
                .ToList();

            // Queue each tag
            var queue = new QueueClient(
                storageConnectionString,
                QueueName,
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                }
            );

            foreach (var tag in tags)
            {
                var jsonMessage = JsonSerializer.Serialize(new ScheduledTagMessage
                {
                    Version = "v0",
                    PartitionKey = tag.PartitionKey,
                    RowKey = tag.RowKey,
                });

                var result = await queue.SendMessageAsync(jsonMessage);

                if (!result.GetRawResponse().IsError)
                {
                    tag.MarkAsQueued();
                    await tagRepo.InsertOrMerge(tag);
                }
            }
        }

        _logger.LogInformation($"QueueSiteTagsTimer trigger function completed at: {DateTime.UtcNow}");
    }
}
