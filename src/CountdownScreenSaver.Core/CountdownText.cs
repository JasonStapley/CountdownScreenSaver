using System.Globalization;

namespace CountdownScreenSaver;

/// <summary>
/// Builds the sentence painted by the screensaver.
/// Weeks and leftover days use the 2016 split: truncating division for weeks,
/// and the remainder for days. Truncation is toward zero, so a date in the past
/// stays negative the same way the original program did.
/// </summary>
public static class CountdownText
{
    public static string Format(DateTime endDate, string? message, DateTime today)
    {
        // Round away binary dust so a whole number of days stays whole.
        // A time-of-day on the target is still visible (about 0.1 second resolution).
        var totalDays = Math.Round((endDate - today).TotalDays, 6, MidpointRounding.AwayFromZero);
        var daysUntil = totalDays % 7d;
        var weeksUntil = (double)((int)totalDays / 7);

        return string.Format(
            CultureInfo.CurrentCulture,
            "There are {0} and {1} until {2} Total days {3}",
            Unit(weeksUntil, "week"),
            Unit(daysUntil, "day"),
            message ?? "",
            Number(totalDays));
    }

    private static string Unit(double number, string unit)
    {
        var rounded = Math.Round(number, 6, MidpointRounding.AwayFromZero);
        var singular = Math.Abs(Math.Abs(rounded) - 1d) < 0.0000005d;
        var word = singular ? unit : unit + "s";
        return string.Format(CultureInfo.CurrentCulture, "{0} {1}", Number(rounded), word);
    }

    private static string Number(double number)
    {
        var rounded = Math.Round(number, 6, MidpointRounding.AwayFromZero);
        // A remainder of zero from a negative total is signed zero, which formats as "-0".
        if (rounded == 0)
            rounded = 0;

        return rounded.ToString("0.####", CultureInfo.CurrentCulture);
    }
}
