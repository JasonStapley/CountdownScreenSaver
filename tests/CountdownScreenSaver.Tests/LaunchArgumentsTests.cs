using Xunit;

namespace CountdownScreenSaver.Tests;

public sealed class LaunchArgumentsTests
{
    [Fact]
    public void No_arguments_runs_fullscreen()
    {
        var launch = LaunchArguments.Parse([]);
        Assert.Equal(ScreensaverMode.Fullscreen, launch.Mode);
        Assert.Equal(0, launch.ParentHandle);
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/S")]
    [InlineData("-s")]
    public void Show_switch_runs_fullscreen(string argument)
    {
        var launch = LaunchArguments.Parse([argument]);
        Assert.Equal(ScreensaverMode.Fullscreen, launch.Mode);
    }

    [Theory]
    [InlineData("/c", null, 0L)]
    [InlineData("/c:42", null, 42L)]
    [InlineData("/C: 15", null, 15L)]
    [InlineData("/c", "99", 99L)]
    public void Configure_switch_opens_settings(string first, string? second, long handle)
    {
        string[] args = second is null ? [first] : [first, second];
        var launch = LaunchArguments.Parse(args);
        Assert.Equal(ScreensaverMode.Settings, launch.Mode);
        Assert.Equal(handle, launch.ParentHandle);
    }

    [Theory]
    [InlineData("/p", "1234", 1234L)]
    [InlineData("/p:7", null, 7L)]
    [InlineData("/P", "0", 0L)]
    public void Preview_switch_keeps_the_parent_handle(string first, string? second, long handle)
    {
        string[] args = second is null ? [first] : [first, second];
        var launch = LaunchArguments.Parse(args);
        Assert.Equal(ScreensaverMode.Preview, launch.Mode);
        Assert.Equal(handle, launch.ParentHandle);
    }

    [Fact]
    public void Unknown_switch_does_nothing()
    {
        var launch = LaunchArguments.Parse(["/unknown"]);
        Assert.Equal(ScreensaverMode.None, launch.Mode);
    }

    [Fact]
    public void Unparseable_handle_is_zero()
    {
        var launch = LaunchArguments.Parse(["/p:not-a-window"]);
        Assert.Equal(ScreensaverMode.Preview, launch.Mode);
        Assert.Equal(0, launch.ParentHandle);
    }
}
