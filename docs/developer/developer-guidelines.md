# Developer Guidelines

## Local environment

- Install the .NET SDK (6.0+ recommended).
- Restore tools and dependencies with `dotnet restore`.
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

## Release checklist

- Ensure version bump in `Proligent.XmlGenerator.csproj`.
- `dotnet test` passes.
- Publish to NuGet with `dotnet pack` then `dotnet nuget push` using `NUGET_API_KEY`.
