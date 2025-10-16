using System.Text.RegularExpressions;

namespace AICodeReviewer.Services
{
	/// <summary>
	/// Simple beautifier: reindents code and normalizes keyword casing.
	/// Intended as a heuristic, not a full formatter.
	/// </summary>
	public class Beautifier
	{
		public string Beautify(string code, string language)
		{
			if (string.IsNullOrWhiteSpace(code)) return string.Empty;
			if (language.Equals("SQL", StringComparison.OrdinalIgnoreCase))
				return BeautifySql(code);
			if (language.Equals("VBNet", StringComparison.OrdinalIgnoreCase))
				return BeautifyVb(code);
			// Default to C# style
			return BeautifyCSharp(code);
		}

		private static string BeautifyCSharp(string code)
		{
			var lines = SplitLines(code);
			int indent = 0;
			var result = new List<string>(lines.Length);
			foreach (var raw in lines)
			{
				string line = raw.Trim();
				if (line.Length == 0) { result.Add(string.Empty); continue; }

				// Pre-deindent if line starts with closing brace
				int leadingClose = CountLeading(line, '}');
				indent = Math.Max(0, indent - leadingClose);

				// Normalize some C# keywords to PascalCase
				line = NormalizeKeywords(line, CSharpKeywordsPascal);

				result.Add(new string(' ', indent * 4) + line);

				// Adjust indent based on brace balance
				int opens = CountChar(line, '{');
				int closes = CountChar(line, '}');
				indent = Math.Max(0, indent + opens - closes);
			}
			return string.Join("\n", result);
		}

		private static string BeautifyVb(string code)
		{
			var lines = SplitLines(code);
			int indent = 0;
			var result = new List<string>(lines.Length);
			foreach (var raw in lines)
			{
				string line = raw.Trim();
				if (line.Length == 0) { result.Add(string.Empty); continue; }

				// Deindent on End keywords
				if (Regex.IsMatch(line, @"^End\b", RegexOptions.IgnoreCase))
					indent = Math.Max(0, indent - 1);

				// Normalize VB keywords to PascalCase
				line = NormalizeKeywords(line, VbKeywordsPascal);

				result.Add(new string(' ', indent * 4) + line);

				// Increase indent on block starts
				if (Regex.IsMatch(line, @"\b(Class|Module|Sub|Function)\b", RegexOptions.IgnoreCase))
					indent++;
				else if (Regex.IsMatch(line, @"\bIf\b.*\bThen\b", RegexOptions.IgnoreCase))
					indent++;
			}
			return string.Join("\n", result);
		}

		private static string BeautifySql(string code)
		{
			var lines = SplitLines(code);
			int indent = 0;
			var result = new List<string>(lines.Length);
			foreach (var raw in lines)
			{
				string line = raw.Trim();
				if (line.Length == 0) { result.Add(string.Empty); continue; }

				// Deindent on END
				if (Regex.IsMatch(line, @"^END\b", RegexOptions.IgnoreCase))
					indent = Math.Max(0, indent - 1);

				// Uppercase SQL keywords
				line = NormalizeKeywordsUpper(line, SqlKeywordsUpper);

				result.Add(new string(' ', indent * 4) + line);

				// Indent after BEGIN
				if (Regex.IsMatch(line, @"\bBEGIN\b", RegexOptions.IgnoreCase))
					indent++;
			}
			return string.Join("\n", result);
		}

		private static string NormalizeKeywords(string line, string[] keywordsPascal)
		{
			foreach (var kw in keywordsPascal)
			{
				// match case-insensitive word boundary
				line = Regex.Replace(line, $"\\b{Regex.Escape(kw.ToLowerInvariant())}\\b", kw, RegexOptions.IgnoreCase);
			}
			return line;
		}

		private static string NormalizeKeywordsUpper(string line, string[] keywords)
		{
			foreach (var kw in keywords)
			{
				line = Regex.Replace(line, $"\\b{Regex.Escape(kw)}\\b", kw, RegexOptions.IgnoreCase);
			}
			return line;
		}

		private static int CountChar(string text, char ch)
		{
			int c = 0; foreach (char x in text) if (x == ch) c++; return c;
		}

		private static int CountLeading(string text, char ch)
		{
			int i = 0; while (i < text.Length && text[i] == ch) i++; return i;
		}

		private static string[] SplitLines(string text)
			=> text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

		private static readonly string[] CSharpKeywordsPascal = new[]
		{
			"Using", "Namespace", "Public", "Private", "Protected", "Internal",
			"Class", "Struct", "Record", "Void", "Static", "Async", "Return",
			"If", "Else", "For", "Foreach", "While", "Switch", "Case", "Break",
			"Continue", "Try", "Catch", "Finally"
		};

		private static readonly string[] VbKeywordsPascal = new[]
		{
			"Public", "Private", "Protected", "Friend", "Shared", "Static", "Module",
			"Class", "Sub", "Function", "End", "End Sub", "End Function", "End Class",
			"If", "Then", "Else", "End If"
		};

		private static readonly string[] SqlKeywordsUpper = new[]
		{
			"SELECT","INSERT","UPDATE","DELETE","CREATE","ALTER","DROP","FROM","WHERE","JOIN",
			"LEFT","RIGHT","FULL","INNER","OUTER","ON","GROUP","BY","ORDER","HAVING","INTO",
			"VALUES","SET","AND","OR","NOT","BEGIN","END","AS","WITH","TOP","DISTINCT"
		};
	}
}
