# Change validation

Use these checks before raising a pull request:

1. **Unit tests**  
   - `dotnet test Proligent.XmlGenerator.sln`
   - Ensure generated XML matches fixtures in `test/Proligent.XmlGenerator.Tests/Expected`.

2. **Schema validation**  
   - Run `dotnet test` to exercise `XmlValidator`; optionally validate any newly generated XML with `XmlValidator.ValidateXml(path)`.

3. **Static analysis**  
   - Run Mega-Linter locally or rely on the CI workflow.

4. **Packaging**  
   - `dotnet pack src/Proligent.XmlGenerator/Proligent.XmlGenerator.csproj -c Release` to ensure NuGet metadata is valid.
   
## Validate XMLs Can be Integrated In Proligent

You can ask the Proligent team to validate that your generated XMLs can be
integrated in Proligent Analytics or Proligent Cloud.

> [!NOTE]
> Even valid XMLs can be rejected. There are some validation that can't be done
> in a XSD. Please make sure the DIT (Data Integration Toolkit) can process
> your generated XMLs.

