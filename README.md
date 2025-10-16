# AI-Assisted Code Reviewer & Code Smell Detector (Scaffold)

This is a minimal scaffold for a console-based analyzer that:

- Accepts a code snippet (via file path argument or stdin)
- Detects language (C#, VB.NET, SQL)
- Runs a stubbed `CodeAnalyzerService` (rule-based, naming, smells)
- Prints Summary, Language, and Issues Found

Logic is intentionally stubbed; this is the initial structure to build upon.

## Run

Build and run:

```powershell
# from the repository root
dotnet build

# Run with a file path
dotnet run -- samples/sample.cs

# Run with SQL sample
dotnet run -- samples/sample.sql

# Paste code via stdin (press Ctrl+Z then Enter to finish in Windows PowerShell)
Get-Content samples/sample.cs | dotnet run --
```

## Project structure

- `Program.cs` – input/output wiring
- `Enums/DetectedLanguage.cs` – supported languages
- `Models/Issue.cs` – issue model
- `Models/AnalysisResult.cs` – analysis result model
- `Services/LanguageDetector.cs` – naive detector
- `Services/CodeAnalyzerService.cs` – stubbed analyzer
- `samples/` – example code files

## Next steps

- Implement real detection heuristics or use parsers
- Fill in rule-based analysis, naming checks, and code smells
- Add tests (xUnit) and CI
- Support more languages