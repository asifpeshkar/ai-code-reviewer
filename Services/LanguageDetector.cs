using AICodeReviewer.Enums;

namespace AICodeReviewer.Services;

/// <summary>
/// Naive language detector using simple heuristics.
/// </summary>
public class LanguageDetector
{
    /// <summary>
    /// Detects the language of the provided code snippet.
    /// Rules:
    ///  - If contains "using ", "namespace", "public class", or "void" => CSharp
    ///  - Else if contains "Imports ", "Module", "Sub", or "End Sub" => VBNet
    ///  - Else if contains "SELECT ", "INSERT ", "UPDATE ", or "CREATE TABLE" => SQL
    ///  - Else => Unknown
    /// Case-insensitive, simple IndexOf for efficiency.
    /// </summary>
    public DetectedLanguage Detect(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return DetectedLanguage.Unknown;

        static bool Has(string text, string token)
            => text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        // C# checks (first)
        if (Has(code, "using ") || Has(code, "namespace") || Has(code, "public class") || Has(code, "void"))
            return DetectedLanguage.CSharp;

        // VB.NET checks
        if (Has(code, "Imports ") || Has(code, "Module") || Has(code, "Sub") || Has(code, "End Sub"))
            return DetectedLanguage.VBNet;

        // SQL checks
        if (Has(code, "SELECT ") || Has(code, "INSERT ") || Has(code, "UPDATE ") || Has(code, "CREATE TABLE"))
            return DetectedLanguage.Sql;

        return DetectedLanguage.Unknown;
    }

    /// <summary>
    /// Returns the language name string per requirements: "CSharp", "VBNet", "SQL", or "Unknown".
    /// </summary>
    public string DetectName(string code)
    {
        return ToName(Detect(code));
    }

    /// <summary>
    /// Maps DetectedLanguage enum to display name.
    /// </summary>
    public static string ToName(DetectedLanguage lang) => lang switch
    {
        DetectedLanguage.CSharp => "CSharp",
        DetectedLanguage.VBNet => "VBNet",
        DetectedLanguage.Sql => "SQL",
        _ => "Unknown"
    };
}
