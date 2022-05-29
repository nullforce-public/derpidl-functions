using DerpiDownload.Functions.Tables;
using DerpiDownload.Functions.Utilities;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DerpiDownload.Functions.Repositories;

public class ImageRepository
{
    private CloudTable _table;

    public ImageRepository(string connectionString)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        _table = tableClient.GetTableReference("SeenImages");
    }

    public async Task<TableResult> AddSeenImage(string siteCode, string tagName, int id)
    {
        return await AddSeenImages(siteCode, tagName, new int[] { id });
    }

    public async Task<TableResult> AddSeenImages(string siteCode, string tagName, IEnumerable<int> seenImages)
    {
        var seen = GetSeenImages(siteCode, tagName).FirstOrDefault();

        if (seen != null)
        {
            seen.SeenImages.UnionWith(seenImages);
        }
        else
        {
            seen = new SeenImagesEntity(siteCode, tagName, seenImages);
        }

        var op = TableOperation.InsertOrMerge(seen);
        return await _table.ExecuteAsync(op);
    }

    public IEnumerable<SeenImagesEntity> GetSeenImages(string siteCode, string tagName)
    {
        var sanitizedName = AzureCosmosStrings.SanitizeKeyName(tagName).ToLower();
        var condition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, siteCode);
        var condition2 = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sanitizedName);
        var q = TableQuery.CombineFilters(condition, TableOperators.And, condition2);
        var query = new TableQuery<SeenImagesEntity>().Where(q);

        return _table.ExecuteQuery(query);
    }

    public async Task<TableResult> InsertOrMerge(SeenImagesEntity seenImages)
    {
        var op = TableOperation.InsertOrMerge(seenImages);
        return await _table.ExecuteAsync(op);
    }
}
