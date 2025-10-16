using AICodeReviewer.Enums;
using AICodeReviewer.Models;
using AICodeReviewer.Services;

// Entry point: console mode (default) or server mode with --server flag.

var detector = new LanguageDetector();
var analyzer = new CodeAnalyzerService();
var beautifier = new Beautifier();

// If server mode requested, run minimal API
if (args.Length > 0 && args[0].Equals("--server", StringComparison.OrdinalIgnoreCase))
{
	await RunServerAsync();
	return;
}

string code = await ReadCodeAsync(args);

if (string.IsNullOrWhiteSpace(code))
{
	Console.Error.WriteLine("No code provided. Pass a file path as the first argument or pipe/paste code via stdin.");
	return;
}

DetectedLanguage language = detector.Detect(code);
AnalysisResult result = analyzer.Analyze(code, language);
var languageName = LanguageDetector.ToName(language);

// Display output
Console.WriteLine("=== Summary ===");
Console.WriteLine(result.Summary ?? "No summary available.");
Console.WriteLine();

Console.WriteLine("=== Language ===");
Console.WriteLine(languageName);
Console.WriteLine();

Console.WriteLine("=== Issues Found ===");
if (result.Issues.Count == 0)
{
	Console.WriteLine("No issues detected.\n");
}
else
{
	int i = 1;
	foreach (var issue in result.Issues)
	{
		var lineInfo = issue.LineNumber.HasValue ? $" (Line {issue.LineNumber})" : string.Empty;
		var typeInfo = string.IsNullOrWhiteSpace(issue.Type) ? "UnknownRule" : issue.Type;

		var color = Console.ForegroundColor;
		Console.ForegroundColor = issue.Severity?.ToLowerInvariant() switch
		{
			"error" => ConsoleColor.Red,
			"warning" => ConsoleColor.Yellow,
			_ => ConsoleColor.Cyan
		};

		// Requested final format: [Type] (Line n): Message -> Suggestion
		var message = string.IsNullOrWhiteSpace(issue.Message) ? "" : issue.Message;
		var suggestion = string.IsNullOrWhiteSpace(issue.Suggestion) ? "" : issue.Suggestion;
		Console.WriteLine($"{i++}. [{typeInfo}]{lineInfo}: {message} -> {suggestion}");
		Console.ForegroundColor = color;
	}
}

// Optional beautified code section
Console.WriteLine();
Console.WriteLine("=== Beautified Code (optional) ===");
var beautified = beautifier.Beautify(code, languageName);
Console.WriteLine(beautified);

static async Task<string> ReadCodeAsync(string[] args)
{
	// 1) If a file path is provided, read from the file
	if (args.Length > 0)
	{
		string pathArg = args[0];
		if (File.Exists(pathArg))
		{
			return await File.ReadAllTextAsync(pathArg);
		}
	}

	// 2) Otherwise, read from standard input until EOF
	//    PowerShell: you can paste code then press Ctrl+Z then Enter
	if (!Console.IsInputRedirected)
	{
		Console.WriteLine("Paste your code snippet. Press Ctrl+Z then Enter to finish (Windows PowerShell).");
	}
	using var reader = new StreamReader(Console.OpenStandardInput());
	string content = await reader.ReadToEndAsync();
	return content;
}

static async Task RunServerAsync()
{
	var builder = WebApplication.CreateBuilder();
	builder.Services.ConfigureHttpJsonOptions(options =>
	{
		options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
	});
	var app = builder.Build();

	// Static files for web UI (from output folder web/)
	var webRoot = Path.Combine(AppContext.BaseDirectory, "web");
	var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
	app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
	app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

	app.MapPost("/analyze", async (HttpContext ctx) =>
	{
		using var reader = new StreamReader(ctx.Request.Body);
		var body = await reader.ReadToEndAsync();
		string code = tryExtractCode(body);

		var detector = new LanguageDetector();
		var analyzer = new CodeAnalyzerService();
		var beautifier = new Beautifier();

		var lang = detector.Detect(code);
		var result = analyzer.Analyze(code, lang);
		var languageName = LanguageDetector.ToName(lang);
		var beautified = beautifier.Beautify(code, languageName);

		await ctx.Response.WriteAsJsonAsync(new
		{
			summary = result.Summary,
			language = languageName,
			issues = result.Issues,
			beautifiedCode = beautified
		});
	});

	await app.RunAsync("http://localhost:5080");

	static string tryExtractCode(string json)
	{
		try
		{
			var doc = System.Text.Json.JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("code", out var el))
			{
				return el.GetString() ?? string.Empty;
			}
		}
		catch { }
		return string.Empty;
	}
}

