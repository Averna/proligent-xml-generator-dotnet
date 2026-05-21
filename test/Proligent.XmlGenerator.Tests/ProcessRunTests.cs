using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class ProcessRunTests
{
    [Fact]
    public void BuildDeterministicProcessRunId_MatchesPythonReferenceValues()
    {
        // This unit test exists to be manually compared to other methods that
        // need to produce the same output IDs.
        //
        // e.g. ResultsProcessor's Utils.GetDeterministicProcessRunId
        //      Python's ProcessRun.build_deterministic_process_run_id (test_comparable_deterministic_process_run_id)
        //
        // Look for this reference:
        // CPDD: {4CABFF4A-F1A0-4C73-AD91-4C84BA2E0E92}

        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "1.0",
                processMode: "PROD"
            )
            .Should()
            .Be("7af755d8-3ee3-4d09-897e-2e7810170091");

        // different productFullName
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/Different123",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "1.0",
                processMode: "PROD"
            )
            .Should()
            .Be("0a433e5a-2975-4672-9ad4-9d6949802492");

        // different identifier
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "DIFFERENT-99999",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "1.0",
                processMode: "PROD"
            )
            .Should()
            .Be("68549968-4863-4179-bd03-288af539d32f");

        // different processFullName
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/DifferentProcessName",
                processVersion: "1.0",
                processMode: "PROD"
            )
            .Should()
            .Be("d327dcd3-5bb8-4c53-9bc4-d4f75cd77af0");

        // different processVersion
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "2.0",
                processMode: "PROD"
            )
            .Should()
            .Be("9e64615b-72dd-48ef-b475-8cff51f3caa2");

        // different processMode
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "1.0",
                processMode: "RMA"
            )
            .Should()
            .Be("153e2144-9cc7-489f-8191-dc0662aecaf2");

        // empty processVersion
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "ProductFamily/ProductName/PartNumber",
                identifier: "PROD-12345",
                processFullName: "ProcessFamily/ProcessName",
                processVersion: "",
                processMode: "PROD"
            )
            .Should()
            .Be("81423af8-ede4-4b27-ade1-7eb95fb59f75");

        // all fields empty and null
        const string expectedWhenAllEmptyOrNull = "bd64bb8e-fa5b-4f27-a644-2f23600c3b51";
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: "",
                identifier: "",
                processFullName: "",
                processVersion: "",
                processMode: ""
            )
            .Should()
            .Be(expectedWhenAllEmptyOrNull);
        ProcessRun.BuildDeterministicProcessRunId(
                productFullName: null,
                identifier: null,
                processFullName: null,
                processVersion: null,
                processMode: null
            )
            .Should()
            .Be(expectedWhenAllEmptyOrNull);
    }

    [Fact]
    public void BuildDeterministicProcessRunId_TreatsNullAsEmpty()
    {
        string withNull = ProcessRun.BuildDeterministicProcessRunId(
            "ProductFamily/ProductName/PartNumber",
            "PROD-12345",
            null,
            null,
            "Production"
        );

        string withEmpty = ProcessRun.BuildDeterministicProcessRunId(
            "ProductFamily/ProductName/PartNumber",
            "PROD-12345",
            string.Empty,
            string.Empty,
            "Production"
        );

        withNull.Should().Be(withEmpty);
    }

    [Fact]
    public void Build_UsesDeterministicId_WhenExplicitIdIsNotProvided()
    {
        var processRun = new ProcessRun(
            productUnitIdentifier: "UNIT-001",
            productFullName: "TestProduct/Widget/123",
            name: "StationA/ProcessB",
            version: "v2.1",
            processMode: "Production"
        );

        string processRunId = processRun.Build().Attribute("ProcessRunId")!.Value;

        processRunId.Should().Be(processRun.IdDeterministic);
    }

    [Fact]
    public void Build_UsesExplicitId_WhenProvided()
    {
        const string customId = "12345678-1234-5678-1234-567812345678";
        var processRun = new ProcessRun(
            id: customId,
            productUnitIdentifier: "UNIT-001",
            productFullName: "TestProduct/Widget/123",
            processMode: "Production"
        );

        string processRunId = processRun.Build().Attribute("ProcessRunId")!.Value;

        processRunId.Should().Be(customId);
    }

    [Fact]
    public void IdDeterministic_ReflectsFieldChanges()
    {
        var processRun = new ProcessRun(
            productUnitIdentifier: "UNIT-001",
            productFullName: "Product/Widget/123",
            processMode: "Production"
        );

        string before = processRun.IdDeterministic;
        processRun.ProductFullName = "Product/Widget/456";
        string after = processRun.IdDeterministic;

        before.Should().NotBe(after);
    }
}
