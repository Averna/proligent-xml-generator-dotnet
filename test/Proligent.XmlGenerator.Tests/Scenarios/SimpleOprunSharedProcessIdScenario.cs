namespace Proligent.XmlGenerator.Tests.Scenarios;

public class SimpleOprunSharedProcessIdXmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate()
    {
        Util tzUtil = new Util(timeZoneId: "America/New_York");
        DateTime start = new DateTime(2024, 1, 2, 9, 0, 0, DateTimeKind.Unspecified);
        DateTime processStart   = start;
        DateTime processEnd     = start.AddMinutes(25);
        DateTime sequenceStart  = start.AddMinutes(5);
        DateTime sequenceEnd    = start.AddMinutes(18);
        DateTime continuityTime = start.AddMinutes(6);
        DateTime torqueTime     = start.AddMinutes(9).AddSeconds(15);
        DateTime firmwareTime   = start.AddMinutes(12).AddSeconds(30);
        DateTime labelTime      = start.AddMinutes(15).AddSeconds(45);
        DateTime generationTime = start.AddHours(1);

        DataWareHouse warehouse = new DataWareHouse(generationTime: generationTime);

        ProductUnit productUnit = warehouse.SetProductUnit(new ProductUnit(
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_normal_order"
        ));
        productUnit.AddCharacteristic(new Characteristic("Serial", "PU-001"));

        ProcessRun process = warehouse.SetProcessRun(new ProcessRun(
            name: "XmlGenerator/Process/simple_oprun_normal_order",
            processMode: "PROD",
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_normal_order",
            startTime: processStart
        ));

        OperationRun operation = process.AddOperationRun(new OperationRun(
            station: "XmlGenerator/Station/simple_oprun_shared_process_id",
            name: "Operation/ExtendedValidation",
            user: "operator_b",
            startTime: processStart
        ));
        operation.AddCharacteristic(new Characteristic("Lot", "L-9001"));
        operation.AddDocument(new Document(
            fileName: "ValidationPacket.pdf",
            name: "Validation Packet",
            description: "Evidence package for extended validation operation"
        ));

        SequenceRun sequence = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/Secondary",
            version: "2.1",
            user: "operator_b",
            startTime: sequenceStart
        ));

        sequence.AddStepRun(new StepRun(
            name: "Continuity",
            status: ExecutionStatusKind.PASS,
            startTime: continuityTime,
            endTime: continuityTime,
            measure: Measure.Create(true, new MeasureOptions(Time: continuityTime, Status: ExecutionStatusKind.PASS)),
            characteristics: new[] { new Characteristic("Fixture", "FX-7") }
        ));

        sequence.AddStepRun(new StepRun(
            name: "TorqueAudit",
            status: ExecutionStatusKind.PASS,
            startTime: torqueTime,
            endTime: torqueTime,
            measure: Measure.Create(5.8, new MeasureOptions(
                Time: torqueTime,
                Unit: "Nm",
                Symbol: "Nm",
                Status: ExecutionStatusKind.PASS,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 5.5, higherBound: 6.1)
            ))
        ));

        sequence.AddStepRun(new StepRun(
            name: "FirmwareCheck",
            status: ExecutionStatusKind.PASS,
            startTime: firmwareTime,
            endTime: firmwareTime,
            measure: Measure.Create("FW-1.2.3", new MeasureOptions(Time: firmwareTime, Status: ExecutionStatusKind.PASS))
        ));

        sequence.AddStepRun(new StepRun(
            name: "LabelScan",
            status: ExecutionStatusKind.PASS,
            startTime: labelTime,
            endTime: labelTime,
            measure: Measure.Create(1001, new MeasureOptions(Time: labelTime, Status: ExecutionStatusKind.PASS)),
            documents: new[] { new Document(fileName: "LabelImage.png", name: "Label Image", description: "Captured label image for traceability") }
        ));

        sequence.Complete(ExecutionStatusKind.PASS, sequenceEnd);
        operation.Complete(ExecutionStatusKind.PASS, processEnd);
        process.Complete(ExecutionStatusKind.PASS, processEnd);

        return new ScenarioResult(warehouse, tzUtil);
    }
}
