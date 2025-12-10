# Quickstart

Install the NuGet package:

```bash
dotnet add package Proligent.XmlGenerator
```

Generate a simple process/operation/sequence/step hierarchy and save it to XML:

```csharp
using Proligent.XmlGenerator;

var measure = new Measure(
    value: 15,
    status: ExecutionStatusKind.PASS,
    limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 10, higherBound: 25));

var step = new StepRun(name: "Step1", status: ExecutionStatusKind.PASS, measure: measure);
var sequence = new SequenceRun(name: "Sequence1", status: ExecutionStatusKind.PASS);
sequence.AddStepRun(step);

var operation = new OperationRun(
    station: "Station/Example",
    sequences: new[] { sequence },
    name: "Operation1",
    status: ExecutionStatusKind.PASS);

var process = new ProcessRun(
    name: "Process/Example",
    productUnitIdentifier: "Unit-1",
    productFullName: "Product/Example",
    operations: new[] { operation },
    status: ExecutionStatusKind.PASS);

var warehouse = new DataWareHouse(topProcess: process);
warehouse.SaveXml(); // defaults to C:\Proligent\IntegrationService\Acquisition
```

Validate the generated XML using the bundled XSDs:

```csharp
XmlValidator.ValidateXml(@"C:\Proligent\IntegrationService\Acquisition\Proligent_<guid>.xml");
```

For more examples, see the fixtures in `test/Proligent.XmlGenerator.Tests/Expected` and the README.
