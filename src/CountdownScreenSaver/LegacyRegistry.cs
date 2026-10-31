using System.Globalization;
using Microsoft.Win32;

namespace CountdownScreenSaver;

/// <summary>
/// One-way read of the 2016 registry values. The new program never creates or updates this key.
/// </summary>
internal static class LegacyRegistry
{
    internal const string Subkey = @"CountdownScreenSaver";

    public static ScreensaverSettings? TryRead()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(Subkey);
            if (key is null)
                return null;

            var dateText = key.GetValue("CountdownDate") as string;
            var text = key.GetValue("CountdownText") as string;
            var fontName = key.GetValue("Fontname") as string;
            var fontSizeText = key.GetValue("Fontsize") as string;
            var intervalValue = key.GetValue("CountdownInterval");

            if (dateText is null && text is null && fontName is null && fontSizeText is null && intervalValue is null)
                return null;

            var settings = ScreensaverSettings.CreateLegacyDefaults();
            if (ScreensaverSettings.TryParseLegacyDate(dateText, out var date))
                settings.CountdownDate = date;

            if (text is not null)
                settings.CountdownText = text;

            if (!string.IsNullOrWhiteSpace(fontName))
                settings.FontName = fontName;

            if (ScreensaverSettings.TryParseLegacyFloat(fontSizeText, out var size))
                settings.FontSize = size;

            if (TryReadInterval(intervalValue, out var interval))
                settings.IntervalMilliseconds = interval;

            // The old font dialog did not store Bold, Italic, or the other styles.
            settings.FontStyle = "Regular";
            return settings;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool TryReadInterval(object? value, out int interval)
    {
        switch (value)
        {
            case int number:
                interval = number;
                return true;
            case long number when number is >= int.MinValue and <= int.MaxValue:
                interval = (int)number;
                return true;
            case string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                interval = parsed;
                return true;
            case string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed):
                interval = parsed;
                return true;
            default:
                interval = 0;
                return false;
        }
    }
}
