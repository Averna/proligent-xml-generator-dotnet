# Proligentƒ,› XML Generator for .NET

Generate Proligentƒ,› Datawarehouse XML payloads from strongly typed C# models.
The library mirrors the Python `proligent-xml-generator` interface while
following .NET conventions, bundling XSD validation and IntelliSense-ready XML
documentation.

- **Typed model**: `DataWareHouse`, `ProcessRun`, `OperationRun`, `SequenceRun`,
  `StepRun`, `Measure`, `Limit`, `Characteristic`, and `Document`.
- **Validation**: built-in schema validation with `XmlValidator`.
- **Docs**: mkdocs + mkdocstrings-csharp for API docs, plus XML doc output for
  editor auto-completion.

Get started with the [Quickstart](user/quickstart.md) or review the
[Manufacturing Information Model](user/manufacturing-information-model.md) for
data mapping guidance.
