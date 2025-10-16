using System.Text.RegularExpressions;
using AICodeReviewer.Models;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Checks function and variable names for clarity and descriptiveness.
    /// </summary>
    public class NameQualityChecker
    {
        private static readonly HashSet<string> GenericNames = new(StringComparer.OrdinalIgnoreCase)
        { "abc", "tmp", "temp", "test", "func", "data", "value", "var" };

        public List<Issue> Analyze(string code, string language)
        {
            if (string.IsNullOrWhiteSpace(code)) return new List<Issue>();

            var lang = (language ?? string.Empty).Trim();
            return lang.Equals("CSharp", StringComparison.OrdinalIgnoreCase) ? AnalyzeCSharp(code)
                 : lang.Equals("VBNet", StringComparison.OrdinalIgnoreCase) ? AnalyzeVB(code)
                 : lang.Equals("SQL", StringComparison.OrdinalIgnoreCase) ? AnalyzeSql(code)
                 : new List<Issue>();
        }

        private static List<Issue> AnalyzeCSharp(string code)
        {
            var issues = new List<Issue>();
            var lines = SplitLines(code);

            // Detect if loan-related parameters appear anywhere (for suggestion tailoring)
            bool hasLoanParams = code.IndexOf("principal", StringComparison.OrdinalIgnoreCase) >= 0
                              || code.IndexOf("rate", StringComparison.OrdinalIgnoreCase) >= 0
                              || code.IndexOf("term", StringComparison.OrdinalIgnoreCase) >= 0;

            // Capture for-loop short variable exemptions (i/j)
            var exemptShortVars = FindCSharpForLoopIterators(lines);

            // Per-line scan for methods and variables
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Method names: returnType MethodName(
                foreach (Match m in Regex.Matches(line,
                            @"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern)?\s*(?:[A-Za-z_][A-Za-z0-9_<>\[\]?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
                            RegexOptions.IgnoreCase))
                {
                    var name = m.Groups[1].Value;
                    EvaluateName(issues, name, i + 1, isFunction:true, language:"CSharp", hasLoanParams: hasLoanParams, sqlVerb: null);
                }

                // Variable names: var|Type name = ... or end with ;/,
                foreach (Match m in Regex.Matches(line,
                            @"\b(?:var|[A-Za-z_][A-Za-z0-9_<>\[\]?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;|,)",
                            RegexOptions.IgnoreCase))
                {
                    var name = m.Groups[1].Value;
                    bool exempt = (name.Equals("i", StringComparison.OrdinalIgnoreCase) || name.Equals("j", StringComparison.OrdinalIgnoreCase))
                                  && exemptShortVars.Contains(name);
                    EvaluateName(issues, name, i + 1, isFunction:false, language:"CSharp", hasLoanParams: hasLoanParams, sqlVerb: null, exemptShortVar: exempt);
                }
            }

            return issues;
        }

        private static List<Issue> AnalyzeVB(string code)
        {
            var issues = new List<Issue>();
            var lines = SplitLines(code);

            bool hasLoanParams = code.IndexOf("principal", StringComparison.OrdinalIgnoreCase) >= 0
                              || code.IndexOf("rate", StringComparison.OrdinalIgnoreCase) >= 0
                              || code.IndexOf("term", StringComparison.OrdinalIgnoreCase) >= 0;

            var exemptShortVars = FindVBForLoopIterators(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Method names: Sub/Function Name
                foreach (Match m in Regex.Matches(line,
                            @"\b(?:Public|Private|Protected|Friend|Shared|Static)?\s*(Sub|Function)\s+([A-Za-z_][A-Za-z0-9_]*)",
                            RegexOptions.IgnoreCase))
                {
                    var name = m.Groups[2].Value;
                    EvaluateName(issues, name, i + 1, isFunction:true, language:"VBNet", hasLoanParams: hasLoanParams, sqlVerb: null);
                }

                // Variables: Dim name
                foreach (Match m in Regex.Matches(line,
                            @"\bDim\s+([A-Za-z_][A-Za-z0-9_]*)",
                            RegexOptions.IgnoreCase))
                {
                    var name = m.Groups[1].Value;
                    bool exempt = (name.Equals("i", StringComparison.OrdinalIgnoreCase) || name.Equals("j", StringComparison.OrdinalIgnoreCase))
                                  && exemptShortVars.Contains(name);
                    EvaluateName(issues, name, i + 1, isFunction:false, language:"VBNet", hasLoanParams: hasLoanParams, sqlVerb: null, exemptShortVar: exempt);
                }
            }

            return issues;
        }

        private static List<Issue> AnalyzeSql(string code)
        {
            var issues = new List<Issue>();
            var lines = SplitLines(code);

            // Determine verb for suggestion
            string? sqlVerb = null;
            if (code.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) >= 0) sqlVerb = "InsertRecord";
            else if (code.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0) sqlVerb = "UpdateRecord";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Table/column aliases: FROM/JOIN ... [AS] alias
                foreach (Match m in Regex.Matches(line,
                            @"\b(?:FROM|JOIN)\s+[^\s]+\s+(?:AS\s+)?([A-Za-z_][A-Za-z0-9_]*)",
                            RegexOptions.IgnoreCase))
                {
                    var alias = m.Groups[1].Value;
                    EvaluateName(issues, alias, i + 1, isFunction:false, language:"SQL", hasLoanParams:false, sqlVerb: sqlVerb);
                }

                // Column aliases: AS alias
                foreach (Match m in Regex.Matches(line,
                            @"\bAS\s+([A-Za-z_][A-Za-z0-9_]*)",
                            RegexOptions.IgnoreCase))
                {
                    var alias = m.Groups[1].Value;
                    EvaluateName(issues, alias, i + 1, isFunction:false, language:"SQL", hasLoanParams:false, sqlVerb: sqlVerb);
                }

                // Procedure/function names in CREATE statements
                foreach (Match m in Regex.Matches(line,
                            @"\bCREATE\s+(?:PROC|PROCEDURE|FUNCTION)\s+([A-Za-z_][A-Za-z0-9_]*)",
                            RegexOptions.IgnoreCase))
                {
                    var name = m.Groups[1].Value;
                    EvaluateName(issues, name, i + 1, isFunction:true, language:"SQL", hasLoanParams:false, sqlVerb: sqlVerb);
                }
            }

            return issues;
        }

        private static void EvaluateName(List<Issue> issues, string name, int lineNumber, bool isFunction, string language, bool hasLoanParams, string? sqlVerb, bool exemptShortVar = false)
        {
            // d) Starts with a number
            if (name.Length > 0 && char.IsDigit(name[0]))
            {
                issues.Add(MakeIssue(lineNumber, $"Name '{name}' starts with a number.", Suggest(language, isFunction, hasLoanParams, sqlVerb)));
                return;
            }

            // a) Function/method name shorter than 3
            if (isFunction && name.Length < 3)
            {
                issues.Add(MakeIssue(lineNumber, $"Function/method name '{name}' is too short.", Suggest(language, isFunction, hasLoanParams, sqlVerb)));
            }

            // c) Variable name shorter than 2 (unless i/j in for-loop)
            if (!isFunction && name.Length < 2 && !exemptShortVar)
            {
                issues.Add(MakeIssue(lineNumber, $"Variable name '{name}' is too short.", Suggest(language, isFunction, hasLoanParams, sqlVerb)));
            }

            // b) Generic names list
            if (GenericNames.Contains(name))
            {
                issues.Add(MakeIssue(lineNumber, $"Name '{name}' is generic and non-descriptive.", Suggest(language, isFunction, hasLoanParams, sqlVerb)));
            }
        }

        private static Issue MakeIssue(int line, string message, string suggestion)
            => new()
            {
                Type = "Naming.NonDescriptive",
                LineNumber = line,
                Message = message,
                Suggestion = suggestion,
                Severity = "Info"
            };

        private static string Suggest(string language, bool isFunction, bool hasLoanParams, string? sqlVerb)
        {
            // Base suggestion
            string baseSuggestion = "Use a meaningful verb/noun, e.g., 'CalculateLoanInterest' or 'ProcessData'.";

            if (language.Equals("SQL", StringComparison.OrdinalIgnoreCase) && sqlVerb is not null)
            {
                return $"{baseSuggestion} For SQL, consider '{sqlVerb}'.";
            }

            if ((language.Equals("CSharp", StringComparison.OrdinalIgnoreCase) || language.Equals("VBNet", StringComparison.OrdinalIgnoreCase)) && hasLoanParams)
            {
                return $"{baseSuggestion} Given parameters like principal/rate/term, consider 'CalculateLoanInterest'.";
            }

            return baseSuggestion;
        }

        private static HashSet<string> FindCSharpForLoopIterators(string[] lines)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var m = Regex.Match(line, @"for\s*\(([^)]*)\)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var header = m.Groups[1].Value;
                    // Common pattern: int i = 0; or var j = 0;
                    var mi = Regex.Match(header, @"\b(?:var|[A-Za-z_][A-Za-z0-9_<>\[\]?]*)\s+([ij])\b");
                    if (mi.Success) set.Add(mi.Groups[1].Value);
                }
            }
            return set;
        }

        private static HashSet<string> FindVBForLoopIterators(string[] lines)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var m = Regex.Match(line, @"\bFor\s+([A-Za-z_][A-Za-z0-9_]*)\s*=", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var v = m.Groups[1].Value;
                    if (v.Equals("i", StringComparison.OrdinalIgnoreCase) || v.Equals("j", StringComparison.OrdinalIgnoreCase))
                        set.Add(v);
                }
            }
            return set;
        }

        private static string[] SplitLines(string text)
            => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }
}
