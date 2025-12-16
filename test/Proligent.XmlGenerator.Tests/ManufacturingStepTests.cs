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
        StepRun step = new StepRun();
        DateTime first = new DateTime(2024, 1, 1, 12, 0, 0);
        DateTime second = new DateTime(2024, 1, 1, 12, 0, 5);

        step.Complete(ExecutionStatusKind.PASS, first);
        step.EndTime.Should().Be(first);

        step.Complete(ExecutionStatusKind.FAIL, second);
        step.EndTime.Should().Be(second);
    }
}
