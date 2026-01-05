using Proligent.XmlGenerator;

string? destinationFolder = null;
// If the variable is null, the file is written to the default path
// C:\Proligent\IntegrationService\Acquisition\Proligent_<guid>.xml.
// To override this behavior, uncomment the following line and specify your custom output directory.
destinationFolder = @"C:\temp\Proligent";


var val = XmlValidator.ValidateXmlSafe(
    @"c:\Proligent\IntegrationService\Acquisition\Processing\Proligent_Test_RF_1_ff5c7082-d832-4ab4-93a5-aafa5437ca25.xml");

return;
////////////////////////
// Example 1
////////////////////////

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
string example1file = warehouse.SaveXml(destinationFolder: destinationFolder); 

// validation 
var validation = XmlValidator.ValidateXmlSafe(example1file);
Console.WriteLine($"Example 1 validation result:{validation.IsValid}");
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
        manufacturer: "Averna"));

var process2 = warehouse2.SetProcessRun(
    new ProcessRun(
        name: "Process/readme_example",
        processMode: "PROD",
        productUnitIdentifier: "DutSerialNumber",
        productFullName: "Product/readme_example"));

var operation2 = process2.AddOperationRun(
    new OperationRun(
        station: "Station/readme_example",
        name: "Operation1"));

var sequence2 = operation2.AddSequenceRun(new SequenceRun(name: "Sequence1"));
sequence2.AddStepRun(
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

sequence2.Complete(ExecutionStatusKind.PASS);
operation2.Complete(ExecutionStatusKind.PASS);
process2.Complete(ExecutionStatusKind.PASS);

string example2file = warehouse2.SaveXml(destinationFolder: destinationFolder);

// validation 
var validation2 = XmlValidator.ValidateXmlSafe(example2file);
Console.WriteLine($"Example 1 validation result:{validation2.IsValid}");
if (!validation2.IsValid)
{
    Console.WriteLine($"{validation2.Message}");
}