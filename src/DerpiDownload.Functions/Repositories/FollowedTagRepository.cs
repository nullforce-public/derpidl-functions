using DerpiDownload.Functions.Tables;
using DerpiDownload.Functions.Utilities;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DerpiDownload.Functions.Repositories;

public class FollowedTagRepository
{
    private CloudTable _table;

    public FollowedTagRepository(string connectionString)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        _table = tableClient.GetTableReference("FollowedTags");
    }

    public async Task<TableResult> AddTag(string siteCode, string tagName)
    {
        var tag = new FollowedTagEntity(siteCode, tagName);
        var op = TableOperation.Insert(tag);
        return await _table.ExecuteAsync(op);
    }

    public async Task<TableResult> InsertOrMerge(FollowedTagEntity tag)
    {
        var op = TableOperation.InsertOrMerge(tag);
        return await _table.ExecuteAsync(op);
    }

    public async Task DeleteTag(string siteCode, string tagName)
    {
        var tag = new FollowedTagEntity(siteCode, tagName)
        {
            ETag = "*",
        };
        var op = TableOperation.Delete(tag);
        await _table.ExecuteAsync(op);
    }

    public IEnumerable<FollowedTagEntity> GetTags(string siteCode)
    {
        var condition = TableQuery.GenerateFilterCondition(
            "PartitionKey",
            QueryComparisons.Equal,
            AzureCosmosStrings.SanitizeKeyName(siteCode));
        var query = new TableQuery<FollowedTagEntity>().Where(condition);
        return _table.ExecuteQuery(query);
    }
}
