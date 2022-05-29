using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using DerpiDownload.Functions.Messages;
using DerpiDownload.Functions.Repositories;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nullforce.Api.Derpibooru.JsonModels;
using Polly;

namespace DerpiDownload.Functions;

public class ScheduledTagsQueueReader
{
    private readonly ILogger _logger;

    public ScheduledTagsQueueReader(ILogger<ScheduledTagsQueueReader> logger)
    {
        _logger = logger;
    }

    [FunctionName("ScheduledTagsQueueReader")]
    public async Task Run([QueueTrigger("scheduled-tags", Connection = "StorageConnectionString")] ScheduledTagMessage myQueueItem)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

        if (string.IsNullOrWhiteSpace(myQueueItem.PartitionKey)
            || string.IsNullOrWhiteSpace(myQueueItem.RowKey))
        {
            return;
        }

        // Get configuration
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
        var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";

        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables();

        if (isDevelopment)
        {
            builder.AddUserSecrets<AppSettings>();
        }

        var configRoot = builder.Build();
        var appSettings = configRoot.Get<AppSettings>();

        // Define Polly policies
        var longRetryPolicy = Policy
            .Handle<FlurlHttpException>(ex => ex.Call?.Response.StatusCode == 429)
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5) });

        var shortRetryPolicy = Policy
            .Handle<FlurlHttpException>(IsWorthRetrying)
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) });

        var retryPolicy = Policy
            .WrapAsync(longRetryPolicy, shortRetryPolicy);

        const string QueueName = "image-downloads";
        var storageConnectionString = appSettings.StorageConnectionString;

        // Get the site info
        var siteRepo = new SiteRepository(storageConnectionString);
        var site = siteRepo.GetSite(myQueueItem.PartitionKey);

        if (site == null) return;

        // Get the tag record
        var tagRepo = new TagRepository(storageConnectionString);
        var tag = tagRepo
            .GetTag(site.RowKey, myQueueItem.RowKey);

        if (tag == null) return;

        // Get a list of seen images
        var imageRepo = new ImageRepository(appSettings.StorageConnectionString);

        // Queue
        var queue = new QueueClient(appSettings.StorageConnectionString, QueueName);

        if (string.IsNullOrEmpty(site.BaseApiUrl)) return;

        string baseUri = site.BaseApiUrl;

        FlurlHttp.ConfigureClient(baseUri, c => c
            .WithHeaders(new
            {
                Accept = "application/json",
                User_Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.75 Safari/537.36"
            }));

        // TODO: tb uses old api https://twibooru.org/search.json?q=first_seen_at.gt%3A3+days+ago&sd=desc&sf=score
        var searchUri = baseUri
            .AppendPathSegment("/search/images")
            .SetQueryParam("filter_id", site.EverythingFilter)
            .SetQueryParam("sf", "id")
            .SetQueryParam("sd", "desc")
            .SetQueryParam("per_page", "50");

        var page = 1;
        var total = 0;
        searchUri = searchUri.SetQueryParam("q", tag.TagName);
        var seenImages = imageRepo.GetSeenImages(site.SiteCode, tag.TagName).Select(i => i.SeenImages)?.FirstOrDefault() ?? new HashSet<int>();
        var maxId = seenImages.Count > 0 ? seenImages.Max() : 0;
        var done = false;

        while (!done)
        {
            try
            {
                searchUri = searchUri.SetQueryParam("page", page);

                _logger.LogInformation("search: {searchUri}", searchUri);

                var json = await retryPolicy.ExecuteAsync(async () => await searchUri.GetStringAsync());
                var searchResults = JsonSerializer.Deserialize<ImageSearchRootJson>(json);
                total = searchResults.Total;

                // Skip an artist if the total queried images equals that of the previous run
                if (tag.ImageCount == total)
                {
                    break;
                }

                var imagesToDownload = searchResults.Images.ToList();

                if (imagesToDownload.Count == 0)
                {
                    break;
                }

                foreach (var image in imagesToDownload)
                {
                    if (image.Id <= maxId)
                    {
                        // We've processed all new images
                        done = true;
                        break;
                    }

                    // Only queue images we haven't already seen/downloaded
                    if (!seenImages.Contains(image.Id))
                    {
                        var filename = Path.GetFileName(image.ViewUri);

                        var message = new ImageDownloadMessage
                        {
                            SiteCode = site.SiteCode,
                            Id = image.Id,
                            TagName = tag.TagName,
                            TagFolderName = tag.TagFolderName,
                            ImageName = filename,
                            ImageUri = image.Representations.Full,
                        };

                        var jsonMessage = JsonSerializer.Serialize(message);

                        await queue.SendMessageAsync(jsonMessage);
                    }
                }

                page++;
            }
            catch (FlurlHttpTimeoutException)
            {
            }
        }

        // Update ArtistEntity
        tag.ImageCount = total;
        tag.MarkAsQueued();
        await tagRepo.InsertOrMerge(tag);

        //var page = 1;
        //var total = 0;
        //searchUri = searchUri.SetQueryParam("q", tag.TagName);

        //var uristring = searchUri.ToString();

        //var json = await searchUri.GetStringAsync();
        //var searchResults = JsonSerializer.Deserialize<ImageSearchRootJson>(json);
        //total = searchResults.Total;

        //var imagesToDownload = searchResults.Images.ToList();

        //foreach (var image in imagesToDownload)
        //{
        //    _logger.LogInformation("{id}", image.Id);
        //}
    }

    private static bool IsWorthRetrying(FlurlHttpException ex)
    {
        if (ex.Call?.Response == null)
        {
            // Call timed out
            return true;
        }

        switch (ex.Call?.Response.StatusCode)
        {
            case 408 or >= 500:
                return true;

            default:
                return false;
        }
    }
}
