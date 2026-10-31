using Xunit;

namespace CountdownScreenSaver.Tests;

public sealed class ScreensaverSettingsTests
{
    [Fact]
    public void Round_trips_json()
    {
        var settings = new ScreensaverSettings
        {
            CountdownDate = new DateTime(2030, 1, 2, 3, 4, 5),
            CountdownText = "New Year",
            FontName = "Broadway",
            FontSize = 48f,
            FontStyle = "Bold, Italic",
            IntervalMilliseconds = 2500
        };

        var path = Path.Combine(Path.GetTempPath(), "countdown-tests", Guid.NewGuid().ToString("N"), "settings.json");
        settings.Save(path);

        var json = File.ReadAllText(path);
        Assert.Contains("\"countdownText\"", json, StringComparison.Ordinal);
        Assert.Contains("\"intervalMilliseconds\"", json, StringComparison.Ordinal);

        var loaded = ScreensaverSettings.Load(path);
        Assert.Equal(new DateTime(2030, 1, 2, 3, 4, 5), loaded.CountdownDate);
        Assert.Equal("New Year", loaded.CountdownText);
        Assert.Equal("Broadway", loaded.FontName);
        Assert.Equal(48f, loaded.FontSize);
        Assert.Equal("Bold, Italic", loaded.FontStyle);
        Assert.Equal(2500, loaded.IntervalMilliseconds);
    }

    [Fact]
    public void Missing_or_corrupt_file_returns_the_new_defaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), "countdown-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var missing = Path.Combine(directory, "missing.json");
        var missingSettings = ScreensaverSettings.Load(missing);
        Assert.Equal(new DateTime(2016, 11, 25), missingSettings.CountdownDate);
        Assert.Equal("Broadway", missingSettings.FontName);
        Assert.Equal(48f, missingSettings.FontSize);
        Assert.Equal("Bold", missingSettings.FontStyle);
        Assert.Equal(3000, missingSettings.IntervalMilliseconds);

        var corrupt = Path.Combine(directory, "corrupt.json");
        File.WriteAllText(corrupt, "{not json");
        var corruptSettings = ScreensaverSettings.Load(corrupt);
        Assert.Equal("Enter text here", corruptSettings.CountdownText);
        Assert.Equal(3000, corruptSettings.IntervalMilliseconds);
    }

    [Fact]
    public void Normalize_rejects_a_zero_interval_and_a_blank_font()
    {
        var settings = new ScreensaverSettings
        {
            FontName = " ",
            FontSize = 0,
            FontStyle = "",
            IntervalMilliseconds = 5,
            CountdownDate = default
        };

        settings.Normalize();

        Assert.Equal("Broadway", settings.FontName);
        Assert.Equal(48f, settings.FontSize);
        Assert.Equal("Bold", settings.FontStyle);
        Assert.Equal(50, settings.IntervalMilliseconds);
        Assert.Equal(new DateTime(2016, 11, 25), settings.CountdownDate);
    }

    [Fact]
    public void Legacy_placeholder_matches_only_the_2016_factory_values()
    {
        var legacy = ScreensaverSettings.CreateLegacyDefaults();
        Assert.True(ScreensaverSettings.IsLegacyPlaceholder(legacy));

        legacy.CountdownText = "Birthday";
        Assert.False(ScreensaverSettings.IsLegacyPlaceholder(legacy));
    }

    [Theory]
    [InlineData("2016-11-25")]
    [InlineData("2016-11-25T00:00:00")]
    public void Parses_legacy_dates(string text)
    {
        Assert.True(ScreensaverSettings.TryParseLegacyDate(text, out var value));
        Assert.Equal(new DateTime(2016, 11, 25), value);
    }

    [Fact]
    public void Parses_the_legacy_font_size()
    {
        Assert.True(ScreensaverSettings.TryParseLegacyFloat("8.25", out var size));
        Assert.Equal(8.25f, size);
    }
}
