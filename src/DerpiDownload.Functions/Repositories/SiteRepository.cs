using DerpiDownload.Functions.Tables;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DerpiDownload.Functions.Repositories;

public class SiteRepository
{
    private CloudTable _table;

    public SiteRepository(string connectionString)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        _table = tableClient.GetTableReference("Sites");
    }

    public async Task<TableResult> AddSite(string siteCode, string siteName, string baseApiUrl)
    {
        var tag = new SiteEntity(siteCode, siteName, baseApiUrl);
        var op = TableOperation.Insert(tag);
        return await _table.ExecuteAsync(op);
    }

    public async Task<TableResult> InsertOrMerge(SiteEntity site)
    {
        var op = TableOperation.InsertOrMerge(site);
        return await _table.ExecuteAsync(op);
    }

    public SiteEntity GetSite(string siteCode)
    {
        var partitionKeyCondition = TableQuery.GenerateFilterCondition(
            "PartitionKey",
            QueryComparisons.Equal,
            "sites");
        var rowKeyCondition = TableQuery.GenerateFilterCondition(
            "RowKey",
            QueryComparisons.Equal,
            siteCode);
        var conditions = TableQuery.CombineFilters(
            partitionKeyCondition,
            TableOperators.And,
            rowKeyCondition);
        var query = new TableQuery<SiteEntity>().Where(conditions);
        return _table.ExecuteQuery(query).FirstOrDefault();
    }

    public IEnumerable<SiteEntity> GetSites()
    {
        var condition = TableQuery.GenerateFilterCondition(
            "PartitionKey",
            QueryComparisons.Equal,
            "sites");
        var query = new TableQuery<SiteEntity>().Where(condition);
        return _table.ExecuteQuery(query);
    }
}
