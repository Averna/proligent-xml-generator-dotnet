namespace Proligent.XmlGenerator.Tests.Scenarios;

public class SimpleOprunReverseOrderXmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate()
    {
        Util tzUtil = new Util(timeZoneId: "America/New_York");
        DateTime start = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);
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

        StepRun[] steps = new[]
        {
            new StepRun(
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
            ),
            new StepRun(
                name: "CountCheck",
                status: ExecutionStatusKind.PASS,
                startTime: countTime,
                endTime: countTime,
                measure: Measure.Create(42, new MeasureOptions(Time: countTime, Status: ExecutionStatusKind.PASS))
            ),
            new StepRun(
                name: "VisualApproval",
                status: ExecutionStatusKind.PASS,
                startTime: visualTime,
                endTime: visualTime,
                measure: Measure.Create(true, new MeasureOptions(Time: visualTime, Status: ExecutionStatusKind.PASS))
            ),
            new StepRun(
                name: "CalibrationTimestamp",
                status: ExecutionStatusKind.PASS,
                startTime: calibrationTime,
                endTime: calibrationTime,
                measure: Measure.Create(calibrationTime, new MeasureOptions(Time: calibrationTime, Unit: "Volt", Symbol: "V", Status: ExecutionStatusKind.PASS))
            ),
            new StepRun(
                name: "OperatorNotes",
                status: ExecutionStatusKind.PASS,
                startTime: notesTime,
                endTime: notesTime,
                measure: Measure.Create("All checks passed", new MeasureOptions(Time: notesTime, Status: ExecutionStatusKind.PASS))
            ),
        };

        SequenceRun sequence = new SequenceRun(
            steps: steps,
            name: "Sequence/Main",
            version: "1.0",
            user: "operator",
            status: ExecutionStatusKind.PASS,
            startTime: sequenceStart,
            endTime: sequenceEnd,
            characteristics: new[] { new Characteristic("SequenceType", "Main") },
            documents: new[] { new Document(fileName: "SequenceChecklist.pdf", name: "Sequence Checklist", description: "Checklist completed before running the sequence") }
        );

        OperationRun operation = new OperationRun(
            station: "XmlGenerator/Station/simple_oprun_reverse_order",
            sequences: new[] { sequence },
            name: "Operation/Example",
            user: "operator",
            processName: "XmlGenerator/Process/simple_oprun_reverse_order",
            status: ExecutionStatusKind.PASS,
            startTime: processStart,
            endTime: processEnd,
            characteristics: new[] { new Characteristic("Batch", "B-42") },
            documents: new[] { new Document(fileName: "OperationReport.pdf", name: "Operation Report", description: "Summary for operation Operation/Example") }
        );

        ProcessRun process = new ProcessRun(
            name: "XmlGenerator/Process/simple_oprun_reverse_order",
            processMode: "PROD",
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_reverse_order",
            operations: new[] { operation },
            status: ExecutionStatusKind.PASS,
            startTime: processStart,
            endTime: processEnd
        );

        ProductUnit productUnit = new ProductUnit(
            productUnitIdentifier: "PU-001",
            productFullName: "XmlGenerator/Product/simple_oprun_reverse_order",
            characteristics: new[] { new Characteristic("Serial", "PU-001") },
            documents: new[] { new Document(fileName: "ProductCertificate.pdf", name: "Certificate of Conformance", description: "Certification for product PU-001") }
        );

        DataWareHouse warehouse = new DataWareHouse(
            topProcess: process,
            productUnit: productUnit,
            generationTime: generationTime
        );

        return new ScenarioResult(warehouse, tzUtil);
    }
}
