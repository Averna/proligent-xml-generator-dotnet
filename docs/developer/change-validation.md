# Change validation

Use these checks before raising a pull request:

1. **Unit tests**  
   - `dotnet test Proligent.XmlGenerator.sln`
   - Ensure generated XML matches fixtures in `test/Proligent.XmlGenerator.Tests/Expected`.

2. **Schema validation**  
   - Run `dotnet test` to exercise `XmlValidator`; optionally validate any newly generated XML with `XmlValidator.ValidateXml(path)`.

3. **Static analysis**  
   - Run Mega-Linter locally or rely on the CI workflow.

4. **Docs**  
   - `mkdocs build` after running `dotnet build` so mkdocstrings-csharp can read the compiled assembly/XML doc.

5. **Packaging**  
   - `dotnet pack src/Proligent.XmlGenerator/Proligent.XmlGenerator.csproj -c Release` to ensure NuGet metadata is valid.
