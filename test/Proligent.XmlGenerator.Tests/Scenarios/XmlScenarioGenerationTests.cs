using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests.Scenarios;

public class XmlScenarioGenerationTests(ITestOutputHelper output)
{
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

    private static string OutputDirectory =>
        Environment.GetEnvironmentVariable("VSTEST_RESULTS_DIRECTORY")
        ?? Path.Combine(AppContext.BaseDirectory, "TestOutput");

    private void CompareToFixture(ScenarioResult result, string fixtureName)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string runDirectory = Path.Combine(OutputDirectory, "xml-scenarios", timestamp);
        Directory.CreateDirectory(runDirectory);

        // Use ToXml to produce the XML string — SaveXml is for real deployments and
        // requires the referenced document files (PDFs, images, etc.) to exist on disk.
        string xmlString = result.Warehouse.ToXml(result.Util);
        XDocument generated = XDocument.Parse(xmlString);

        string generatedPath = Path.Combine(runDirectory, AddSuffix(fixtureName, "actual"));
        WriteXml(generated, generatedPath);
        output.WriteLine($"Generated : {generatedPath}");

        string expectedSource = Path.Combine(AppContext.BaseDirectory, "Expected", fixtureName);
        string expectedCopyPath = Path.Combine(runDirectory, AddSuffix(fixtureName, "expected"));
        if (File.Exists(expectedSource))
        {
            File.Copy(expectedSource, expectedCopyPath, overwrite: true);
            output.WriteLine($"Expected  : {expectedCopyPath}");
        }

        if (Environment.GetEnvironmentVariable("REGEN_FIXTURES") == "1")
        {
            string sourceFixturePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "Expected", fixtureName));
            WriteXml(generated, sourceFixturePath);
            output.WriteLine($"Fixture regenerated: {sourceFixturePath}");
            return;
        }

        XDocument expected = XDocument.Load(expectedSource);
        XmlTestUtils.NormalizeXml(generated.ToString())
            .Should().Be(XmlTestUtils.NormalizeXml(expected.ToString()));
    }

    /// <summary>Inserts a dot-separated suffix before the file extension: "foo.xml" → "foo.actual.xml".</summary>
    private static string AddSuffix(string fileName, string suffix)
    {
        string ext = Path.GetExtension(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        return $"{stem}.{suffix}{ext}";
    }

    private static void WriteXml(XDocument doc, string destPath)
    {
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        using var sw = new StreamWriter(destPath, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var xw = System.Xml.XmlWriter.Create(sw, settings);
        doc.Save(xw);
    }

    [Fact]
    public void ReadmeExample1()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new ReadmeExample1XmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_readme_example1.xml");
    }

    [Fact]
    public void ReadmeExample2()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new ReadmeExample2XmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_readme_example2.xml");
    }

    [Fact]
    public void SimpleOprunNormalOrder()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new SimpleOprunNormalOrderXmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_simple_oprun_normal_order.xml");
    }

    [Fact]
    public void SimpleOprunReverseOrder()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new SimpleOprunReverseOrderXmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_simple_oprun_reverse_order.xml");
    }

    [Fact]
    public void SimpleOprunSharedProcessId()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new SimpleOprunSharedProcessIdXmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_simple_oprun_shared_process_id.xml");
    }

    [Fact]
    public void ComplexOprun()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new ComplexOprunXmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_complex_oprun.xml");
    }

    [Fact]
    public void PartialOperationRun()
    {
        using var _ = MockUuidSequence();
        ScenarioResult result = new PartialOperationRunXmlGenerationScenario().Generate();
        CompareToFixture(result, "Proligent_partial_operation_run.xml");
    }
}
