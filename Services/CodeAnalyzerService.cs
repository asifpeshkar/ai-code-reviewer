using AICodeReviewer.Enums;
using AICodeReviewer.Models;

namespace AICodeReviewer.Services;

/// <summary>
/// Performs rule-based analysis, naming checks, and code smell detection.
/// Logic is stubbed for now; returns placeholder results.
/// </summary>
public class CodeAnalyzerService
{
    private readonly NameQualityChecker _nameQualityChecker = new();
    private readonly CodeSmellDetector _codeSmellDetector = new();
    private readonly SummaryGenerator _summaryGenerator = new();
    /// <summary>
    /// Rule-based analysis entry accepting language name string.
    /// Returns only the list of issues, per requirement.
    /// Supported names: "CSharp", "VBNet", "SQL" (case-insensitive).
    /// </summary>
    public List<Issue> AnalyzeRules(string code, string languageName)
    {
        var lang = languageName?.Trim().ToUpperInvariant() switch
        {
            "CSHARP" => DetectedLanguage.CSharp,
            "VBNET" => DetectedLanguage.VBNet,
            "SQL" => DetectedLanguage.Sql,
            _ => DetectedLanguage.Unknown
        };

        var result = new AnalysisResult();
        RunRuleBasedAnalysis(code, lang, result);
        return result.Issues;
    }

    /// <summary>
    /// Analyze the provided code for issues.
    /// </summary>
    public AnalysisResult Analyze(string code, DetectedLanguage language)
    {
        var result = new AnalysisResult();

    // Summary first
    var languageName = LanguageDetector.ToName(language);
    result.Summary = _summaryGenerator.Generate(code, languageName);

    // Implement rule-based analysis
        RunRuleBasedAnalysis(code, language, result);

    // Naming checks
        var namingIssues = _nameQualityChecker.Analyze(code, languageName);
        result.Issues.AddRange(namingIssues);

    // Code smell checks
    var smellIssues = _codeSmellDetector.Analyze(code, languageName);
    result.Issues.AddRange(smellIssues);

        return result;
    }

    // --- Rule-based checks ---
    protected virtual void RunRuleBasedAnalysis(string code, DetectedLanguage language, AnalysisResult result)
    {
        switch (language)
        {
            case DetectedLanguage.CSharp:
                AnalyzeCSharp(code, result);
                break;
            case DetectedLanguage.VBNet:
                AnalyzeVB(code, result);
                break;
            case DetectedLanguage.Sql:
                AnalyzeSql(code, result);
                break;
            default:
                break;
        }
    }

    private static void AnalyzeCSharp(string code, AnalysisResult result)
    {
        var lines = SplitLines(code);

        // b) Lines exceeding 120 characters
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 120)
            {
                result.Issues.Add(new Issue
                {
                    Type = "CSharp.LongLine",
                    LineNumber = i + 1,
                    Message = $"Line exceeds 120 characters ({lines[i].Length}).",
                    Suggestion = "Wrap or refactor to reduce line length.",
                    Severity = "Warning"
                });
            }
        }

        // c) Detect TODO comments
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Issues.Add(new Issue
                {
                    Type = "CSharp.TODO",
                    LineNumber = i + 1,
                    Message = "TODO comment found.",
                    Suggestion = "Resolve or track the TODO with an issue.",
                    Severity = "Info"
                });
            }
        }

        // a) Flag any method with nested "if" appearing more than twice
        // Strategy: Track nesting depth by counting opening/closing braces and 'if' occurrences per method-like block.
        // This is a heuristic, not a full parser.
        int braceDepth = 0;
        int currentMethodIfCount = 0;
        bool inMethod = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Detect method start heuristically: line containing '(' and ')' and possibly access modifiers/return types
            if (!inMethod && line.IndexOf('(') >= 0 && line.IndexOf(')') > line.IndexOf('(')
                && (line.IndexOf("class", StringComparison.OrdinalIgnoreCase) < 0))
            {
                inMethod = true;
                currentMethodIfCount = 0;
                braceDepth = 0;
            }

            // Count 'if' tokens
            int idx = 0;
            while ((idx = line.IndexOf("if", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                // crude word boundary check
                bool isWordStart = idx == 0 || !char.IsLetterOrDigit(line[idx - 1]);
                bool isWordEnd = (idx + 2 >= line.Length) || !char.IsLetterOrDigit(line[idx + 2]);
                if (isWordStart && isWordEnd)
                {
                    currentMethodIfCount++;
                }
                idx += 2;
            }

            // Track braces to know when method likely ends
            for (int c = 0; c < line.Length; c++)
            {
                if (line[c] == '{') braceDepth++;
                else if (line[c] == '}') braceDepth--;
            }

            if (inMethod && braceDepth < 0)
            {
                // Method likely ended on this line
                if (currentMethodIfCount > 2)
                {
                    result.Issues.Add(new Issue
                    {
                        Type = "CSharp.NestedIfs",
                        LineNumber = i + 1,
                        Message = $"Method contains more than two 'if' statements (found {currentMethodIfCount}).",
                        Suggestion = "Consider refactoring (guard clauses, strategy, or early returns).",
                        Severity = "Warning"
                    });
                }
                inMethod = false;
                currentMethodIfCount = 0;
                braceDepth = 0;
            }
        }
    }

    private static void AnalyzeVB(string code, AnalysisResult result)
    {
        var lines = SplitLines(code);

        // a) Flag GoTo usage
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("GoTo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Issues.Add(new Issue
                {
                    Type = "VBNet.GoTo",
                    LineNumber = i + 1,
                    Message = "Usage of 'GoTo' found.",
                    Suggestion = "Avoid GoTo; use structured control flow (If/Else, Select Case, loops).",
                    Severity = "Warning"
                });
            }
        }

        // b) Missing End Sub or End Function (basic pattern check)
        int subCount = 0, endSubCount = 0, funcCount = 0, endFuncCount = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            if (l.IndexOf("Sub", StringComparison.OrdinalIgnoreCase) >= 0) subCount++;
            if (l.IndexOf("Function", StringComparison.OrdinalIgnoreCase) >= 0) funcCount++;
            if (l.IndexOf("End Sub", StringComparison.OrdinalIgnoreCase) >= 0) endSubCount++;
            if (l.IndexOf("End Function", StringComparison.OrdinalIgnoreCase) >= 0) endFuncCount++;
        }
        if (subCount > endSubCount)
        {
            result.Issues.Add(new Issue
            {
                Type = "VBNet.MissingEndSub",
                LineNumber = null,
                Message = "Detected 'Sub' without matching 'End Sub'.",
                Suggestion = "Ensure each Sub is closed with 'End Sub'.",
                Severity = "Error"
            });
        }
        if (funcCount > endFuncCount)
        {
            result.Issues.Add(new Issue
            {
                Type = "VBNet.MissingEndFunction",
                LineNumber = null,
                Message = "Detected 'Function' without matching 'End Function'.",
                Suggestion = "Ensure each Function is closed with 'End Function'.",
                Severity = "Error"
            });
        }
    }

    private static void AnalyzeSql(string code, AnalysisResult result)
    {
        var lines = SplitLines(code);

        // a) Flag SELECT *
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("SELECT *", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Issues.Add(new Issue
                {
                    Type = "SQL.SelectStar",
                    LineNumber = i + 1,
                    Message = "Usage of SELECT * detected.",
                    Suggestion = "Specify explicit column names to improve performance and stability.",
                    Severity = "Warning"
                });
            }
        }

        // b) DELETE or UPDATE without WHERE (very naive; line-based)
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            bool isDelete = l.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isUpdate = l.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasWhere = l.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) >= 0;
            if ((isDelete || isUpdate) && !hasWhere)
            {
                result.Issues.Add(new Issue
                {
                    Type = "SQL.MissingWhere",
                    LineNumber = i + 1,
                    Message = "DELETE/UPDATE without WHERE clause.",
                    Suggestion = "Add a WHERE clause to avoid affecting unintended rows.",
                    Severity = "Error"
                });
            }
        }

        // c) NOLOCK usage (informational)
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("NOLOCK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Issues.Add(new Issue
                {
                    Type = "SQL.NoLock",
                    LineNumber = i + 1,
                    Message = "WITH (NOLOCK) hint detected.",
                    Suggestion = "Be aware NOLOCK can read uncommitted data; consider SNAPSHOT isolation or proper indexing.",
                    Severity = "Info"
                });
            }
        }
    }

    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
