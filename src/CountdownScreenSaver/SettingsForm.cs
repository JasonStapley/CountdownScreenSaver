using System.Globalization;

namespace CountdownScreenSaver;

internal sealed class SettingsForm : Form
{
    private readonly ScreensaverSettings _settings;
    private readonly TextBox _message = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Long, Width = 280 };
    private readonly Label _fontLabel = new()
    {
        AutoSize = true,
        MaximumSize = new Size(420, 0),
        Anchor = AnchorStyles.Left
    };
    private readonly NumericUpDown _interval = new()
    {
        Minimum = 50,
        Maximum = 3_600_000,
        Increment = 100,
        ThousandsSeparator = true,
        Width = 120
    };
    private readonly Button _chooseFont = new() { Text = "&Choose Font", AutoSize = true };
    private readonly FontDialog _fontDialog = new() { ShowColor = false, ShowEffects = true };
    private readonly Panel _preview = new() { BackColor = Color.Black, Dock = DockStyle.Fill };
    private readonly Button _ok = new()
    {
        Text = "&OK",
        AutoSize = true,
        MinimumSize = new Size(88, 28),
        Margin = new Padding(4)
    };
    private readonly Button _cancel = new()
    {
        Text = "&Cancel",
        DialogResult = DialogResult.Cancel,
        AutoSize = true,
        MinimumSize = new Size(88, 28),
        Margin = new Padding(4)
    };

    private string _fontName;
    private float _fontSize;
    private FontStyle _fontStyle;
    private Font? _previewFont;
    private bool _disposed;

    public SettingsForm(ScreensaverSettings settings)
    {
        _settings = settings;
        _fontName = settings.FontName;
        _fontSize = settings.FontSize;
        _fontStyle = FontFactory.ParseStyle(settings.FontStyle);

        Text = "Countdown ScreenSaver Configuration Author Jason Stapley";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(12);
        ClientSize = new Size(700, 400);
        AcceptButton = _ok;
        CancelButton = _cancel;

        _message.Text = settings.CountdownText;
        _date.Value = ClampDate(settings.CountdownDate);
        _interval.Value = Math.Clamp(settings.IntervalMilliseconds, (int)_interval.Minimum, (int)_interval.Maximum);

        _chooseFont.Click += ChooseFont;
        _ok.Click += SaveAndClose;
        _preview.Paint += PaintPreview;
        _message.TextChanged += (_, _) => _preview.Invalidate();
        _date.ValueChanged += (_, _) => _preview.Invalidate();

        Controls.Add(BuildLayout());
        ApplyPreviewFont();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _previewFont?.Dispose();
            _previewFont = null;
            _fontDialog.Dispose();
        }

        base.Dispose(disposing);
    }

    private TableLayoutPanel BuildLayout()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var row = 0; row < 6; row++)
        {
            table.RowStyles.Add(row == 4
                ? new RowStyle(SizeType.Percent, 100f)
                : new RowStyle(SizeType.AutoSize));
        }

        table.Controls.Add(FieldLabel("Countdown text"), 0, 0);
        table.Controls.Add(_message, 1, 0);
        table.SetColumnSpan(_message, 2);

        table.Controls.Add(FieldLabel("Countdown date"), 0, 1);
        table.Controls.Add(_date, 1, 1);

        table.Controls.Add(FieldLabel("Font"), 0, 2);
        table.Controls.Add(_fontLabel, 1, 2);
        table.Controls.Add(_chooseFont, 2, 2);

        table.Controls.Add(FieldLabel("Jump interval"), 0, 3);
        table.Controls.Add(_interval, 1, 3);
        table.Controls.Add(FieldLabel("milliseconds"), 2, 3);

        table.Controls.Add(_preview, 0, 4);
        table.SetColumnSpan(_preview, 3);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0)
        };
        // Right-to-left places the first control on the right: Cancel, then OK.
        buttons.Controls.Add(_cancel);
        buttons.Controls.Add(_ok);
        table.Controls.Add(buttons, 0, 5);
        table.SetColumnSpan(buttons, 3);
        return table;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 8, 12, 8)
    };

    private void ChooseFont(object? sender, EventArgs e)
    {
        var next = FontFactory.Create(_fontName, Math.Clamp(_fontSize, 1f, 500f), _fontStyle.ToString());
        _fontDialog.Font = next;

        if (_fontDialog.ShowDialog(this) != DialogResult.OK)
            return;

        var chosen = _fontDialog.Font;
        _fontName = chosen.FontFamily.Name;
        _fontSize = chosen.SizeInPoints;
        _fontStyle = chosen.Style;
        ApplyPreviewFont();
    }

    private void ApplyPreviewFont()
    {
        _fontLabel.Text = string.Format(
            CultureInfo.CurrentCulture,
            "{0}, {1:0.##} pt, {2}",
            _fontName,
            _fontSize,
            _fontStyle);

        var next = FontFactory.Create(_fontName, Math.Clamp(_fontSize, 8f, 22f), _fontStyle.ToString());
        var previous = _previewFont;
        _previewFont = next;
        previous?.Dispose();
        _preview.Invalidate();
    }

    private void PaintPreview(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Black);
        var text = CountdownText.Format(_date.Value, _message.Text, DateTime.Today);
        var font = _previewFont ?? SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        var flags = TextFormatFlags.WordBreak
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(
            e.Graphics,
            text,
            font,
            _preview.ClientRectangle,
            SystemColors.Info,
            Color.Black,
            flags);
    }

    private void SaveAndClose(object? sender, EventArgs e)
    {
        try
        {
            _settings.CountdownDate = _date.Value;
            _settings.CountdownText = _message.Text;
            _settings.FontName = _fontName;
            _settings.FontSize = _fontSize;
            _settings.FontStyle = _fontStyle.ToString();
            _settings.IntervalMilliseconds = (int)_interval.Value;
            _settings.Normalize();
            _settings.Save();
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(
                this,
                "Could not save settings." + Environment.NewLine + ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static DateTime ClampDate(DateTime value)
    {
        var min = new DateTime(1753, 1, 1);
        var max = new DateTime(9998, 12, 31, 23, 59, 59);
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }
}
