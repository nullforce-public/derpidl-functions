using DerpiDownload.Functions.Utilities;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace DerpiDownload.Functions.Tables;

/// <summary>
/// Represents all of the seen images for an artist
/// </summary>
public class SeenImagesEntity : TableEntity
{
    public HashSet<int> SeenImages { get; set; }

    public SeenImagesEntity()
    {
        SeenImages = new();
    }

    public SeenImagesEntity(string siteCode, string artistName, IEnumerable<int> seenImages)
    {
        var sanitizedName = AzureCosmosStrings.SanitizeKeyName(artistName).ToLower();
        PartitionKey = AzureCosmosStrings.SanitizeKeyName(siteCode);
        RowKey = sanitizedName;

        SeenImages = new(seenImages);
    }

    public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        base.ReadEntity(properties, operationContext);

        // Handle SeenImages, SeenImages2, ...
        foreach (var key in properties.Keys)
        {
            if (key.StartsWith("SeenImages") && !string.IsNullOrEmpty(properties[key].StringValue))
            {
                var items = properties[key].StringValue.Split(',').Select(x => int.Parse(x));
                SeenImages.UnionWith(items);
            }
        }
    }

    public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    {
        var properties = base.WriteEntity(operationContext);

        // Handle SeenImages, SeenImages2, ...
        var chunks = SeenImages
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / 3_000)
            .Select(g => g.Select(x => x.item))
            .ToList();

        if (chunks.Count > 0)
        {
            properties["SeenImages"] = new EntityProperty(string.Join(",", chunks[0].ToList()));

            for (int i = 1; i < chunks.Count; i++)
            {
                properties[$"SeenImages{i + 1}"] = new EntityProperty(string.Join(",", chunks[i].ToList()));
            }
        }

        return properties;
    }
}
