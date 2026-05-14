namespace Proligent.XmlGenerator.Tests.Scenarios;

public class PartialOperationRunXmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate()
    {
        Util tzUtil = new Util(timeZoneId: "America/Chicago");
        DateTime start          = new DateTime(2024, 5, 5, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime generationTime = start.AddHours(15);
        DateTime processStart   = start.AddHours(14);

        DataWareHouse warehouse = new DataWareHouse(generationTime: generationTime);

        warehouse.SetProductUnit(new ProductUnit(
            productUnitIdentifier: "PU-PARTIAL-01",
            productFullName: "XmlGenerator/Product/partial_operation_flow"
        ));

        ProcessRun process = warehouse.SetProcessRun(new ProcessRun(
            name: "XmlGenerator/Process/partial_operation_flow",
            processMode: "AUTO",
            productUnitIdentifier: "PU-PARTIAL-01",
            productFullName: "XmlGenerator/Product/partial_operation_flow",
            status: ExecutionStatusKind.NOT_COMPLETED,
            startTime: processStart
        ));

        OperationRun operation = process.AddOperationRun(new OperationRun(
            station: "XmlGenerator/Station/partial_operation_flow",
            name: "Operation/Partial",
            user: "in-progress.operator",
            status: ExecutionStatusKind.NOT_COMPLETED,
            startTime: processStart
        ));

        SequenceRun initialSeq = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/Initial",
            version: "INIT-1.0",
            user: "in-progress.operator",
            status: ExecutionStatusKind.PASS,
            startTime: start.AddHours(14).AddMinutes(5),
            endTime: start.AddHours(14).AddMinutes(15)
        ));

        DateTime measureTime = start.AddHours(14).AddMinutes(7);
        initialSeq.AddStepRun(new StepRun(
            name: "InitialStep",
            status: ExecutionStatusKind.PASS,
            startTime: measureTime,
            endTime: measureTime,
            measure: Measure.Create(42.0, new MeasureOptions(Unit: "Volt", Symbol: "V", Time: measureTime, Status: ExecutionStatusKind.PASS))
        ));

        SequenceRun ongoingSeq = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/Ongoing",
            version: "ONGOING-1.0",
            user: "in-progress.operator",
            status: ExecutionStatusKind.NOT_COMPLETED,
            startTime: start.AddHours(14).AddMinutes(35)
        ));

        DateTime ongoingMeasureTime = start.AddHours(14).AddMinutes(36);
        ongoingSeq.AddStepRun(new StepRun(
            name: "OngoingMeasurement",
            status: ExecutionStatusKind.PASS,
            startTime: ongoingMeasureTime,
            endTime: ongoingMeasureTime,
            measure: Measure.Create("Collecting data", new MeasureOptions(Time: ongoingMeasureTime, Status: ExecutionStatusKind.PASS))
        ));

        return new ScenarioResult(warehouse, tzUtil);
    }
}
