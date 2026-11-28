using Proligent.XmlGenerator;

string? destinationFolder = null;

// If the variable is null, the file is written to the default path
// C:\Proligent\IntegrationService\Acquisition\Proligent_<guid>.xml.
// To override this behavior, uncomment the following line and specify your custom output directory.
//destinationFolder = @"C:\";

////////////////////////
// Example 1
////////////////////////

var limit = new Limit(
    LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
    lowerBound: 100,
    higherBound: 120
);
var measure = Measure.Create(111, new MeasureOptions(
    Status: ExecutionStatusKind.PASS,
    Limit: limit,
    Time: DateTime.UtcNow
));
var step = new StepRun(name: "Step1", status: ExecutionStatusKind.PASS, measure: measure);

var sequence = new SequenceRun(name: "Sequence1", status: ExecutionStatusKind.PASS);
sequence.AddStepRun(step);
var operation = new OperationRun(
    station: "Station/readme_example",
    sequences: new[] { sequence },
    name: "Operation1",
    status: ExecutionStatusKind.PASS
);
var process = new ProcessRun(
    productUnitIdentifier: "DutSerialNumber",
    productFullName: "Product/readme_example",
    operations: new[] { operation },
    name: "Process/readme_example",
    processMode: "PROD",
    status: ExecutionStatusKind.PASS
);

var product = new ProductUnit(
    productUnitIdentifier: "DutSerialNumber",
    productFullName: "Product/readme_example",
    manufacturer: "Averna"
);
var warehouse = new DataWareHouse(topProcess: process, productUnit: product);
string example1File = warehouse.SaveXml(destinationFolder: destinationFolder);

// validation - optional
var validation = XmlValidator.ValidateXmlSafe(example1File);
Console.WriteLine($"Example 1 is valid:{validation.IsValid}");
if (!validation.IsValid)
{
    Console.WriteLine($"{validation.Message}");
}

////////////////////////
// Example 2 (top-down)
////////////////////////

var warehouse2 = new DataWareHouse();

var product2 = warehouse2.SetProductUnit(
    new ProductUnit(
        productUnitIdentifier: "UutSerialNumber",
        productFullName: "Product/readme_example",
        manufacturer: "Averna"
    )
);

var process2 = warehouse2.SetProcessRun(
    new ProcessRun(
        name: "Process/readme_example",
        processMode: "PROD",
        productUnitIdentifier: "DutSerialNumber",
        productFullName: "Product/readme_example"
    )
);

var operation2 = process2.AddOperationRun(
    new OperationRun(station: "Station/readme_example", name: "Operation1")
);

var sequence2 = operation2.AddSequenceRun(new SequenceRun(name: "Sequence2"));
sequence2.AddStepRun(
    new StepRun(
        name: "Step2",
        status: ExecutionStatusKind.PASS,
        measure: Measure.Create(222, new MeasureOptions(
            Time: DateTime.UtcNow,
            Status: ExecutionStatusKind.PASS,
            Limit: new Limit(
                LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
                lowerBound: 200,
                higherBound: 250
            )
        ))
    )
);

sequence2.Complete(ExecutionStatusKind.PASS);
operation2.Complete(ExecutionStatusKind.PASS);
process2.Complete(ExecutionStatusKind.PASS);

string example2File = warehouse2.SaveXml(destinationFolder: destinationFolder);

// validation - optional
var validation2 = XmlValidator.ValidateXmlSafe(example2File);
Console.WriteLine($"Example 2 is valid:{validation2.IsValid}");
if (!validation2.IsValid)
{
    Console.WriteLine($"{validation2.Message}");
}
////////////////////////
// Example 3 (assembly traces)
////////////////////////
var warehouse3 = new DataWareHouse(
    topProcess: process,
    productUnit: new ProductUnit("ParentSerial", "Product/Parent", manufacturer: "Averna")
);
warehouse3.AddProductUnit(new ProductUnit("ChildSerial", "Product/Child", manufacturer: "Averna"));
var parent = new AssemblyTraceParent("Product/Parent", "ParentSerial", referenceDesignator: "U1");
// Product-unit child, linked to an existing operation run by its object.
warehouse3.AddAssemblyTrace(
    new AssemblyTrace(
        parent,
        AssemblyTraceChild.FromProductUnit("Product/Child", "ChildSerial"),
        AssemblyAction.ASSEMBLE,
        actionTime: new DateTime(2025, 3, 4, 8, 30, 0),
        context: AssemblyTraceContext.FromOperationRun(operation),
        stationFullName: "Station/readme_example",
        operatorName: "Jane"
    )
);
// Supplier component child, disassembled 4 units, no execution context.
warehouse3.AddAssemblyTrace(
    new AssemblyTrace(
        parent,
        AssemblyTraceChild.FromSupplierComponent(
            manufacturer: "Acme",
            manufacturerPartNumber: "PN-100",
            componentFullName: "Component/Resistor",
            manufacturerLot: "LOT-7"
        ),
        AssemblyAction.DISASSEMBLE,
        actionTime: new DateTime(2025, 3, 5, 9, 0, 0),
        childComponentCount: 4
    )
);
// SET_CURRENT: time the existing assembly was observed and entered, not when it was built.
warehouse3.AddAssemblyTrace(
    new AssemblyTrace(
        parent,
        AssemblyTraceChild.FromSupplierComponent("Acme", "PN-200", "Component/Chip", manufacturerSerialNumber: "SN-1"),
        AssemblyAction.SET_CURRENT,
        actionTime: new DateTime(2025, 3, 6, 10, 0, 0)
    )
);
string example3File = warehouse3.SaveXml(destinationFolder: destinationFolder);
var validation3 = XmlValidator.ValidateXmlSafe(example3File);
Console.WriteLine($"Example 3 is valid:{validation3.IsValid}");
if (!validation3.IsValid)
{
    Console.WriteLine($"{validation3.Message}");
}


