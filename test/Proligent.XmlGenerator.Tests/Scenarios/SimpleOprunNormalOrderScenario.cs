namespace Proligent.XmlGenerator.Tests.Scenarios;

public class SimpleOprunNormalOrderXmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate(DateTime? startTimestamp = null)
    {
        Util tzUtil = new Util(timeZoneId: "America/New_York");
        DateTime start = startTimestamp ?? new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);
        DateTime processStart     = start;
        DateTime processEnd       = start.AddMinutes(20);
        DateTime sequenceStart    = start.AddMinutes(10);
        DateTime sequenceEnd      = start.AddMinutes(12);
        DateTime inspectionTime   = start.AddMinutes(10).AddSeconds(30);
        DateTime countTime        = start.AddMinutes(10).AddSeconds(45);
        DateTime visualTime       = start.AddMinutes(11);
        DateTime calibrationTime  = start.AddMinutes(11).AddSeconds(15);
        DateTime notesTime        = start.AddMinutes(11).AddSeconds(30);
        DateTime generationTime   = start.AddHours(1);

        DataWareHouse warehouse = new DataWareHouse(generationTime: generationTime);

        ProductUnit productUnit = warehouse.SetProductUnit(new ProductUnit(
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_normal_order"
        ));
        productUnit.AddCharacteristic(new Characteristic("Serial", "PU-001"));
        productUnit.AddDocument(new Document(
            fileName: "ProductCertificate.pdf",
            name: "Certificate of Conformance",
            description: "Certification for product PU-001"
        ));

        ProcessRun process = warehouse.SetProcessRun(new ProcessRun(
            name: "XmlGenerator/Process/simple_oprun_normal_order",
            processMode: "PROD",
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_normal_order",
            startTime: processStart
        ));

        OperationRun operation = process.AddOperationRun(new OperationRun(
            station: "XmlGenerator/Station/simple_oprun_normal_order",
            name: "Operation/Example",
            user: "operator",
            startTime: processStart
        ));
        operation.AddCharacteristic(new Characteristic("Batch", "B-42"));
        operation.AddDocument(new Document(
            fileName: "OperationReport.pdf",
            name: "Operation Report",
            description: "Summary for operation Operation/Example"
        ));

        SequenceRun sequence = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/Main",
            version: "1.0",
            startTime: sequenceStart
        ));
        sequence.AddCharacteristic(new Characteristic("SequenceType", "Main"));
        sequence.AddDocument(new Document(
            fileName: "SequenceChecklist.pdf",
            name: "Sequence Checklist",
            description: "Checklist completed before running the sequence"
        ));

        sequence.AddStepRun(new StepRun(
            name: "Inspection",
            status: ExecutionStatusKind.PASS,
            startTime: inspectionTime,
            endTime: inspectionTime,
            measure: Measure.Create(1.23, new MeasureOptions(
                Time: inspectionTime,
                Unit: "Volt",
                Symbol: "V",
                Status: ExecutionStatusKind.PASS,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 1.1, higherBound: 1.25)
            )),
            characteristics: new[] { new Characteristic("Channel", "A") },
            documents: new[] { new Document(fileName: "InspectionReport.pdf", name: "Inspection Report", description: "Results for the inspection step") }
        ));

        sequence.AddStepRun(new StepRun(
            name: "CountCheck",
            status: ExecutionStatusKind.PASS,
            startTime: countTime,
            endTime: countTime,
            measure: Measure.Create(42, new MeasureOptions(Time: countTime, Status: ExecutionStatusKind.PASS))
        ));

        sequence.AddStepRun(new StepRun(
            name: "VisualApproval",
            status: ExecutionStatusKind.PASS,
            startTime: visualTime,
            endTime: visualTime,
            measure: Measure.Create(true, new MeasureOptions(Time: visualTime, Status: ExecutionStatusKind.PASS))
        ));

        sequence.AddStepRun(new StepRun(
            name: "CalibrationTimestamp",
            status: ExecutionStatusKind.PASS,
            startTime: calibrationTime,
            endTime: calibrationTime,
            measure: Measure.Create(calibrationTime, new MeasureOptions(Time: calibrationTime, Unit: "Volt", Symbol: "V", Status: ExecutionStatusKind.PASS))
        ));

        sequence.AddStepRun(new StepRun(
            name: "OperatorNotes",
            status: ExecutionStatusKind.PASS,
            startTime: notesTime,
            endTime: notesTime,
            measure: Measure.Create("All checks passed", new MeasureOptions(Time: notesTime, Status: ExecutionStatusKind.PASS))
        ));

        sequence.Complete(ExecutionStatusKind.PASS, sequenceEnd);
        operation.Complete(ExecutionStatusKind.PASS, processEnd);
        process.Complete(ExecutionStatusKind.PASS, processEnd);

        return new ScenarioResult(warehouse, tzUtil);
    }
}
