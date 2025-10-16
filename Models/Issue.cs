namespace AICodeReviewer.Models;

/// <summary>
/// Represents a single issue found during analysis, with optional suggestion.
/// </summary>
public class Issue
{
    /// <summary>Rule name or type identifier (e.g., CSharp.NestedIfs).</summary>
    public string? Type { get; set; }

    /// <summary>Line number where the issue was detected (1-based).</summary>
    public int? LineNumber { get; set; }

    /// <summary>Primary issue message.</summary>
    public string? Message { get; set; }

    /// <summary>Suggested fix or improvement.</summary>
    public string? Suggestion { get; set; }

    /// <summary>Severity (Info/Warning/Error). Free-form for now.</summary>
    public string Severity { get; set; } = "Info";

    /// <summary>Location hint (e.g., file path, symbol).</summary>
    public string? Location { get; set; }

    // Back-compat fields (not required by current requirements but preserved):
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
