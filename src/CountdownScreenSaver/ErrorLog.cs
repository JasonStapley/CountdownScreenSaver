namespace CountdownScreenSaver;

internal static class ErrorLog
{
    public static void Write(Exception exception)
    {
        try
        {
            var directory = Path.GetDirectoryName(ScreensaverSettings.DefaultFilePath);
            if (string.IsNullOrEmpty(directory))
                return;

            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "error.log");
            File.AppendAllText(path, $"{DateTimeOffset.Now:u}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // The screensaver has no console. Drop the log rather than crash again.
        }
    }
}
