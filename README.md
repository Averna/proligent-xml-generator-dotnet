# Proligent™ XML Generator for .NET

[![Build][actions-shield]][actions]
[![NuGet Gallery][nuget-version]][nuget-page]

[actions-shield]: https://github.com/averna/proligent-xml-generator-dotnet/actions/workflows/build.yml/badge.svg
[actions]: https://github.com/averna/proligent-xml-generator-dotnet/actions/workflows/build.yml
[nuget-version]: https://img.shields.io/nuget/v/Proligent.XmlGenerator
[nuget-page]: https://www.nuget.org/packages/Proligent.XmlGenerator

.NET library for generating Proligent™ XML files. It provides a simple,
structured API for building valid import files, reducing manual XML writing and
ensuring consistent data formatting. These files are used to import data into
[Proligent™ Cloud][cloud] and [Proligent™ Analytics][analytics].

[cloud]: https://www.averna.com/en/products/smart-data-management/proligent-cloud
[analytics]: https://www.averna.com/en/products/proligent-analytics

> **Tip:** Refer to the [Proligent™ Manufacturing Information Model][model] to
> learn how to structure and map your data in Proligent™.

[model]: https://github.com/averna/proligent-xml-generator-dotnet/blob/main/docs/user/manufacturing-information-model.md

Proligent™ software are designed for Operations Managers, Quality Engineers,
Manufacturing Engineers and Test Engineers. This easy-to-use software solution
monitors test stations and provides valuable insight into your product line.

## Installation

Install from NuGet (package id `Proligent.XmlGenerator`):

```bash
dotnet add package Proligent.XmlGenerator
```

## Getting started

Each layer of the Proligent Manufacturing Information Model is represented by a
class in the library. All timestamps are automatically formatted to the
Datawarehouse schema, and XML output can be validated with the bundled XSDs.

### Example 1

```csharp
using Proligent.XmlGenerator;

var limit = new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 10, higherBound: 25);
var measure = new Measure(
    value: 15,
    status: ExecutionStatusKind.PASS,
    limit: limit,
    time: DateTime.UtcNow);
var step = new StepRun(name: "Step1", status: ExecutionStatusKind.PASS, measure: measure);

var sequence = new SequenceRun(name: "Sequence1", status: ExecutionStatusKind.PASS);
sequence.AddStepRun(step);

var operation = new OperationRun(
    station: "Station/readme_example",
    sequences: new[] { sequence },
    name: "Operation1",
    status: ExecutionStatusKind.PASS);

var process = new ProcessRun(
    productUnitIdentifier: "DutSerialNumber",
    productFullName: "Product/readme_example",
    operations: new[] { operation },
    name: "Process/readme_example",
    processMode: "PROD",
    status: ExecutionStatusKind.PASS);

var product = new ProductUnit(
    productUnitIdentifier: "DutSerialNumber",
    productFullName: "Product/readme_example",
    manufacturer: "Averna");

var warehouse = new DataWareHouse(topProcess: process, productUnit: product);
warehouse.SaveXml(); // writes to C:\Proligent\IntegrationService\Acquisition\Proligent_<guid>.xml
```

> **Note:** For simplicity this example omits the start and end times, so they
> default to `DateTime.UtcNow`. It is highly recommended to set these values with
> real timestamps when used in production.

You can also provide the output path for the XML:

```csharp
using Proligent.XmlGenerator;

var warehouse = new DataWareHouse();
warehouse.SaveXml(destination: @"C:\path_to\Proligent_file_name.xml");
```

### Example 2 (top-down)

```csharp
using Proligent.XmlGenerator;

var warehouse = new DataWareHouse();

var product = warehouse.SetProductUnit(
    new ProductUnit(
        productUnitIdentifier: "UutSerialNumber",
        productFullName: "Product/readme_example",
        manufacturer: "Averna"));

var process = warehouse.SetProcessRun(
    new ProcessRun(
        name: "Process/readme_example",
        processMode: "PROD",
        productUnitIdentifier: "DutSerialNumber",
        productFullName: "Product/readme_example"));

var operation = process.AddOperationRun(
    new OperationRun(
        station: "Station/readme_example",
        name: "Operation1"));

var sequence = operation.AddSequenceRun(new SequenceRun(name: "Sequence1"));
sequence.AddStepRun(
    new StepRun(
        name: "Step1",
        status: ExecutionStatusKind.PASS,
        measure: new Measure(
            value: 15,
            time: DateTime.UtcNow,
            status: ExecutionStatusKind.PASS,
            limit: new Limit(
                LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
                lowerBound: 10,
                higherBound: 25))));

sequence.Complete(ExecutionStatusKind.PASS);
operation.Complete(ExecutionStatusKind.PASS);
process.Complete(ExecutionStatusKind.PASS);
warehouse.SaveXml();
```

### About Names

Most of the "names" (product, station, etc.) in the model can be simple strings,
or can be built in hierarchies.

The goal is to allow grouping of items in meaningful categories, which can be
useful when selecting items for display or reporting.

Note that the last item in the full name (the "leaf" of the tree) must be
meaningful: some reports display only the leaf, for simplicity and brevity. In
most cases the full name is also available, but may be less visible (tooltips).

#### Example: Stations

`Country/ManufacturerName/ProductionLine/StationName`

Other items in the full station name could be: city, station type, etc.

For stations that can test multiple units in parallel: see `testPositionName`.

#### Example: Products

`ProductFamily/ProductName/PartNumber`

We don't recommend having a version or revision as the leaf of the full product
name. This is best recorded as a characteristic, either on the product unit or
the operation.

### Sequence "Tree"

All sequences are added to the operation. However, typically sequences are
organized in "trees".

```text
MainSequence1
    SubSequence1
        SubSubSeq1
        SubSubSeq2
    SubSequence2
MainSequence2
    etc.
```

To keep the sequences organized in trees like the example above, the sequence
names need to include all nodes of the tree, separated by `/`.

```csharp
operation.AddSequenceRun(new SequenceRun(name: "MainSequence1/SubSequence1/SubSubSeq1"));
operation.AddSequenceRun(new SequenceRun(name: "MainSequence1/SubSequence1/SubSubSeq2"));
operation.AddSequenceRun(new SequenceRun(name: "MainSequence1/SubSequence2"));
operation.AddSequenceRun(new SequenceRun(name: "MainSequence2"));
// etc.
```

It is not necessary to create all nodes (e.g. `MainSequence1` and
`MainSequence1/SubSequence1`). However, they can also be created if they need to
hold steps, characteristics, or documents.

Also note that the full sequence name is limited to 2000 characters, and each
part to 64 characters.

### Process Run ID

By default, the `ProcessRunId` is automatically generated as a deterministic ID
based on `productFullName`, `productUnitIdentifier`, `processMode`, and a
configurable fixed process start time (default: `2000-01-01`).

This ensures that multiple operation runs within the same process refer to the
same process run, which is critical for accurate reporting in Proligent.

If you need a fixed ID regardless of field values, you can optionally set the
`id` parameter directly when constructing the `ProcessRun`.

### File Names

The XML files must have the prefix `Proligent_`.

The documents attached must have the prefix `Document_`.

In case of compressed documents, `CompressedDocument_`.

### XML Validation

```csharp
using Proligent.XmlGenerator;

// Throws if validation fails
XmlValidator.ValidateXml(@"C:\path_to\Proligent_file_name.xml");

// Safe call returns metadata
var metadata = XmlValidator.ValidateXmlSafe(@"C:\path_to\Proligent_file_name.xml");
if (!metadata.IsValid)
{
    Console.WriteLine(metadata.Path);
    Console.WriteLine(metadata.Reason);
}
```

### Configuration

Some parameters are configurable via the `Util` helper:

- `DestinationDirectory`: target folder when `SaveXml` has no explicit path. Defaults to `C:\Proligent\IntegrationService\Acquisition`.
- `TimeZone`: set with either `TimeZoneInfo` or `SetTimeZone(string id)` for IANA/Windows IDs.
- `SchemaPath`: override the default XSD location for validation.

```csharp
var util = new Util(timeZoneId: "America/New_York");
DataWareHouse warehouse = new(topProcess: process);
warehouse.SaveXml(util: util);
```

## Scripts and docs

- `scripts/generate-help.ps1`: builds the project and emits IntelliSense XML + a lightweight HTML help snapshot.

## Trademarks

Proligent is a registered trademark, and Averna is a trademark, of [Averna Technologies Inc.][web-site]

[web-site]: https://www.averna.com

Other product and company names mentioned herein are trademarks or trade names
of their respective companies.
