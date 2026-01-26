using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class DocumentSerializationTests
{
    [Fact]
    public void StepRun_Includes_Document_Elements_When_Populated()
    {
        var doc = new Document(
            fileName: "step-file.txt",
            identifier: "step-id",
            name: "StepDoc",
            description: "Step description"
        );

        var step = new StepRun(name: "S", status: ExecutionStatusKind.PASS, documents: new[] { doc });

        var element = step.Build();

        var documentElements = element.Elements(XmlNamespaces.Dw + "Document").ToList();
        documentElements.Should().HaveCount(1);

        var docElem = documentElements.Single();
        docElem.Attribute("Identifier")?.Value.Should().Be("step-id");
        docElem.Attribute("FileName")?.Value.Should().Be("step-file.txt");
        docElem.Attribute("Name")?.Value.Should().Be("StepDoc");
        docElem.Attribute("Description")?.Value.Should().Be("Step description");
    }

    [Fact]
    public void SequenceRun_Includes_Document_Elements_When_Populated()
    {
        var seqDoc = new Document(fileName: "sequence-file.txt", identifier: "seq-id");
        var sequence = new SequenceRun(name: "Seq", status: ExecutionStatusKind.PASS, documents: new[] { seqDoc });

        // SequenceRun requires a station before building; add it to an OperationRun to assign station.
        var operation = new OperationRun(station: "Station/Test", sequences: new[] { sequence }, name: "Op");
        var opElement = operation.Build();

        var seqElement = opElement.Elements(XmlNamespaces.Dw + "SequenceRun")
            .First(e => e.Attribute("SequenceFullName")?.Value == "Seq" || e.Attribute("SequenceRunId") != null);

        var documentElements = seqElement.Elements(XmlNamespaces.Dw + "Document").ToList();
        documentElements.Should().HaveCount(1);

        var docElem = documentElements.Single();
        docElem.Attribute("Identifier")?.Value.Should().Be("seq-id");
        docElem.Attribute("FileName")?.Value.Should().Be("sequence-file.txt");
    }

    [Fact]
    public void OperationRun_Includes_Document_Elements_When_Populated()
    {
        var opDoc = new Document(fileName: "operation-file.txt", identifier: "op-id", name: "OpDoc");
        var operation = new OperationRun(station: "Station/A", documents: new[] { opDoc }, name: "OpX");

        var element = operation.Build();

        var documentElements = element.Elements(XmlNamespaces.Dw + "Document").ToList();
        documentElements.Should().HaveCount(1);

        var docElem = documentElements.Single();
        docElem.Attribute("Identifier")?.Value.Should().Be("op-id");
        docElem.Attribute("FileName")?.Value.Should().Be("operation-file.txt");
        docElem.Attribute("Name")?.Value.Should().Be("OpDoc");
    }

    [Fact]
    public void ProductUnit_Includes_Document_Elements_When_Populated()
    {
        var puDoc = new Document(fileName: "product-file.txt", identifier: "pu-id", description: "PU Desc");
        var product = new ProductUnit(productUnitIdentifier: "PU1", productFullName: "Product/One", documents: new[] { puDoc });

        var element = product.Build();

        var documentElements = element.Elements(XmlNamespaces.Dw + "Document").ToList();
        documentElements.Should().HaveCount(1);

        var docElem = documentElements.Single();
        docElem.Attribute("Identifier")?.Value.Should().Be("pu-id");
        docElem.Attribute("FileName")?.Value.Should().Be("product-file.txt");
        docElem.Attribute("Description")?.Value.Should().Be("PU Desc");
    }
}   