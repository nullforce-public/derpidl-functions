namespace DerpiDownload.Functions.Utilities;

public class FileStrings
{
    private static readonly string[] InvalidParts =
    {
        "\\",
        "/",
        ":",
        "*",
        "?",
        "\"",
        "<",
        ">",
        "|",
    };

    /// <summary>
    /// Strips invalid characters from a file string
    /// </summary>
    /// <param name="unsanitizedPart">The unsanitized string</param>
    /// <returns>The sanitized string</returns>
    public static string SanitizeFileParts(string unsanitizedPart)
    {
        var result = unsanitizedPart;

        foreach (var part in InvalidParts)
        {
            result = result.Replace(part, string.Empty);
        }

        return result;
    }
}
