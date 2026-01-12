using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class XmlGenerationTests
{
    [Fact]
    public void ReadmeExample1_MatchesFixture()
    {
        Util tzUtil = new Util(timeZoneId: "Europe/Brussels");
        DateTime instant = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        Limit limit = new Limit(
            LimitExpression.LOWERBOUND_LEQ_X_LE_HIGHER_BOUND,
            lowerBound: 10,
            higherBound: 25
        );
        Measure measure = new Measure(
            value: 15,
            id: "00000000-0000-0000-0000-000000000001",
            limit: limit,
            time: instant,
            status: ExecutionStatusKind.PASS
        );

        StepRun step = new StepRun(
            measure: measure,
            id: "00000000-0000-0000-0000-000000000002",
            name: "Step1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        SequenceRun sequence = new SequenceRun(
            steps: new[] { step },
            id: "00000000-0000-0000-0000-000000000003",
            name: "Sequence1",
            status: ExecutionStatusKind.PASS,
            startTime: instant,
            endTime: instant
        );

        OperationRun operation = new OperationRun(
            station: "Station/readme_example1",
            sequences: new[] { sequence },
            id: "00000000-0000-0000-0000-000000000004",
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
            id: "00000000-0000-0000-0000-000000000005",
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
            generationTime: instant,
            sourceFingerprint: "00000000-0000-0000-0000-000000000006"
        );

        string xml = warehouse.ToXml(tzUtil);
        XDocument generated = XDocument.Parse(xml);
        string expectedPath = Path.Combine(
            AppContext.BaseDirectory,
            "Expected",
            "Proligent_readme_example1.xml"
        );
        XDocument expected = XDocument.Load(expectedPath);

        XNode
            .DeepEquals(generated, expected)
            .Should()
            .BeTrue("generated XML should match the fixture");
    }
}
