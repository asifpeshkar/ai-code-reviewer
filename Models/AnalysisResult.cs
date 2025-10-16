using System.Collections.Generic;

namespace AICodeReviewer.Models;

/// <summary>
/// High-level result of analyzing a code snippet.
/// </summary>
public class AnalysisResult
{
    /// <summary>Short human-readable summary.</summary>
    public string? Summary { get; set; }

    /// <summary>List of issues found.</summary>
    public List<Issue> Issues { get; set; } = new();
}
