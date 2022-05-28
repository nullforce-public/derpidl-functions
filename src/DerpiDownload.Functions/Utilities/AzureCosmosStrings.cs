namespace DerpiDownload.Functions.Utilities;

public class AzureCosmosStrings
{
    private static readonly string[] InvalidParts =
    {
        "/",
        "\\",
        "?",
        "%",
        "#",
    };

    /// <summary>
    /// Strips invalid characters from a key string
    /// </summary>
    /// <param name="unsanitizedKey">The unsanitized string</param>
    /// <returns>The sanitized string</returns>
    public static string SanitizeKeyName(string unsanitizedKey)
    {
        var result = unsanitizedKey;

        foreach (var part in InvalidParts)
        {
            result = result.Replace(part, string.Empty);
        }

        return result;
    }
}
