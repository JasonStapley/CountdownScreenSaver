namespace CountdownScreenSaver;

internal static class FontFactory
{
    public static Font Create(string? family, float size, string? styleName)
    {
        var fontSize = size is > 0 and <= 500 ? size : 48f;
        var style = ParseStyle(styleName);

        foreach (var name in Candidates(family))
        {
            var font = TryCreate(name, fontSize, style) ?? TryCreate(name, fontSize, FontStyle.Regular);
            if (font is not null)
                return font;
        }

        return new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static FontStyle ParseStyle(string? styleName)
    {
        if (!string.IsNullOrWhiteSpace(styleName)
            && Enum.TryParse<FontStyle>(styleName, ignoreCase: true, out var style))
        {
            return style;
        }

        return FontStyle.Bold;
    }

    private static IEnumerable<string> Candidates(string? family)
    {
        if (!string.IsNullOrWhiteSpace(family))
            yield return family.Trim();

        yield return "Broadway";
        yield return "Segoe UI";
        yield return "Arial";
    }

    private static Font? TryCreate(string family, float size, FontStyle style)
    {
        try
        {
            return new Font(family, size, style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
