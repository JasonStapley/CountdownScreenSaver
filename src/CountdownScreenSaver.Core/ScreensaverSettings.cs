using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountdownScreenSaver;

/// <summary>
/// User settings. The running screensaver reads
/// <c>%AppData%\CountdownScreenSaver\settings.json</c>.
/// </summary>
public sealed class ScreensaverSettings
{
    public DateTime CountdownDate { get; set; } = new(2016, 11, 25);

    public string CountdownText { get; set; } = "Enter text here";

    public string FontName { get; set; } = "Broadway";

    public float FontSize { get; set; } = 48f;

    /// <summary>
    /// Font style names accepted by <c>System.Drawing.FontStyle</c>, for example
    /// <c>Bold</c> or <c>Bold, Italic</c>.
    /// </summary>
    public string FontStyle { get; set; } = "Bold";

    /// <summary>How often the message jumps, in milliseconds.</summary>
    public int IntervalMilliseconds { get; set; } = 3000;

    public static string DefaultFilePath
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = Path.GetTempPath();

            return Path.Combine(root, "CountdownScreenSaver", "settings.json");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static ScreensaverSettings CreateDefault()
    {
        var settings = new ScreensaverSettings();
        settings.Normalize();
        return settings;
    }

    /// <summary>
    /// Values the 2016 program used when the registry key was missing:
    /// Microsoft Sans Serif 8.25 pt, a 5 millisecond timer, and no font style.
    /// </summary>
    public static ScreensaverSettings CreateLegacyDefaults() => new()
    {
        CountdownDate = new DateTime(2016, 11, 25),
        CountdownText = "Enter text here",
        FontName = "Microsoft Sans Serif",
        FontSize = 8.25f,
        FontStyle = "Regular",
        IntervalMilliseconds = 5
    };

    public static bool IsLegacyPlaceholder(ScreensaverSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.CountdownDate.Date == new DateTime(2016, 11, 25)
            && settings.CountdownText == "Enter text here"
            && string.Equals(settings.FontName, "Microsoft Sans Serif", StringComparison.OrdinalIgnoreCase)
            && Math.Abs(settings.FontSize - 8.25f) < 0.001f
            && settings.IntervalMilliseconds == 5;
    }

    public void Normalize()
    {
        CountdownText ??= "";
        if (CountdownDate.Year is < 1753 or > 9998)
            CountdownDate = new DateTime(2016, 11, 25);

        if (string.IsNullOrWhiteSpace(FontName))
            FontName = "Broadway";

        if (float.IsNaN(FontSize) || float.IsInfinity(FontSize) || FontSize <= 0)
            FontSize = 48f;

        FontSize = Math.Clamp(FontSize, 1f, 500f);

        if (string.IsNullOrWhiteSpace(FontStyle))
            FontStyle = "Bold";

        IntervalMilliseconds = Math.Clamp(IntervalMilliseconds, 50, 3_600_000);
    }

    public void Save(string? path = null)
    {
        var file = string.IsNullOrWhiteSpace(path) ? DefaultFilePath : path;
        var directory = Path.GetDirectoryName(file);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("Settings path has no directory.");

        Normalize();
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(file, json + Environment.NewLine);
    }

    /// <summary>
    /// Loads JSON from <paramref name="path"/>. A missing or unreadable file
    /// returns the current defaults. This method does not read the registry.
    /// </summary>
    public static ScreensaverSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var loaded = TryRead(path) ?? new ScreensaverSettings();
        loaded.Normalize();
        return loaded;
    }

    public static bool TryParseLegacyDate(string? text, out DateTime value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out value)
            || DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out value);
    }

    public static bool TryParseLegacyFloat(string? text, out float value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static ScreensaverSettings? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScreensaverSettings>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
