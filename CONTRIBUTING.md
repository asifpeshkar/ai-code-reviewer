# Contributing

Thank you for your interest in contributing to AI-Assisted Code Reviewer!

## Development Setup

- Prerequisite: .NET SDK 8.0 or newer (project tested with .NET 9)
- Clone and build:
  - `git clone https://github.com/asifpeshkar/ai-code-reviewer.git`
  - `cd ai-code-reviewer`
  - `dotnet build`
- Run (console): `dotnet run -- samples\sample.cs`
- Run (web server): `dotnet run -- --server` → open http://localhost:5080

## How to Contribute

- Search existing issues and discussions to avoid duplicates.
- Open a new issue describing the bug/feature before large changes.
- Keep changes focused and incremental; prefer multiple small PRs over one large PR.

## Coding Guidelines

- Keep the codebase lightweight and dependency-free when possible.
- Prefer readable, well-scoped methods over overly clever one-liners.
- Add comments for non-obvious heuristics and regexes.
- Follow existing naming and folder structure:
  - `Services/` for analysis components
  - `Models/` for DTOs and result shapes
  - `Enums/` for enums
  - `web/` for the static site

## Testing & Validation

- Manual smoke test with the provided samples in `samples/`.
- For server mode, validate `/analyze` returns the expected JSON shape.
- If changing the UI, test against C#, VB.NET, and SQL inputs.

## Pull Requests

- Fork the repository and create a feature branch.
- Write a clear title and description. Include screenshots for UI changes.
- Ensure the project builds: `dotnet build`.
- Keep commits clean and meaningful. Rebase/squash as needed.
- Link the relevant issue(s) in the PR description.

## Code of Conduct

- Be respectful, inclusive, and constructive.
- Assume good intent and provide actionable feedback.

Thanks for helping improve AI-Assisted Code Reviewer!
