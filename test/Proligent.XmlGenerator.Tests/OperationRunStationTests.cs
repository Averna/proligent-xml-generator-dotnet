using System;
using System.Linq;
using Proligent.XmlGenerator;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class OperationRunStationTests
{
    [Fact]
    public void SequenceRun_BuildRequiresStation()
    {
        var sequence = new SequenceRun();
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
        var operation = new OperationRun("Station/Example");
        var sequence = operation.AddSequenceRun(new SequenceRun());

        var builtOperation = operation.Build();

        Assert.Equal("Station/Example", builtOperation.Attribute("StationFullName")?.Value);
        Assert.Equal("Station/Example", sequence.Station);
        var sequenceElement = builtOperation.Elements().First(e => e.Name.LocalName == "SequenceRun");
        Assert.Equal("Station/Example", sequenceElement.Attribute("StationFullName")?.Value);
    }
}
