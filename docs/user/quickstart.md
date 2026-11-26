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
## Assembly traces (product genealogy)
Use `AssemblyTrace` to export assembly, disassembly and "current assembly" facts as top-level `AssemblyTrace` elements.
```csharp
var warehouse = new DataWareHouse(productUnit: new ProductUnit("SN-PARENT", "Product/Parent"));
warehouse.AddProductUnit(new ProductUnit("SN-CHILD", "Product/Child"));
// Product-unit child
warehouse.AddAssemblyTrace(new AssemblyTrace(
    new AssemblyTraceParent("Product/Parent", "SN-PARENT", referenceDesignator: "U1"),
    AssemblyTraceChild.FromProductUnit("Product/Child", "SN-CHILD"),
    AssemblyAction.ASSEMBLE,
    new DateTime(2025, 3, 4, 5, 6, 7),
    context: AssemblyTraceContext.FromOperationRunId("11111111-2222-3333-4444-555555555555"),
    stationFullName: "Root/Station",
    operatorName: "Jane"));
// Supplier component child (lot and serial are optional), quantity 4
warehouse.AddAssemblyTrace(new AssemblyTrace(
    new AssemblyTraceParent("Product/Parent", "SN-PARENT"),
    AssemblyTraceChild.FromSupplierComponent("Acme", "PN-1", "Comp/Name", manufacturerLot: "LOT7"),
    AssemblyAction.DISASSEMBLE,
    new DateTime(2025, 3, 5, 8, 0, 0),
    childComponentCount: 4));
```
Notes:
- The action time is mandatory and never defaults to the current time. It is serialized with a numeric timezone offset.
- `SET_CURRENT` records when an existing assembly was observed and entered in the system, not necessarily when it was physically assembled.
- A trace may omit its context or reference exactly one operation run or sequence run (from an object or an id). Referenced runs are not built or exported automatically.
- `ChildComponentCount` defaults to 1, accepts 0 and cannot be negative.
- Adding a trace does not create product records. Use `AddProductUnit()` to export additional products after the primary one (set via the constructor or `SetProductUnit()`).
- Traces are immutable facts in Proligent, identified by parent (including reference designator), child, action and action time. They cannot be updated or deleted.
- Not supported: tracked-component associations, custom data, assembly rules. XSD validity does not guarantee ingestion by every DIT version.
