namespace Proligent.XmlGenerator.Tests.Scenarios;

public class ReadmeExample1XmlGenerationScenario : IXmlGenerationScenario
{
    public ScenarioResult Generate()
    {
        Util tzUtil = new Util(timeZoneId: "Europe/Brussels");
        DateTime instant = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        Limit limit = new Limit(
            LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
            lowerBound: 10,
            higherBound: 25
        );
        var measure = Measure.Create(15, new MeasureOptions(
            Limit: limit,
            Time: instant,
            Status: ExecutionStatusKind.PASS
        ));

        StepRun step = new StepRun(
            measure: measure,
            name: "Step1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        SequenceRun sequence = new SequenceRun(
            steps: new[] { step },
            name: "Sequence1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        OperationRun operation = new OperationRun(
            station: "Station/readme_example1",
            sequences: new[] { sequence },
            name: "Operation1",
            processName: "Process/readme_example1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        ProcessRun process = new ProcessRun(
            productUnitIdentifier: "DutSerialNumber",
            productFullName: "Product/readme_example1",
            operations: new[] { operation },
            name: "Process/readme_example1",
            processMode: "PROD",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        ProductUnit product = new ProductUnit(
            productUnitIdentifier: "DutSerialNumber",
            productFullName: "Product/readme_example1",
            manufacturer: "Averna"
        );

        DataWareHouse warehouse = new DataWareHouse(
            topProcess: process,
            productUnit: product,
            generationTime: instant
        );

        return new ScenarioResult(warehouse, tzUtil);
    }
}
