namespace Proligent.XmlGenerator.Tests.Scenarios;

public class ComplexOprunXmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate(DateTime? startTimestamp = null)
    {
        Util tzUtil = new Util(timeZoneId: "America/Bogota");
        DateTime start = startTimestamp ?? new DateTime(2024, 4, 1, 7, 0, 0, DateTimeKind.Unspecified);
        DateTime generationTime   = start.AddHours(1);
        DateTime processStart     = start;
        DateTime processEnd       = start.AddMinutes(50);
        DateTime functionalStart  = start.AddMinutes(10);
        DateTime functionalEnd    = start.AddMinutes(27);
        DateTime safetyStart      = start.AddMinutes(15);
        DateTime safetyEnd        = start.AddMinutes(23);
        DateTime diagnosticsStart = start.AddMinutes(32);
        DateTime diagnosticsEnd   = start.AddMinutes(47);

        DataWareHouse warehouse = new DataWareHouse(generationTime: generationTime);

        warehouse.SetProductUnit(new ProductUnit(
            productUnitIdentifier: "PU-COMP-999",
            productFullName: "XmlGenerator/Product/complex_oprun",
            manufacturer: "Averna",
            creationTime: processStart,
            manufacturingTime: processStart,
            scrapped: true,
            scrapTime: processEnd,
            characteristics: new[]
            {
                new Characteristic("Variant", "Ultimate-RevD"),
                new Characteristic("Lot", "LOT-5566"),
            }
        ));

        ProcessRun process = warehouse.SetProcessRun(new ProcessRun(
            name: "XmlGenerator/Process/complex_oprun",
            version: "2.0",
            processMode: "AUTO",
            productUnitIdentifier: "PU-COMP-999",
            productFullName: "XmlGenerator/Product/complex_oprun",
            startTime: processStart
        ));

        OperationRun operation = process.AddOperationRun(new OperationRun(
            station: "XmlGenerator/Station/complex_oprun",
            name: "Operation/Comprehensive",
            user: "chief.operator",
            testPositionName: "UUT1",
            startTime: processStart,
            characteristics: new[]
            {
                new Characteristic("Shift", "Night"),
                new Characteristic("Technician", "Charlie"),
                new Characteristic("FinalStatus", "Repaired"),
            },
            documents: new[]
            {
                new Document(fileName: "ComprehensiveOperationLog.pdf", name: "Operation Log", description: "Aggregated log for Operation/Comprehensive failure event."),
            }
        ));

        // Functional sequence
        SequenceRun functionalSeq = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/FunctionalTest",
            version: "FT-3.2",
            user: "chief.operator",
            startTime: functionalStart,
            characteristics: new[] { new Characteristic("Fixture", "FT-FX-22") },
            documents: new[] { new Document(fileName: "FunctionalProcedure.pdf", name: "Functional Test Procedure", description: "Checklist reviewed before running functional test.") }
        ));

        DateTime t = functionalStart.AddMinutes(1);
        functionalSeq.AddStepRun(new StepRun(
            name: "InitialPowerUp",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create(t, new MeasureOptions(Time: t, Status: ExecutionStatusKind.PASS)),
            characteristics: new[] { new Characteristic("VoltageRange", "nominal") }
        ));

        t = functionalStart.AddMinutes(12);
        functionalSeq.AddStepRun(new StepRun(
            name: "FunctionalSummary",
            status: ExecutionStatusKind.FAIL,
            startTime: t,
            endTime: t,
            measure: Measure.Create("Errors Logged", new MeasureOptions(Time: t, Status: ExecutionStatusKind.FAIL)),
            characteristics: new[] { new Characteristic("SummaryCode", "ERR-871") }
        ));

        functionalSeq.Complete(ExecutionStatusKind.FAIL, functionalEnd);

        // Safety sequence
        SequenceRun safetySeq = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/FunctionalTest/SubSequence/SafetyChecks",
            version: "SC-1.0",
            user: "chief.operator",
            startTime: safetyStart,
            characteristics: new[] { new Characteristic("Inspector", "QA-123") },
            documents: new[] { new Document(fileName: "SafetyChecklist.pdf", name: "Safety Checklist", description: "QA inspector notes for safety evaluation.") }
        ));

        t = safetyStart.AddMinutes(1);
        safetySeq.AddStepRun(new StepRun(
            name: "GroundContinuity",
            status: ExecutionStatusKind.FAIL,
            startTime: t,
            endTime: t,
            measure: Measure.Create(false, new MeasureOptions(Time: t, Status: ExecutionStatusKind.FAIL)),
            characteristics: new[] { new Characteristic("Threshold", "0.5 Ohm") }
        ));

        t = safetyStart.AddMinutes(3);
        safetySeq.AddStepRun(new StepRun(
            name: "RawMeasure",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create(123.456789, new MeasureOptions(
                Time: t,
                Unit: "Amp",
                Symbol: "A",
                Precision: 3,
                Status: ExecutionStatusKind.PASS,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 100.0, higherBound: 130.0)
            ))
        ));
        safetySeq.AddStepRun(new StepRun(
            name: "OverCurrentDetection",
            status: ExecutionStatusKind.FAIL,
            startTime: t,
            endTime: t,
            measure: Measure.Create(12.5, new MeasureOptions(
                Time: t,
                Unit: "Amp",
                Symbol: "A",
                Precision: 3,
                Status: ExecutionStatusKind.FAIL,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 0.0, higherBound: 10.0)
            )),
            documents: new[] { new Document(fileName: "OverCurrentTrace.png", name: "Oscilloscope Capture", description: "Trace captured during over-current failure.") }
        ));

        t = safetyStart.AddMinutes(5);
        safetySeq.AddStepRun(new StepRun(
            name: "AlarmReset",
            status: ExecutionStatusKind.FAIL,
            startTime: t,
            endTime: t,
            measure: Measure.Create("Timeout", new MeasureOptions(Time: t, Status: ExecutionStatusKind.FAIL))
        ));

        safetySeq.Complete(ExecutionStatusKind.FAIL, safetyEnd);

        // Diagnostics sequence
        SequenceRun diagnosticsSeq = operation.AddSequenceRun(new SequenceRun(
            name: "Sequence/Diagnostics",
            version: "DG-7.4",
            user: "chief.operator",
            startTime: diagnosticsStart,
            characteristics: new[] { new Characteristic("Routing", "Manual") },
            documents: new[] { new Document(fileName: "DiagnosticsMatrix.xlsx", name: "Diagnostics Matrix", description: "Manual routing instructions for diagnostics sequence.") }
        ));

        t = diagnosticsStart.AddMinutes(2);
        diagnosticsSeq.AddStepRun(new StepRun(
            name: "DiagnosticScanRange",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create("W01-W05", new MeasureOptions(Time: t, Status: ExecutionStatusKind.PASS)),
            characteristics: new[] { new Characteristic("ScanType", "Full") }
        ));

        t = diagnosticsStart.AddMinutes(2);
        diagnosticsSeq.AddStepRun(new StepRun(
            name: "DiagnosticScanValue",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create(3, new MeasureOptions(
                Time: t,
                Status: ExecutionStatusKind.PASS,
                Limit: new Limit(LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND, lowerBound: 0, higherBound: 5)
            ))
        ));

        t = diagnosticsStart.AddMinutes(5);
        diagnosticsSeq.AddStepRun(new StepRun(
            name: "RepairNotes",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create("Replaced fuse F7", new MeasureOptions(Time: t, Status: ExecutionStatusKind.PASS))
        ));

        t = diagnosticsStart.AddMinutes(12);
        diagnosticsSeq.AddStepRun(new StepRun(
            name: "FinalVerification",
            status: ExecutionStatusKind.PASS,
            startTime: t,
            endTime: t,
            measure: Measure.Create(true, new MeasureOptions(Time: t, Status: ExecutionStatusKind.PASS))
        ));

        diagnosticsSeq.Complete(ExecutionStatusKind.PASS, diagnosticsEnd);

        operation.Complete(ExecutionStatusKind.FAIL, processEnd);
        process.Complete(ExecutionStatusKind.FAIL, processEnd);

        return new ScenarioResult(warehouse, tzUtil);
    }
}
