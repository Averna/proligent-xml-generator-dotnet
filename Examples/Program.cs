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
var measure = new Measure(
    value: 111,
    status: ExecutionStatusKind.PASS,
    limit: limit,
    time: DateTime.UtcNow
);
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
        measure: new Measure(
            value: 222,
            time: DateTime.UtcNow,
            status: ExecutionStatusKind.PASS,
            limit: new Limit(
                LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
                lowerBound: 200,
                higherBound: 250
            )
        )
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
