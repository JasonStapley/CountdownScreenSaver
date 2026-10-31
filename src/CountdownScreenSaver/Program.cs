namespace CountdownScreenSaver;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var launch = LaunchArguments.Parse(args);
        try
        {
            switch (launch.Mode)
            {
                case ScreensaverMode.Settings:
                    ScreensaverSession.RunSettings(launch.ParentHandle);
                    break;
                case ScreensaverMode.Preview:
                    ScreensaverSession.RunPreview(launch.ParentHandle);
                    break;
                case ScreensaverMode.Fullscreen:
                    ScreensaverSession.RunFullscreen();
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
            if (launch.Mode != ScreensaverMode.Fullscreen)
            {
                MessageBox.Show(
                    ex.Message,
                    "Countdown Screen Saver",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
