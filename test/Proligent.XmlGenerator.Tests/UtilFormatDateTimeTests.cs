using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class UtilFormatDateTimeTests
{
    [Fact]
    public void FormatDateTime_NaiveWithTimeZoneName()
    {
        Util util = new Util(timeZoneId: "America/New_York");
        DateTime? naive = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        string formatted = util.FormatDateTime(naive);

        Assert.Equal("2024-01-01T12:00:00-05:00", formatted);
    }

    [Fact]
    public void FormatDateTime_NaiveWithTimeZoneInfo()
    {
        TimeSpan offset = TimeSpan.FromHours(3.5);
        TimeZoneInfo tz = TimeZoneInfo.CreateCustomTimeZone("Custom", offset, "Custom", "Custom");
        Util util = new Util(timeZone: tz);
        DateTime? naive = new DateTime(2024, 6, 1, 8, 15, 30, DateTimeKind.Unspecified);

        string formatted = util.FormatDateTime(naive);

        Assert.Equal("2024-06-01T08:15:30+03:30", formatted);
    }
}
