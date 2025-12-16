using System.Xml.Schema;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class XmlValidatorTests
{
    private static string ExpectedDir => Path.Combine(AppContext.BaseDirectory, "Expected");
    private static string ResourceDir => Path.Combine(AppContext.BaseDirectory, "Resources");

    [Fact]
    public void ValidateXml_PassesForValidDocument()
    {
        string path = Path.Combine(ExpectedDir, "Proligent_readme_example1.xml");
        XmlValidator.ValidateXml(path);
    }

    [Fact]
    public void ValidateXml_RaisesForInvalidDocument()
    {
        string path = Path.Combine(ResourceDir, "invalid_product_unit_missing_full_name.xml");
        Assert.Throws<XmlSchemaValidationException>(() => XmlValidator.ValidateXml(path));
    }

    [Fact]
    public void ValidateXmlSafe_ReturnsMetadataForInvalidDocument()
    {
        string path = Path.Combine(ResourceDir, "invalid_product_unit_missing_full_name.xml");

        var (isValid, metadata) = XmlValidator.ValidateXmlSafe(path);

        Assert.False(isValid);
        Assert.NotNull(metadata);
        Assert.Contains("ProductFullName", metadata!.Message);
        Assert.Contains("ProductUnit", metadata.Path ?? string.Empty);
    }
}
