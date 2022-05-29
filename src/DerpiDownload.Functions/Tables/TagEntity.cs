using DerpiDownload.Functions.Utilities;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace DerpiDownload.Functions.Tables;

public class TagEntity : TableEntity
{
    public string TagName { get; set; }
    public string TagFolderName { get; set; }
    public bool IsDownloadEnabled { get; set; }
    public DateTime LastDownloadCheck { get; set; }
    public int ImageCount { get; set; }

    public TagEntity()
    {
    }

    public TagEntity(string siteCode, string name)
    {
        PartitionKey = AzureCosmosStrings.SanitizeKeyName(siteCode);
        RowKey = AzureCosmosStrings.SanitizeKeyName(name);

        TagName = name;
        TagFolderName = GetTagFolderName(name);
        ImageCount = 0;
        IsDownloadEnabled = true;
        LastDownloadCheck = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public void MarkAsQueued()
    {
        LastDownloadCheck = DateTime.UtcNow.AddDays(1);
    }

    public void ResetLastQueued()
    {
        LastDownloadCheck = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private string GetTagFolderName(string name)
    {
        var colonIndex = name.IndexOf(':');
        var unprefixedTag = FileStrings.SanitizeFileParts(name.Substring(colonIndex + 1));

        return unprefixedTag;
    }
}
