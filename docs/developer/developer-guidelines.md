# Developer Guidelines

## Local environment

- Install the .NET SDK (6.0+ recommended) and Python 3.10+ for documentation generation.
- Restore tools and dependencies with `dotnet restore` and `pip install -r requirements-docs.txt`.
- Enable XML documentation output (already configured) to keep IntelliSense and docs fresh.

## Build and test

```bash
dotnet restore Proligent.XmlGenerator.sln
dotnet build Proligent.XmlGenerator.sln -c Release
dotnet test Proligent.XmlGenerator.sln
```

## Linting

Mega-Linter runs in CI. To run locally:

```bash
docker run -it --rm -v "${PWD}:/tmp/lint" oxsecurity/megalinter:v9
```

## Documentation

Generate the docs locally:

```bash
dotnet build src/Proligent.XmlGenerator/Proligent.XmlGenerator.csproj -c Release
pip install -r requirements-docs.txt
mkdocs serve
```

The GitHub workflow builds and publishes the mkdocs site (including mkdocstrings-csharp output) to GitHub Pages.

## Release checklist

- Ensure version bump in `Proligent.XmlGenerator.csproj`.
- `dotnet test` passes.
- Docs build (`mkdocs build`) succeeds.
- Publish to NuGet with `dotnet pack` then `dotnet nuget push` using `NUGET_API_KEY`.
