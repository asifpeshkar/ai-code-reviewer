using System.Text.RegularExpressions;

namespace AICodeReviewer.Services
{
	/// <summary>
	/// Produces a one-sentence summary of what the code likely does.
	/// </summary>
	public class SummaryGenerator
	{
		public string Generate(string code, string language)
		{
			if (string.IsNullOrWhiteSpace(code))
				return "Analyzes generic code snippet.";

			var lang = (language ?? string.Empty).Trim();

			string summary = lang.Equals("SQL", StringComparison.OrdinalIgnoreCase)
				? GenerateSql(code)
				: GenerateDotNet(code); // C# / VB.NET shared heuristics

			return Finalize(summary);
		}

		private static string GenerateDotNet(string code)
		{
			// Loan parameters
			bool hasLoanParams = code.IndexOf("principal", StringComparison.OrdinalIgnoreCase) >= 0
							  || code.IndexOf("rate", StringComparison.OrdinalIgnoreCase) >= 0
							  || code.IndexOf("term", StringComparison.OrdinalIgnoreCase) >= 0;
			if (hasLoanParams)
				return "Calculates loan interest.";

			// Extract first method/class name
			var methodMatch = Regex.Match(code,
				@"\b(?:public|private|protected|internal|shared|static|async|virtual|override|sealed|partial|extern)?\s*(?:[A-Za-z_][A-Za-z0-9_<>\[\]?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
				RegexOptions.IgnoreCase);
			string? name = methodMatch.Success ? methodMatch.Groups[1].Value : null;
			if (string.IsNullOrEmpty(name))
			{
				var classMatch = Regex.Match(code, @"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase);
				name = classMatch.Success ? classMatch.Groups[1].Value : null;
			}

			if (!string.IsNullOrEmpty(name))
			{
				if (name.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("insert", StringComparison.OrdinalIgnoreCase) >= 0)
					return "Saves data to the database.";
				if (name.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("remove", StringComparison.OrdinalIgnoreCase) >= 0)
					return "Deletes records.";

				// Verb inference fallback
				if (Regex.IsMatch(name, @"\b(Get|Save|Update|Calculate|Process|Validate)\b", RegexOptions.IgnoreCase))
				{
					var verb = Regex.Match(name, @"Get|Save|Update|Calculate|Process|Validate", RegexOptions.IgnoreCase).Value;
					return verb switch
					{
						"Get" => "Retrieves application data.",
						"Save" => "Saves application data.",
						"Update" => "Updates application data.",
						"Calculate" => "Performs a calculation.",
						"Process" => "Processes application logic.",
						"Validate" => "Validates input or state.",
						_ => "Processes application logic."
					};
				}
			}

			return "Processes application logic.";
		}

		private static string GenerateSql(string code)
		{
			string upper = code.ToUpperInvariant();

			// Primary keyword detection
			if (upper.Contains("SELECT"))
			{
				string table = ExtractSqlTable(code, "FROM");
				return $"Retrieves data from {table}.";
			}
			if (upper.Contains("INSERT"))
			{
				string table = ExtractSqlTable(code, "INTO");
				if (table == "") table = ExtractSqlTable(code, "INSERT INTO");
				return $"Inserts new records into {table}.";
			}
			if (upper.Contains("UPDATE"))
			{
				string table = ExtractSqlTable(code, "UPDATE");
				return $"Updates records in {table}.";
			}
			if (upper.Contains("DELETE"))
			{
				string table = ExtractSqlTable(code, "FROM");
				return $"Deletes records from {table}.";
			}
			if (Regex.IsMatch(upper, @"\bCREATE\s+(PROC|PROCEDURE|FUNCTION)\b"))
			{
				return "Defines stored procedure/function.";
			}

			return "Executes SQL command.";
		}

		private static string ExtractSqlTable(string code, string keyword)
		{
			// Simple: find keyword and next identifier-like token
			var rx = new Regex($"{keyword}\\s+([A-Za-z0-9_\\.]+)", RegexOptions.IgnoreCase);
			var m = rx.Match(code);
			return m.Success ? m.Groups[1].Value : "table";
		}

		private static string Finalize(string summary)
		{
			if (string.IsNullOrWhiteSpace(summary))
				summary = "Analyzes generic code snippet.";

			summary = summary.Trim();
			if (!summary.EndsWith('.')) summary += ".";

			// Capitalize first letter
			if (summary.Length > 1)
				summary = char.ToUpperInvariant(summary[0]) + summary.Substring(1);

			// Enforce <= 120 chars
			if (summary.Length > 120)
				summary = summary.Substring(0, 119) + ".";

			return summary;
		}
	}
}
