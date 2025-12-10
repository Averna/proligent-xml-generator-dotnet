using System;
using FluentAssertions;
using Proligent.XmlGenerator;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class ManufacturingStepTests
{
    [Fact]
    public void Complete_WithoutEndTime_RefreshesTimestamp()
    {
        var step = new StepRun();
        var first = new DateTime(2024, 1, 1, 12, 0, 0);
        var second = new DateTime(2024, 1, 1, 12, 0, 5);

        step.Complete(ExecutionStatusKind.PASS, first);
        step.EndTime.Should().Be(first);

        step.Complete(ExecutionStatusKind.FAIL, second);
        step.EndTime.Should().Be(second);
    }
}
