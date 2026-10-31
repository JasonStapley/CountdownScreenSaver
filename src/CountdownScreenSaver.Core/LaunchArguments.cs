using System.Globalization;

namespace CountdownScreenSaver;

public enum ScreensaverMode
{
    /// <summary>Unrecognized arguments. The process exits without opening a window.</summary>
    None,

    /// <summary>Full-screen countdown. Used for <c>/s</c> and for a launch with no arguments.</summary>
    Fullscreen,

    /// <summary>Settings dialog. Used for <c>/c</c>.</summary>
    Settings,

    /// <summary>Preview embedded in the Windows display settings window. Used for <c>/p</c>.</summary>
    Preview
}

public readonly record struct LaunchRequest(ScreensaverMode Mode, long ParentHandle);

/// <summary>
/// Parses the command line Windows passes to a .scr file.
/// </summary>
public static class LaunchArguments
{
    public static LaunchRequest Parse(IReadOnlyList<string>? args)
    {
        if (args is null || args.Count == 0 || string.IsNullOrWhiteSpace(args[0]))
            return new LaunchRequest(ScreensaverMode.Fullscreen, 0);

        var raw = args[0].Trim();
        string? attached = null;
        var colon = raw.IndexOf(':');
        if (colon >= 0)
        {
            attached = raw[(colon + 1)..].Trim();
            raw = raw[..colon];
        }

        var switchName = raw.TrimStart('/', '-').Trim();
        var handleText = string.IsNullOrEmpty(attached) && args.Count > 1
            ? args[1].Trim()
            : attached;

        if (switchName.Equals("s", StringComparison.OrdinalIgnoreCase))
            return new LaunchRequest(ScreensaverMode.Fullscreen, 0);

        if (switchName.Equals("c", StringComparison.OrdinalIgnoreCase))
            return new LaunchRequest(ScreensaverMode.Settings, ParseHandle(handleText));

        if (switchName.Equals("p", StringComparison.OrdinalIgnoreCase))
            return new LaunchRequest(ScreensaverMode.Preview, ParseHandle(handleText));

        return new LaunchRequest(ScreensaverMode.None, 0);
    }

    private static long ParseHandle(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Trim().Trim('"');
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}
