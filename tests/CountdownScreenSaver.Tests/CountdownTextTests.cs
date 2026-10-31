using System.Globalization;
using Xunit;

namespace CountdownScreenSaver.Tests;

public sealed class CountdownTextTests
{
    public CountdownTextTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [Theory]
    [InlineData(0, "There are 0 weeks and 0 days until Party Total days 0")]
    [InlineData(1, "There are 0 weeks and 1 day until Party Total days 1")]
    [InlineData(6, "There are 0 weeks and 6 days until Party Total days 6")]
    [InlineData(7, "There are 1 week and 0 days until Party Total days 7")]
    [InlineData(8, "There are 1 week and 1 day until Party Total days 8")]
    [InlineData(14, "There are 2 weeks and 0 days until Party Total days 14")]
    [InlineData(-1, "There are 0 weeks and -1 day until Party Total days -1")]
    [InlineData(-3, "There are 0 weeks and -3 days until Party Total days -3")]
    [InlineData(-7, "There are -1 week and 0 days until Party Total days -7")]
    [InlineData(-8, "There are -1 week and -1 day until Party Total days -8")]
    public void Counts_weeks_and_remainder_days(int dayOffset, string expected)
    {
        var today = new DateTime(2026, 1, 1);
        var actual = CountdownText.Format(today.AddDays(dayOffset), "Party", today);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Keeps_a_half_day_when_the_target_has_a_time()
    {
        var today = new DateTime(2026, 1, 1);
        var actual = CountdownText.Format(today.AddHours(12), "Noon", today);
        Assert.Equal("There are 0 weeks and 0.5 days until Noon Total days 0.5", actual);
    }

    [Fact]
    public void Leaves_the_original_gap_when_the_message_is_blank()
    {
        var today = new DateTime(2016, 11, 18);
        var actual = CountdownText.Format(new DateTime(2016, 11, 25), null, today);
        Assert.Equal("There are 1 week and 0 days until  Total days 7", actual);
    }
}
