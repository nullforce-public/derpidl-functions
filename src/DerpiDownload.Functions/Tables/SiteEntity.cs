using DerpiDownload.Functions.Utilities;
using Microsoft.Azure.Cosmos.Table;

namespace DerpiDownload.Functions.Tables;

public class SiteEntity : TableEntity
{
    public string BaseApiUrl { get; set; }
    public int EverythingFilter { get; set; }
    public bool IsEnabled { get; set; }
    public string SiteCode => RowKey;
    public string SiteName { get; set; }

    public SiteEntity()
    {
    }

    public SiteEntity(string siteCode, string siteName, string baseApiUrl)
    {
        PartitionKey = "sites";
        RowKey = AzureCosmosStrings.SanitizeKeyName(siteCode);

        BaseApiUrl = baseApiUrl;
        EverythingFilter = 0;
        IsEnabled = true;
        SiteName = siteName;
    }
}
