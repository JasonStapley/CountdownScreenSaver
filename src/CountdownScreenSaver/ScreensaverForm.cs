namespace CountdownScreenSaver;

/// <summary>
/// Black full-screen countdown. The sentence is drawn in one buffered paint,
/// then moved and recolored on the configured interval.
/// </summary>
internal sealed class ScreensaverForm : Form
{
    private const int MovementThresholdPixels = 10;

    private readonly ScreensaverSettings _settings;
    private readonly Screen? _screen;
    private readonly IntPtr _previewParent;
    private readonly bool _isPreview;
    private readonly Random _random = new();
    private readonly System.Windows.Forms.Timer _timer;

    private Font? _font;
    private Size _fontFittedTo;
    private string _line = "";
    private Point _origin;
    private Size _measured;
    private Color _color = Color.White;
    private TextFormatFlags _flags;
    private Point? _anchor;
    private bool _started;
    private bool _advancing;
    private bool _cursorHidden;
    private bool _disposed;

    public ScreensaverForm(ScreensaverSettings settings, Screen? screen, IntPtr previewParent)
    {
        _settings = settings;
        _screen = screen;
        _previewParent = previewParent;
        _isPreview = previewParent != IntPtr.Zero;

        Text = "ScreenSaver";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        WindowState = FormWindowState.Normal;
        BackColor = Color.Black;
        ShowInTaskbar = false;
        TopMost = !_isPreview;
        KeyPreview = true;
        ControlBox = false;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
        UpdateStyles();

        if (!_isPreview && screen is not null)
            DesktopBounds = screen.Bounds;

        _timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Clamp(settings.IntervalMilliseconds, 50, 3_600_000)
        };
        _timer.Tick += OnTick;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            if (_isPreview)
            {
                parameters.Style |= NativeMethods.WsChild | NativeMethods.WsVisible;
                parameters.Style &= ~(NativeMethods.WsPopup | NativeMethods.WsCaption | NativeMethods.WsThickFrame);
                parameters.Parent = _previewParent;
            }
            else
            {
                parameters.Style |= NativeMethods.WsPopup | NativeMethods.WsVisible;
                parameters.ExStyle |= NativeMethods.WsExTopmost | NativeMethods.WsExToolWindow;
                parameters.ExStyle &= ~NativeMethods.WsExAppWindow;
            }

            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!_isPreview)
            return;

        NativeMethods.SetParent(Handle, _previewParent);
        FitToParent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!_isPreview && _screen is not null)
        {
            // Screen.Bounds covers the taskbar. WorkingArea would leave a gap.
            // DesktopBounds keeps monitors that sit left of or above the primary.
            DesktopBounds = _screen.Bounds;
        }
        else if (_isPreview)
        {
            FitToParent();
        }

        Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Start();
        if (_isPreview)
            return;

        // Hide after the window is on screen. Hiding earlier is undone when the window appears.
        HideCursor();
        Activate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_started && !_advancing && WindowState != FormWindowState.Minimized)
            Advance();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Black);
        if (_font is null || _measured.Width <= 0 || _measured.Height <= 0 || _line.Length == 0)
            return;

        var rectangle = new Rectangle(_origin, _measured);
        TextRenderer.DrawText(e.Graphics, _line, _font, rectangle, _color, Color.Black, _flags);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isPreview)
            return;

        var now = Cursor.Position;
        if (_anchor is null)
        {
            _anchor = now;
            return;
        }

        var delta = Math.Abs(now.X - _anchor.Value.X) + Math.Abs(now.Y - _anchor.Value.Y);
        if (delta >= MovementThresholdPixels)
            Close();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_isPreview)
            Close();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!_isPreview)
            Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_isPreview)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        ShowCursor();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            ShowCursor();
            _timer.Tick -= OnTick;
            _timer.Dispose();
            _font?.Dispose();
            _font = null;
        }

        base.Dispose(disposing);
    }

    private void Start()
    {
        if (_started || IsDisposed)
            return;

        _started = true;
        if (!_isPreview)
            _anchor = Cursor.Position;

        Advance();
        _timer.Start();
    }

    private void HideCursor()
    {
        if (_cursorHidden)
            return;

        Cursor.Hide();
        _cursorHidden = true;
    }

    private void ShowCursor()
    {
        if (!_cursorHidden)
            return;

        Cursor.Show();
        _cursorHidden = false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isPreview && !NativeMethods.IsWindow(_previewParent))
        {
            _timer.Stop();
            Close();
            return;
        }

        Advance();
    }

    private void Advance()
    {
        if (_advancing || IsDisposed)
            return;

        _advancing = true;
        try
        {
            if (_isPreview)
                FitToParent();

            _line = CountdownText.Format(_settings.CountdownDate, _settings.CountdownText, DateTime.Today);
            EnsureFont();
            if (_font is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                Invalidate();
                return;
            }

            var singleLine = TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.Left
                | TextFormatFlags.Top;
            var single = TextRenderer.MeasureText(_line, _font, new Size(int.MaxValue, int.MaxValue), singleLine);
            var availableWidth = Math.Max(1, ClientSize.Width - 16);
            if (single.Width <= availableWidth)
            {
                _flags = singleLine;
                _measured = single;
            }
            else
            {
                _flags = TextFormatFlags.WordBreak
                    | TextFormatFlags.NoPadding
                    | TextFormatFlags.NoPrefix
                    | TextFormatFlags.Left
                    | TextFormatFlags.Top;
                _measured = TextRenderer.MeasureText(_line, _font, new Size(availableWidth, int.MaxValue), _flags);
            }

            var maxX = Math.Max(0, ClientSize.Width - _measured.Width);
            var maxY = Math.Max(0, ClientSize.Height - _measured.Height);
            _origin = new Point(_random.Next(0, maxX + 1), _random.Next(0, maxY + 1));
            _color = Color.FromArgb(_random.Next(1, 255), _random.Next(1, 255), _random.Next(1, 255));
            Invalidate();
        }
        finally
        {
            _advancing = false;
        }
    }

    private void EnsureFont()
    {
        if (_font is not null && _fontFittedTo == ClientSize && ClientSize.Width > 0 && ClientSize.Height > 0)
            return;

        var size = _settings.FontSize;
        Font? created = null;
        if (!_isPreview || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            created = FontFactory.Create(_settings.FontName, Math.Max(6f, size), _settings.FontStyle);
        }
        else
        {
            while (true)
            {
                created?.Dispose();
                created = FontFactory.Create(_settings.FontName, Math.Max(6f, size), _settings.FontStyle);
                var sample = _line.Length == 0 ? "Ag" : _line;
                var measured = TextRenderer.MeasureText(
                    sample,
                    created,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                var fits = measured.Width <= Math.Max(1, ClientSize.Width - 8)
                    && measured.Height <= Math.Max(1, ClientSize.Height - 8);
                if (fits || size <= 6f)
                    break;

                var next = Math.Max(6f, size * 0.72f);
                if (Math.Abs(next - size) < 0.05f)
                    break;

                size = next;
            }
        }

        _font?.Dispose();
        _font = created;
        _fontFittedTo = ClientSize;
    }

    private void FitToParent()
    {
        if (!NativeMethods.GetClientRect(_previewParent, out var rect))
            return;

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);
        if (Location != Point.Empty)
            Location = Point.Empty;

        if (Size != new Size(width, height))
            Size = new Size(width, height);
    }
}
