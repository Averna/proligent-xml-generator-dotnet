# Proligentƒ™ XML Generator for .NET

.NET library for generating Proligent™ XML files. It provides a simple,
structured API for building valid import files, reducing manual XML writing and
ensuring consistent data formatting. These files are used to import data into
[Proligent™ Cloud][cloud] and [Proligent™ Analytics][analytics].

[cloud]: https://www.averna.com/en/products/smart-data-management/proligent-cloud
[analytics]: https://www.averna.com/en/products/proligent-analytics

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

### XML Validation

```csharp
using Proligent.XmlGenerator;

// Throws if validation fails
XmlValidator.ValidateXml(@"C:\path_to\Proligent_file_name.xml");

// Safe call returns metadata
var (isValid, metadata) = XmlValidator.ValidateXmlSafe(@"C:\path_to\Proligent_file_name.xml");
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
- `mkdocs.yml`: documentation site powered by mkdocs-material and mkdocstrings-csharp (published via GitHub Pages).

## Trademarks

Proligent is a registered trademark, and Averna is a trademark, of [Averna Technologies Inc.][web-site]

[web-site]: https://www.averna.com

Other product and company names mentioned herein are trademarks or trade names
of their respective companies.
