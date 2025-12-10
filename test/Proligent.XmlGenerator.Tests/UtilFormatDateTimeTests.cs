using System;
using Proligent.XmlGenerator;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class UtilFormatDateTimeTests
{
    [Fact]
    public void FormatDateTime_NaiveWithTimeZoneName()
    {
        var util = new Util(timeZoneId: "America/New_York");
        var naive = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        var formatted = util.FormatDateTime(naive);

        Assert.Equal("2024-01-01T12:00:00-05:00", formatted);
    }

    [Fact]
    public void FormatDateTime_NaiveWithTimeZoneInfo()
    {
        var offset = TimeSpan.FromHours(3.5);
        var tz = TimeZoneInfo.CreateCustomTimeZone("Custom", offset, "Custom", "Custom");
        var util = new Util(timeZone: tz);
        var naive = new DateTime(2024, 6, 1, 8, 15, 30, DateTimeKind.Unspecified);

        var formatted = util.FormatDateTime(naive);

        Assert.Equal("2024-06-01T08:15:30+03:30", formatted);
    }
}
