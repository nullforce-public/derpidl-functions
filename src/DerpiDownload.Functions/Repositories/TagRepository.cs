using DerpiDownload.Functions.Tables;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DerpiDownload.Functions.Repositories;

public class TagRepository
{
    private CloudTable _table;

    public TagRepository(string connectionString)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        _table = tableClient.GetTableReference("FollowedTags");
    }

    public async Task<TableResult> AddTag(string siteCode, string tagName)
    {
        var tag = new TagEntity(siteCode, tagName);
        var op = TableOperation.Insert(tag);
        return await _table.ExecuteAsync(op);
    }

    public async Task<TableResult> InsertOrMerge(TagEntity tag)
    {
        var op = TableOperation.InsertOrMerge(tag);
        return await _table.ExecuteAsync(op);
    }

    public async Task DeleteTag(string siteCode, string tagName)
    {
        var tag = new TagEntity(siteCode, tagName)
        {
            ETag = "*",
        };
        var op = TableOperation.Delete(tag);
        await _table.ExecuteAsync(op);
    }

    public TagEntity GetTag(string siteCode, string tagName)
    {
        var partitionKeyCondition = TableQuery.GenerateFilterCondition(
            "PartitionKey",
            QueryComparisons.Equal,
            siteCode);
        var rowKeyCondition = TableQuery.GenerateFilterCondition(
            "RowKey",
            QueryComparisons.Equal,
            tagName);
        var conditions = TableQuery.CombineFilters(
            partitionKeyCondition,
            TableOperators.And,
            rowKeyCondition);
        var query = new TableQuery<TagEntity>().Where(conditions);
        return _table.ExecuteQuery(query).FirstOrDefault();
    }

    public IEnumerable<TagEntity> GetTags(string siteCode)
    {
        var condition = TableQuery.GenerateFilterCondition(
            "PartitionKey",
            QueryComparisons.Equal,
            siteCode);
        var query = new TableQuery<TagEntity>().Where(condition);
        return _table.ExecuteQuery(query);
    }
}
