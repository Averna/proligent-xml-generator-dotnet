using FluentAssertions;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class ManufacturingStepTests
{
    [Fact]
    public void Complete_WithoutEndTime_RefreshesTimestamp()
    {
        StepRun step = new StepRun();
        DateTime first = new DateTime(2025, 1, 1, 12, 0, 0);
        DateTime second = new DateTime(2025, 1, 1, 12, 0, 5);
        DateTime third = new DateTime(2025, 1, 1, 12, 0, 10);
        DateTime fourth = new DateTime(2025, 1, 1, 12, 0, 15);

        step.Complete(ExecutionStatusKind.PASS, first);
        step.EndTime.Should().Be(first);

        step.Complete(ExecutionStatusKind.FAIL, second);
        step.EndTime.Should().Be(second);

        step.Complete(ExecutionStatusKind.NOT_COMPLETED, third);
        step.EndTime.Should().Be(third);

        step.Complete(ExecutionStatusKind.ABORTED, fourth);
        step.EndTime.Should().Be(fourth);
    }
}
