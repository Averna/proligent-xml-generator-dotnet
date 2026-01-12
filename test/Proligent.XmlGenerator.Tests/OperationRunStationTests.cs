using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class OperationRunStationTests
{
    [Fact]
    public void SequenceRun_BuildRequiresStation()
    {
        SequenceRun sequence = new SequenceRun();
        Assert.Throws<InvalidOperationException>(() => sequence.Build());
    }

    [Fact]
    public void OperationRun_StationIsRequired()
    {
        Assert.Throws<ArgumentException>(() => new OperationRun(""));
    }

    [Fact]
    public void OperationRun_StationPropagatesToSequences()
    {
        OperationRun operation = new OperationRun("Station/Example");
        SequenceRun sequence = operation.AddSequenceRun(new SequenceRun());

        var builtOperation = operation.Build();

        Assert.Equal("Station/Example", builtOperation.Attribute("StationFullName")?.Value);
        Assert.Equal("Station/Example", sequence.Station);
        var sequenceElement = builtOperation
              .Elements()
              .First(e => e.Name.LocalName == "SequenceRun");
        Assert.Equal("Station/Example", sequenceElement.Attribute("StationFullName")?.Value);
    }
}
