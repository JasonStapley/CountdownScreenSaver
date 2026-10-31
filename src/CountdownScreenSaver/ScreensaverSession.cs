namespace CountdownScreenSaver;

internal static class ScreensaverSession
{
    public static void RunFullscreen()
    {
        var settings = SettingsStore.Load();
        var screens = Screen.AllScreens;
        if (screens.Length == 0)
            return;

        var forms = new List<ScreensaverForm>(screens.Length);
        var context = new ApplicationContext();
        var shutdown = false;

        foreach (var screen in screens)
        {
            var form = new ScreensaverForm(settings, screen, previewParent: IntPtr.Zero);
            form.FormClosed += (_, _) =>
            {
                if (shutdown)
                    return;

                shutdown = true;
                foreach (var other in forms)
                {
                    if (!ReferenceEquals(other, form) && !other.IsDisposed)
                        other.Close();
                }

                context.ExitThread();
            };
            forms.Add(form);
        }

        foreach (var form in forms)
            form.Show();

        Application.Run(context);
    }

    public static void RunSettings(long parentHandle)
    {
        var settings = SettingsStore.Load();
        using var form = new SettingsForm(settings);
        if (parentHandle != 0)
        {
            var owner = new IntPtr(parentHandle);
            if (NativeMethods.IsWindow(owner))
            {
                form.ShowDialog(new OwnerWindow(owner));
                return;
            }
        }

        form.ShowDialog();
    }

    public static void RunPreview(long parentHandle)
    {
        if (parentHandle == 0)
            return;

        var parent = new IntPtr(parentHandle);
        if (!NativeMethods.IsWindow(parent))
            return;

        var settings = SettingsStore.Load();
        using var form = new ScreensaverForm(settings, screen: null, previewParent: parent);
        Application.Run(form);
    }

    private sealed class OwnerWindow(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
