namespace Proligent.XmlGenerator.Tests.Scenarios;

public class ReadmeExample2XmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate(DateTime? startTimestamp = null)
    {
        Util tzUtil = new Util(timeZoneId: "Europe/Paris");
        DateTime instant = startTimestamp ?? new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        DataWareHouse warehouse = new DataWareHouse(generationTime: instant);

        warehouse.SetProductUnit(new ProductUnit(
            productUnitIdentifier: "DutSerialNumber",
            productFullName: "Product/readme_example2",
            manufacturer: "Averna"
        ));

        ProcessRun process = warehouse.SetProcessRun(new ProcessRun(
            name: "Process/readme_example2",
            processMode: "PROD",
            productUnitIdentifier: "DutSerialNumber",
            productFullName: "Product/readme_example2",
            startTime: instant
        ));

        OperationRun operation = process.AddOperationRun(new OperationRun(
            station: "Station/readme_example2",
            name: "Operation1",
            startTime: instant
        ));

        SequenceRun sequence = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence1",
            startTime: instant
        ));

        sequence.AddStepRun(new StepRun(
            name: "Step1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant,
            measure: Measure.Create(15, new MeasureOptions(
                Time: instant,
                Status: ExecutionStatusKind.PASS,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 10, higherBound: 25)
            ))
        ));

        sequence.Complete(ExecutionStatusKind.PASS, instant);
        operation.Complete(ExecutionStatusKind.PASS, instant);
        process.Complete(ExecutionStatusKind.PASS, instant);

        return new ScenarioResult(warehouse, tzUtil);
    }
}
