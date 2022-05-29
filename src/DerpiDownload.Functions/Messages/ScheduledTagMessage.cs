namespace DerpiDownload.Functions.Messages;

public class ScheduledTagMessage
{
    public string Version { get; set; } = "v0";
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
}
