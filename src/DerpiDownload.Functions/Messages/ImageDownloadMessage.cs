using System.Text.Json.Serialization;

namespace DerpiDownload.Functions.Messages;

public class ImageDownloadMessage
{
    [JsonPropertyName("sc")]
    public string SiteCode { get; set; }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("tn")]
    public string TagName { get; set; }
    [JsonPropertyName("tfn")]
    public string TagFolderName { get; set; }
    [JsonPropertyName("in")]
    public string ImageName { get; set; }
    [JsonPropertyName("iu")]
    public string ImageUri { get; set; }

    [JsonIgnore]
    public string ImagePathname => $"{TagName}/{ImageName}";
}
