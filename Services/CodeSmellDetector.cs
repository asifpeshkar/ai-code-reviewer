using System.Text.RegularExpressions;
using System.Linq;
using AICodeReviewer.Models;

namespace AICodeReviewer.Services
{
	/// <summary>
	/// Detects common static code smells using simple, language-aware heuristics.
	/// </summary>
	public class CodeSmellDetector
	{
		public List<Issue> Analyze(string code, string language)
		{
			var issues = new List<Issue>();
			if (string.IsNullOrWhiteSpace(code)) return issues;

			var lang = (language ?? string.Empty).Trim();
			var lines = SplitLines(code);

			// Deep nesting
			if (lang.Equals("CSharp", StringComparison.OrdinalIgnoreCase))
				DetectDeepNestingCSharp(lines, issues);
			else if (lang.Equals("VBNet", StringComparison.OrdinalIgnoreCase))
				DetectDeepNestingVB(lines, issues);
			else if (lang.Equals("SQL", StringComparison.OrdinalIgnoreCase))
				DetectDeepNestingSql(lines, issues);

			// Long parameter lists
			DetectLongParameterLists(code, lang, issues);

			// Large classes or procedures
			DetectLargeStructures(lines, lang, issues);

			// Repeated literals
			DetectRepeatedLiterals(lines, issues);

			return issues;
		}

		private static void DetectDeepNestingCSharp(string[] lines, List<Issue> issues)
		{
			var methodRegex = new Regex(@"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern)?\s*[A-Za-z_][A-Za-z0-9_<>\[\]?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.IgnoreCase);
			int i = 0;
			while (i < lines.Length)
			{
				var line = lines[i];
				var m = methodRegex.Match(line);
				if (!m.Success) { i++; continue; }
				string methodName = m.Groups[1].Value;

				// Find body start '{' and track depth until matching '}' closes
				int startLine = i + 1;
				int braceDepth = 0;
				bool inBody = false;
				int maxDepth = 0;
				int j = i;
				for (; j < lines.Length; j++)
				{
					foreach (char ch in lines[j])
					{
						if (ch == '{') { braceDepth++; inBody = true; maxDepth = Math.Max(maxDepth, braceDepth); }
						else if (ch == '}') { braceDepth--; }
					}
					if (inBody && braceDepth <= 0) { j++; break; }
				}
				if (maxDepth > 3)
				{
					issues.Add(new Issue
					{
						Type = "Smell.DeepNesting",
						LineNumber = startLine,
						Message = $"Nested blocks exceed 3 levels in function '{methodName}'.",
						Suggestion = "Refactor using early returns or helper methods.",
						Severity = "Warning"
					});
				}
				i = j; // continue after this method
			}
		}

		private static void DetectDeepNestingVB(string[] lines, List<Issue> issues)
		{
			var methodStart = new Regex(@"\b(?:Public|Private|Protected|Friend|Shared|Static)?\s*(Sub|Function)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
			int i = 0;
			while (i < lines.Length)
			{
				var ms = methodStart.Match(lines[i]);
				if (!ms.Success) { i++; continue; }
				string methodName = ms.Groups[2].Value;
				int startLine = i + 1;
				int depth = 0;
				int maxDepth = 0;
				int j = i;
				for (; j < lines.Length; j++)
				{
					var l = lines[j];
					// Count If/End If (very basic; ElseIf counted as If)
					var idx = 0;
					while ((idx = l.IndexOf("If", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
					{
						// Avoid matching End If here
						if (l.IndexOf("End If", StringComparison.OrdinalIgnoreCase) == idx) { idx += 2; continue; }
						depth++; maxDepth = Math.Max(maxDepth, depth); idx += 2;
					}
					if (l.IndexOf("End If", StringComparison.OrdinalIgnoreCase) >= 0) depth = Math.Max(0, depth - 1);
					if (Regex.IsMatch(l, @"\bEnd\s+(Sub|Function)\b", RegexOptions.IgnoreCase)) { j++; break; }
				}
				if (maxDepth > 3)
				{
					issues.Add(new Issue
					{
						Type = "Smell.DeepNesting",
						LineNumber = startLine,
						Message = $"Nested blocks exceed 3 levels in function '{methodName}'.",
						Suggestion = "Refactor using early returns or helper methods.",
						Severity = "Warning"
					});
				}
				i = j;
			}
		}

		private static void DetectDeepNestingSql(string[] lines, List<Issue> issues)
		{
			// Approximate by counting BEGIN/END depth per batch
			int depth = 0, maxDepth = 0, startLine = 1;
			for (int i = 0; i < lines.Length; i++)
			{
				var l = lines[i];
				int beginCount = CountToken(l, "BEGIN");
				int endCount = CountToken(l, "END");
				depth += beginCount;
				maxDepth = Math.Max(maxDepth, depth);
				depth -= endCount;
			}
			if (maxDepth > 3)
			{
				issues.Add(new Issue
				{
					Type = "Smell.DeepNesting",
					LineNumber = startLine,
					Message = "Nested BEGIN/END blocks exceed 3 levels.",
					Suggestion = "Refactor into smaller procedures or reduce nesting.",
					Severity = "Warning"
				});
			}
		}

		private static void DetectLongParameterLists(string code, string language, List<Issue> issues)
		{
			if (language.Equals("CSharp", StringComparison.OrdinalIgnoreCase))
			{
				var rx = new Regex(@"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern)?\s*[A-Za-z_][A-Za-z0-9_<>\[\]?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)", RegexOptions.IgnoreCase);
				var lines = SplitLines(code);
				for (int i = 0; i < lines.Length; i++)
				{
					var m = rx.Match(lines[i]);
					if (!m.Success) continue;
					var method = m.Groups[1].Value;
					var paramList = m.Groups[2].Value;
					int commas = CountCommas(paramList);
					int paramCount = string.IsNullOrWhiteSpace(paramList) ? 0 : commas + 1;
					if (paramCount > 5)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LongParameterList",
							LineNumber = i + 1,
							Message = $"Method '{method}' has {paramCount} parameters.",
							Suggestion = "Consider grouping parameters into an object or reducing parameters.",
							Severity = "Warning"
						});
					}
				}
			}
			else if (language.Equals("VBNet", StringComparison.OrdinalIgnoreCase))
			{
				var rx = new Regex(@"\b(?:Public|Private|Protected|Friend|Shared|Static)?\s*(Sub|Function)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)", RegexOptions.IgnoreCase);
				var lines = SplitLines(code);
				for (int i = 0; i < lines.Length; i++)
				{
					var m = rx.Match(lines[i]);
					if (!m.Success) continue;
					var method = m.Groups[2].Value;
					var paramList = m.Groups[3].Value;
					int commas = CountCommas(paramList);
					int paramCount = string.IsNullOrWhiteSpace(paramList) ? 0 : commas + 1;
					if (paramCount > 5)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LongParameterList",
							LineNumber = i + 1,
							Message = $"Method '{method}' has {paramCount} parameters.",
							Suggestion = "Consider grouping parameters into a type or reducing parameters.",
							Severity = "Warning"
						});
					}
				}
			}
			else if (language.Equals("SQL", StringComparison.OrdinalIgnoreCase))
			{
				var lines = SplitLines(code);
				for (int i = 0; i < lines.Length; i++)
				{
					var m = Regex.Match(lines[i], @"\bCREATE\s+(?:PROC|PROCEDURE|FUNCTION)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)", RegexOptions.IgnoreCase);
					if (!m.Success) continue;
					string name = m.Groups[1].Value;
					string paramList = m.Groups[2].Value;
					int commas = CountCommas(paramList);
					int paramCount = string.IsNullOrWhiteSpace(paramList) ? 0 : commas + 1;
					if (paramCount > 5)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LongParameterList",
							LineNumber = i + 1,
							Message = $"Procedure '{name}' has {paramCount} parameters.",
							Suggestion = "Refactor to reduce parameters or use table-valued parameters.",
							Severity = "Warning"
						});
					}
				}
			}
		}

		private static void DetectLargeStructures(string[] lines, string language, List<Issue> issues)
		{
			if (language.Equals("CSharp", StringComparison.OrdinalIgnoreCase))
			{
				var classRx = new Regex(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
				var methodRx = new Regex(@"\b(?:public|private|protected|internal|static|async|virtual|override|sealed|partial|extern)?\s*[A-Za-z_][A-Za-z0-9_<>\[\]?]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", RegexOptions.IgnoreCase);
				for (int i = 0; i < lines.Length; i++)
				{
					var m = classRx.Match(lines[i]);
					if (!m.Success) continue;
					string className = m.Groups[1].Value;
					int start = i;
					int depth = 0; bool started = false; int j = i;
					int methodCount = 0;
					for (; j < lines.Length; j++)
					{
						foreach (char ch in lines[j])
						{
							if (ch == '{') { depth++; started = true; }
							else if (ch == '}') { depth--; }
						}
						if (methodRx.IsMatch(lines[j])) methodCount++;
						if (started && depth <= 0) { j++; break; }
					}
					int span = j - start;
					if (span > 300 || methodCount > 10)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LargeClass",
							LineNumber = start + 1,
							Message = $"Class '{className}' spans {span} lines and has {methodCount} methods.",
							Suggestion = "Split into smaller classes or reduce responsibilities.",
							Severity = "Info"
						});
					}
					i = j;
				}
			}
			else if (language.Equals("VBNet", StringComparison.OrdinalIgnoreCase))
			{
				var classStart = new Regex(@"\bClass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
				var methodStart = new Regex(@"\b(?:Public|Private|Protected|Friend|Shared|Static)?\s*(Sub|Function)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
				for (int i = 0; i < lines.Length; i++)
				{
					var m = classStart.Match(lines[i]);
					if (!m.Success) continue;
					string className = m.Groups[1].Value;
					int start = i;
					int j = i;
					int methodCount = 0;
					for (; j < lines.Length; j++)
					{
						if (methodStart.IsMatch(lines[j])) methodCount++;
						if (Regex.IsMatch(lines[j], @"\bEnd\s+Class\b", RegexOptions.IgnoreCase)) { j++; break; }
					}
					int span = j - start;
					if (span > 300 || methodCount > 10)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LargeClass",
							LineNumber = start + 1,
							Message = $"Class '{className}' spans {span} lines and has {methodCount} methods.",
							Suggestion = "Split into smaller classes or reduce responsibilities.",
							Severity = "Info"
						});
					}
					i = j;
				}
			}
			else if (language.Equals("SQL", StringComparison.OrdinalIgnoreCase))
			{
				for (int i = 0; i < lines.Length; i++)
				{
					var m = Regex.Match(lines[i], @"\bCREATE\s+(?:PROC|PROCEDURE|FUNCTION)\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
					if (!m.Success) continue;
					string name = m.Groups[1].Value;
					int start = i;
					int j = i;
					for (; j < lines.Length; j++)
					{
						if (Regex.IsMatch(lines[j], @"\bEND\b", RegexOptions.IgnoreCase)) { j++; break; }
						if (Regex.IsMatch(lines[j], @"^\s*GO\b", RegexOptions.IgnoreCase)) { break; }
					}
					int span = j - start;
					if (span > 300)
					{
						issues.Add(new Issue
						{
							Type = "Smell.LargeProcedure",
							LineNumber = start + 1,
							Message = $"Procedure/Function '{name}' spans {span} lines.",
							Suggestion = "Break into smaller procedures or review logic size.",
							Severity = "Info"
						});
					}
					i = j;
				}
			}
		}

		private static void DetectRepeatedLiterals(string[] lines, List<Issue> issues)
		{
			var stringRx = new Regex("\"(.*?)\"", RegexOptions.IgnoreCase);
			var numberRx = new Regex(@"\b\d+(?:\.\d+)?\b", RegexOptions.IgnoreCase);
			var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

			for (int i = 0; i < lines.Length; i++)
			{
				foreach (Match m in stringRx.Matches(lines[i]))
				{
					string lit = m.Groups[1].Value;
					if (string.IsNullOrEmpty(lit)) continue;
					map.TryAdd(lit, new List<int>());
					map[lit].Add(i + 1);
				}
				foreach (Match m in numberRx.Matches(lines[i]))
				{
					string lit = m.Value;
					map.TryAdd(lit, new List<int>());
					map[lit].Add(i + 1);
				}
			}

			foreach (var kvp in map)
			{
				if (kvp.Value.Count >= 3)
				{
					issues.Add(new Issue
					{
						Type = "Smell.RepeatedLiteral",
						LineNumber = kvp.Value.First(),
						Message = $"Literal '{kvp.Key}' appears {kvp.Value.Count} times.",
						Suggestion = "Extract to a constant or parameter to improve maintainability.",
						Severity = "Info"
					});
				}
			}
		}

		private static int CountToken(string line, string token)
		{
			int count = 0, idx = 0;
			while ((idx = line.IndexOf(token, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				count++; idx += token.Length;
			}
			return count;
		}

		private static int CountCommas(string text)
		{
			int count = 0; foreach (char c in text) if (c == ',') count++; return count;
		}

		private static string[] SplitLines(string text)
			=> text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
	}
}
