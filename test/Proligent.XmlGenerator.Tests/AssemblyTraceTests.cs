#nullable enable

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class AssemblyTraceTests
{
    private static readonly DateTime T = new(2025, 3, 4, 5, 6, 7, DateTimeKind.Unspecified);
    private const string Guid1 = "11111111-2222-3333-4444-555555555555";

    private static AssemblyTraceParent Parent(string? rd = null) =>
        new("Product/Parent", "P-1", rd);

    private static AssemblyTraceChild ProductChild() =>
        AssemblyTraceChild.FromProductUnit("Product/Child", "C-1");

    private static AssemblyTraceChild SupplierChild() =>
        AssemblyTraceChild.FromSupplierComponent("Acme", "PN-1", "Comp/Name");

    private static XElement Build(AssemblyTrace t) => t.Build();

    [Theory]
    [InlineData(AssemblyAction.ASSEMBLE, "ASSEMBLE")]
    [InlineData(AssemblyAction.DISASSEMBLE, "DISASSEMBLE")]
    [InlineData(AssemblyAction.SET_CURRENT, "SET_CURRENT")]
    public void Actions_Serialize(AssemblyAction action, string expected)
    {
        var e = Build(new AssemblyTrace(Parent(), ProductChild(), action, T));
        e.Name.Should().Be(XmlNamespaces.Dw + "AssemblyTrace");
        e.Attribute("Action")!.Value.Should().Be(expected);
        e.Attribute("ActionTime")!.Value.Should().Be(Util.Default.FormatDateTime((DateTime?)T));
        e.Attribute("ActionTime")!.Value.Should().MatchRegex(@"[+-]\d\d:\d\d$");
        e.Attribute("ChildComponentCount")!.Value.Should().Be("1");
    }

    [Fact]
    public void Parent_And_ReferenceDesignator()
    {
        var e = Build(new AssemblyTrace(Parent("R1"), ProductChild(), AssemblyAction.ASSEMBLE, T));
        var p = e.Element(XmlNamespaces.Dw + "Parent")!;
        p.Attribute("ProductFullName")!.Value.Should().Be("Product/Parent");
        p.Attribute("ProductUnitIdentifier")!.Value.Should().Be("P-1");
        p.Attribute("ReferenceDesignator")!.Value.Should().Be("R1");

        var none = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T));
        none.Element(XmlNamespaces.Dw + "Parent")!.Attribute("ReferenceDesignator").Should().BeNull();
    }

    [Fact]
    public void Parent_And_Child_From_ProductUnit_Objects()
    {
        var parent = new ProductUnit("P-9", "Prod/A");
        var child = new ProductUnit("C-9", "Prod/B");
        var e = Build(
            new AssemblyTrace(
                new AssemblyTraceParent(parent),
                AssemblyTraceChild.FromProductUnit(child),
                AssemblyAction.ASSEMBLE,
                T
            )
        );
        e.Element(XmlNamespaces.Dw + "Parent")!.Attribute("ProductUnitIdentifier")!.Value.Should().Be("P-9");
        e.Element(XmlNamespaces.Dw + "Child")!
            .Element(XmlNamespaces.Dw + "ProductUnitUniqueIdentifier")!
            .Attribute("ProductFullName")!.Value.Should().Be("Prod/B");
    }

    [Fact]
    public void ProductUnit_Child_Has_Single_Branch()
    {
        var e = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T));
        var child = e.Element(XmlNamespaces.Dw + "Child")!;
        child.Elements().Should().ContainSingle().Which.Name.Should().Be(XmlNamespaces.Dw + "ProductUnitUniqueIdentifier");
    }

    [Fact]
    public void Supplier_Child_With_And_Without_Optionals()
    {
        var full = AssemblyTraceChild.FromSupplierComponent("Acme", "PN-1", "Comp/Name", "LOT", "SN");
        var e = Build(new AssemblyTrace(Parent(), full, AssemblyAction.DISASSEMBLE, T));
        var c = e.Element(XmlNamespaces.Dw + "Child")!.Elements().Should().ContainSingle().Subject;
        c.Name.Should().Be(XmlNamespaces.Dw + "ComponentUnitIdentifier");
        c.Attribute("ManufacturerLot")!.Value.Should().Be("LOT");
        c.Attribute("ManufacturerSerialNumber")!.Value.Should().Be("SN");

        var min = Build(new AssemblyTrace(Parent(), SupplierChild(), AssemblyAction.DISASSEMBLE, T));
        var m = min.Element(XmlNamespaces.Dw + "Child")!.Elements().Single();
        m.Attribute("ManufacturerLot").Should().BeNull();
        m.Attribute("ManufacturerSerialNumber").Should().BeNull();
    }

    [Fact]
    public void Context_Absent_Operation_And_Sequence()
    {
        Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T))
            .Element(XmlNamespaces.Dw + "AssemblyContext").Should().BeNull();

        var op = new OperationRun(station: "Station/A", name: "Op");
        var seq = new SequenceRun(name: "Seq", status: ExecutionStatusKind.PASS);

        var e1 = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T,
            AssemblyTraceContext.FromOperationRun(op)));
        var c1 = e1.Element(XmlNamespaces.Dw + "AssemblyContext")!.Elements().Single();
        c1.Name.Should().Be(XmlNamespaces.Dw + "OperationRun");
        c1.Attribute("Id")!.Value.Should().Be(op.Id);

        var e2 = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T,
            AssemblyTraceContext.FromSequenceRun(seq)));
        e2.Element(XmlNamespaces.Dw + "AssemblyContext")!.Elements().Single()
            .Name.Should().Be(XmlNamespaces.Dw + "SequenceRun");

        var e3 = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T,
            AssemblyTraceContext.FromSequenceRunId(Guid1)));
        e3.Descendants(XmlNamespaces.Dw + "SequenceRun").Single().Attribute("Id")!.Value.Should().Be(Guid1);
    }

    [Fact]
    public void Context_Conflicts_And_Invalid_Guids_Throw()
    {
        Assert.Throws<ArgumentException>(() =>
            AssemblyTraceContext.Create(operationRunId: Guid1, sequenceRunId: Guid1));
        Assert.Throws<ArgumentException>(() => AssemblyTraceContext.Create());
        var ex = Assert.Throws<ArgumentException>(() => AssemblyTraceContext.FromOperationRunId("nope"));
        ex.Message.Should().Contain("operationRunId");
    }

    [Fact]
    public void Optional_Attributes_Serialized_Or_Omitted()
    {
        var e = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T,
            failureId: Guid1, stationFullName: "Root/Station", operatorName: "Jane"));
        e.Attribute("FailureId")!.Value.Should().Be(Guid1);
        e.Attribute("StationFullName")!.Value.Should().Be("Root/Station");
        e.Attribute("Operator")!.Value.Should().Be("Jane");

        var n = Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T));
        n.Attribute("FailureId").Should().BeNull();
        n.Attribute("StationFullName").Should().BeNull();
        n.Attribute("Operator").Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void ChildComponentCount_Valid(int count)
    {
        Build(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T,
            childComponentCount: count)).Attribute("ChildComponentCount")!.Value.Should().Be(count.ToString());
    }

    [Fact]
    public void ChildComponentCount_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T, childComponentCount: -1));
    }

    [Fact]
    public void Invalid_Inputs_Throw_With_Field_Names()
    {
        Assert.Throws<ArgumentException>(() => new AssemblyTrace(Parent(), ProductChild(), (AssemblyAction)99, T));
        Assert.Throws<ArgumentException>(() => new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, default));
        Assert.Throws<ArgumentNullException>(() => new AssemblyTrace(null!, ProductChild(), AssemblyAction.ASSEMBLE, T));
        Assert.Throws<ArgumentNullException>(() => new AssemblyTrace(Parent(), null!, AssemblyAction.ASSEMBLE, T));
        Assert.Throws<ArgumentException>(() => new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T, failureId: "x"));
        Assert.Throws<ArgumentException>(() => new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T, operatorName: new string('a', 256)));
        Assert.Throws<ArgumentException>(() => new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T, stationFullName: "/bad/"));
        Assert.Throws<ArgumentException>(() => new AssemblyTraceParent("", "P"));
        Assert.Throws<ArgumentException>(() => new AssemblyTraceParent("A", new string('x', 51)));
        Assert.Throws<ArgumentException>(() => new AssemblyTraceParent("A", "P", new string('x', 51)));
        Assert.Throws<ArgumentException>(() => AssemblyTraceChild.FromSupplierComponent("", "PN", "C"));
        Assert.Throws<ArgumentException>(() => AssemblyTraceChild.FromSupplierComponent("M", new string('x', 129), "C"));
        var ex = Assert.Throws<ArgumentException>(() => AssemblyTraceChild.FromProductUnit("A", ""));
        ex.Message.Should().Contain("ProductUnitIdentifier");
    }

    [Fact]
    public void Warehouse_Exports_Multiple_Products_And_Traces_In_Order_And_Validates()
    {
        var primary = new ProductUnit("P-1", "Product/Parent");
        var wh = new DataWareHouse(
            productUnit: primary,
            generationTime: T,
            sourceFingerprint: Guid1
        );
        var add1 = wh.AddProductUnit(new ProductUnit("C-1", "Product/Child"));
        wh.AddProductUnit(new ProductUnit("C-2", "Product/Child"));
        add1.ProductUnitIdentifier.Should().Be("C-1");
        wh.ProductUnit.Should().BeSameAs(primary);

        wh.AddAssemblyTrace(new AssemblyTrace(Parent("R1"), ProductChild(), AssemblyAction.ASSEMBLE, T,
            AssemblyTraceContext.FromOperationRunId(Guid1), Guid1, "Root/Station", "Jane", 2));
        wh.AddAssemblyTrace(new AssemblyTrace(Parent(), SupplierChild(), AssemblyAction.DISASSEMBLE, T));
        wh.AddAssemblyTrace(new AssemblyTrace(Parent(), AssemblyTraceChild.FromSupplierComponent("Acme", "PN", "Comp", "L", "S"),
            AssemblyAction.SET_CURRENT, T, AssemblyTraceContext.FromSequenceRunId(Guid1), childComponentCount: 0));

        var root = wh.Build();
        root.Elements(XmlNamespaces.Dw + "ProductUnit")
            .Select(x => x.Attribute("ProductUnitIdentifier")!.Value)
            .Should().Equal("P-1", "C-1", "C-2");
        root.Elements(XmlNamespaces.Dw + "AssemblyTrace").Should().HaveCount(3);

        var path = Path.Combine(Path.GetTempPath(), $"asm_{Guid.NewGuid()}.xml");
        try
        {
            File.WriteAllText(path, wh.ToXml());
            XmlValidator.ValidateXml(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Warehouse_Without_Additions_Is_Unchanged_And_Traces_Do_Not_Create_Products()
    {
        var wh = new DataWareHouse(generationTime: T, sourceFingerprint: Guid1);
        wh.AddAssemblyTrace(new AssemblyTrace(Parent(), ProductChild(), AssemblyAction.ASSEMBLE, T));
        var root = wh.Build();
        root.Elements(XmlNamespaces.Dw + "ProductUnit").Should().BeEmpty();

        var legacy = new DataWareHouse(productUnit: new ProductUnit("A", "B"), generationTime: T, sourceFingerprint: Guid1);
        legacy.Build().Elements().Should().ContainSingle();
        var replaced = legacy.SetProductUnit(new ProductUnit("Z", "Y"));
        legacy.AddProductUnit(new ProductUnit("Q", "Y"));
        legacy.ProductUnit.Should().BeSameAs(replaced);
        legacy.Build().Elements(XmlNamespaces.Dw + "ProductUnit").Should().HaveCount(2);
    }
}



