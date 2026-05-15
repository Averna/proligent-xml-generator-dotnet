using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests.Scenarios;

public class XmlScenarioGenerationTests(ITestOutputHelper output)
{
    private static string OutputDirectory =>
        Environment.GetEnvironmentVariable("VSTEST_RESULTS_DIRECTORY")
        ?? Path.Combine(AppContext.BaseDirectory, "TestOutput");

    private static readonly string OutputFolderTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    private static readonly string RunDirectory = Path.Combine(OutputDirectory, OutputFolderTimestamp);
    private static readonly string RunRealFilesDirectory = Path.Combine(RunDirectory, "real");

    /// <summary>
    /// Override Util.Default's UUID factory with a sequential counter (matches Python mock_uuid_sequence).
    /// Returns an IDisposable that restores the original factory on Dispose.
    /// </summary>
    private static IDisposable MockUuidSequence(string prefix = "00000000-0000-0000-0000-", int start = 1)
    {
        var original = Util.Default.UuidFactory;
        int counter = start;
        Util.Default.UuidFactory = () => $"{prefix}{counter++:000000000000}";
        return new DelegateDisposable(() => Util.Default.UuidFactory = original);
    }

    private sealed class DelegateDisposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    private static string XmlScenarioFileName(string scenarioName) => $"Proligent_{scenarioName}.xml";
    private static string XmlScenarioFileNameWithSuffix(string scenarioName, string suffix) => $"Proligent_{scenarioName}.{suffix}.xml";

    /// <summary>
    /// Runs a scenario end-to-end:
    /// 1. Generates with mocked UUIDs and compares against the expected fixture.
    /// 2. Generates a "real" file with real UUIDs and real timestamps via SaveXml.
    /// </summary>
    private void RunScenario(IXmlGenerationScenario scenario, string scenarioName)
    {
        output.WriteLine($"RunDirectory : {RunDirectory}");

        Directory.CreateDirectory(RunDirectory);
        Directory.CreateDirectory(RunRealFilesDirectory);

        // define file names and paths
        string expectedSourceFilePath = Path.Combine(AppContext.BaseDirectory, "Expected", XmlScenarioFileName(scenarioName));
        var expectedCopiedFileName = XmlScenarioFileNameWithSuffix(scenarioName, "expected");
        string expectedCopyFilePath = Path.Combine(RunDirectory, expectedCopiedFileName);
        string actualXmlFileName = XmlScenarioFileNameWithSuffix(scenarioName, "actual");
        string realXmlFileName = XmlScenarioFileNameWithSuffix(scenarioName, "real");

        // copy 'expected' file to output folder
        if (!File.Exists(expectedSourceFilePath))
        {
            throw new Exception($"'Expected' file not found for scenario '{scenarioName}'. Expected file path: '{expectedSourceFilePath}'");
        }
        File.Copy(expectedSourceFilePath, expectedCopyFilePath, overwrite: true);
        output.WriteLine($"Expected file copied : {expectedCopiedFileName}");

        using (var _ = MockUuidSequence())
        {
            ScenarioResult result = scenario.Generate();
            result.Warehouse.SaveXml(RunDirectory, actualXmlFileName, result.Util, copyReferenceDocumentsToDestination: false);
            output.WriteLine($"Generated File Path : {actualXmlFileName}");

            string actualFilePath = Path.Combine(RunDirectory, actualXmlFileName);
            CompareActualVsExpected(actualFilePath, expectedSourceFilePath);
        }

        ScenarioResult realXmlResult = scenario.Generate(startTimestamp: DateTime.Now);
        string realXmlFilePath = realXmlResult.Warehouse.SaveXml(RunRealFilesDirectory, realXmlFileName, realXmlResult.Util, copyReferenceDocumentsToDestination: false);
        CreateDummyDocumentFiles(realXmlFilePath, RunRealFilesDirectory);
        output.WriteLine($"Real file : {realXmlFileName}");
    }

    /// <summary>
    /// Creates dummy document files in the destination folder for each Document referenced
    /// in the saved XML file.
    /// </summary>
    private static void CreateDummyDocumentFiles(string xmlFilePath, string destinationFolder)
    {
        XDocument doc = XDocument.Load(xmlFilePath);

        foreach (var docElem in doc.Descendants(XmlNamespaces.Dw + "Document"))
        {
            var fileAttr = docElem.Attribute("FileName");
            if (fileAttr is null || string.IsNullOrWhiteSpace(fileAttr.Value))
                continue;

            string originalFileName = fileAttr.Value;
            string identifier = docElem.Attribute("Identifier")?.Value ?? "unknown";
            string suggestedFileName = $"Document_{identifier}_{originalFileName}";
            string dummyFilePath = Path.Combine(destinationFolder, suggestedFileName);

            if (!File.Exists(dummyFilePath))
            {
                string content =
                    $"""
                    Dummy Document File
                    ===================

                    This is a placeholder document generated for testing purposes.

                    Document ID: {identifier}
                    Original Filename: {originalFileName}
                    Suggested Filename: {suggestedFileName}
                    Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

                    This file would normally contain the actual document content.
                    """;

                File.WriteAllText(dummyFilePath, content);
            }
        }
    }

    private void CompareActualVsExpected(string actualFilePath, string expectedSourceFilePath)
    {
        // load 'actual' XML from the saved file
        XDocument generated = XDocument.Load(actualFilePath);

        // load 'expected' XML
        XDocument expected = XDocument.Load(expectedSourceFilePath);

        // compare
        XmlTestUtils.NormalizeXml(generated.ToString())
            .Should().Be(XmlTestUtils.NormalizeXml(expected.ToString()));
    }

    [Fact] public void ReadmeExample1() => RunScenario(new ReadmeExample1XmlGenerationScenario(), "readme_example1");
    [Fact] public void ReadmeExample2() => RunScenario(new ReadmeExample2XmlGenerationScenario(), "readme_example2");
    [Fact] public void SimpleOprunNormalOrder() => RunScenario(new SimpleOprunNormalOrderXmlGenerationScenario(), "simple_oprun_normal_order");
    [Fact] public void SimpleOprunReverseOrder() => RunScenario(new SimpleOprunReverseOrderXmlGenerationScenario(), "simple_oprun_reverse_order");
    [Fact] public void SimpleOprunSharedProcessId() => RunScenario(new SimpleOprunSharedProcessIdXmlGenerationScenario(), "simple_oprun_shared_process_id");
    [Fact] public void ComplexOprun() => RunScenario(new ComplexOprunXmlGenerationScenario(), "complex_oprun");
    [Fact] public void PartialOperationRun() => RunScenario(new PartialOperationRunXmlGenerationScenario(), "partial_operation_run");
}
