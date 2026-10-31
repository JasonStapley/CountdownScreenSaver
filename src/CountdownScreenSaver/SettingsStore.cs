namespace CountdownScreenSaver;

/// <summary>
/// Reads <c>%AppData%\CountdownScreenSaver\settings.json</c>.
/// When that file does not exist yet, a customized
/// <c>HKCU\CountdownScreenSaver</c> key from the 2016 program is used.
/// Untouched registry defaults are ignored so a new install gets the
/// Broadway 48 pt look instead of 8.25 pt Microsoft Sans Serif.
/// </summary>
internal static class SettingsStore
{
    public static ScreensaverSettings Load()
    {
        var path = ScreensaverSettings.DefaultFilePath;
        if (File.Exists(path))
            return ScreensaverSettings.Load(path);

        var legacy = LegacyRegistry.TryRead();
        if (legacy is not null && !ScreensaverSettings.IsLegacyPlaceholder(legacy))
        {
            legacy.Normalize();
            return legacy;
        }

        return ScreensaverSettings.CreateDefault();
    }
}
