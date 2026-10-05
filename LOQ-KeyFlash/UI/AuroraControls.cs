using System.Drawing.Drawing2D;

namespace LoqKeyFlash.UI;

internal static class AuroraPalette
{
    // AURORA catalog dark tokens: neutral graphite only, with red reserved for danger.
    public static readonly Color Canvas = Color.FromArgb(5, 5, 5);
    public static readonly Color Chrome = Color.FromArgb(8, 8, 8);
    public static readonly Color Surface = Color.FromArgb(11, 11, 11);
    public static readonly Color SurfaceRaised = Color.FromArgb(16, 16, 16);
    public static readonly Color SurfaceHover = Color.FromArgb(23, 23, 23);
    public static readonly Color Border = Color.FromArgb(80, 80, 80);
    public static readonly Color BorderSoft = Color.FromArgb(36, 36, 36);
    public static readonly Color Text = Color.FromArgb(244, 244, 241);
    public static readonly Color Muted = Color.FromArgb(141, 141, 136);
    public static readonly Color Accent = Color.FromArgb(244, 244, 241);
    public static readonly Color AccentDim = Color.FromArgb(101, 101, 97);
    public static readonly Color Danger = Color.FromArgb(166, 70, 70);
    public static readonly Color DangerHover = Color.FromArgb(184, 78, 78);
    public static readonly Color Scribe = Color.FromArgb(112, 80, 80, 80);
    public static readonly Color ScribeStrong = Color.FromArgb(155, 92, 92, 92);
}

internal static class AuroraGeometry
{
    public static int CutDepth(Rectangle rectangle, int maximum = 18, int minimum = 3)
    {
        var shorterSide = Math.Max(0, Math.Min(rectangle.Width, rectangle.Height));
        return Math.Clamp((int)Math.Round(shorterSide * 0.125), minimum, maximum);
    }

    public static GraphicsPath CutPath(Rectangle rectangle, int cut)
    {
        var x = rectangle.X;
        var y = rectangle.Y;
        var right = rectangle.Right - 1;
        var bottom = rectangle.Bottom - 1;
        cut = Math.Max(0, Math.Min(cut, Math.Min(rectangle.Width, rectangle.Height) / 3));

        var path = new GraphicsPath();
        path.AddPolygon(new Point[]
        {
            new Point(x + cut, y),
            new Point(right, y),
            new Point(right, bottom - cut),
            new Point(right - cut, bottom),
            new Point(x, bottom),
            new Point(x, y + cut)
        });
        path.CloseFigure();
        return path;
    }

    public static Color Mix(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * amount),
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }
}

internal static class BrandAssets
{
    public static Bitmap LoadBitmap(string fileName)
    {
        var assembly = typeof(BrandAssets).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($".Resources.{fileName}", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded brand asset is missing: {fileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded brand asset could not be opened: {fileName}");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }
}

internal sealed class AuroraPanel : Panel
{
    public AuroraPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = AuroraGeometry.CutPath(ClientRectangle, AuroraGeometry.CutDepth(ClientRectangle));
        using var fill = new SolidBrush(AuroraPalette.Surface);
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var cut = AuroraGeometry.CutDepth(ClientRectangle);
        var borderBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var path = AuroraGeometry.CutPath(borderBounds, Math.Max(3, cut - 1));
        using var border = new Pen(AuroraPalette.Border, 1.15f);
        e.Graphics.DrawPath(border, path);

        using var scribe = new Pen(AuroraPalette.ScribeStrong);
        using var fine = new Pen(AuroraPalette.Scribe);
        var right = Width - 26;
        e.Graphics.DrawLines(fine, new Point[]
        {
            new Point(right - 82, 13),
            new Point(right - 24, 13),
            new Point(right - 14, 23),
            new Point(right + 8, 23)
        });
        e.Graphics.DrawLines(fine, new Point[]
        {
            new Point(14, Height - 20),
            new Point(34, Height - 20),
            new Point(43, Height - 11),
            new Point(104, Height - 11)
        });
        e.Graphics.DrawLine(scribe, cut + 4, 8, cut + 34, 8);
        e.Graphics.DrawLine(scribe, Width - cut - 38, Height - 8, Width - cut - 4, Height - 8);
        e.Graphics.DrawRectangle(fine, Width - 32, 34, 5, 5);
        e.Graphics.DrawRectangle(fine, 18, Height - 39, 4, 4);
        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        using var path = AuroraGeometry.CutPath(ClientRectangle, AuroraGeometry.CutDepth(ClientRectangle));
        Region = new Region(path);
    }
}

internal sealed class AuroraBrandBadge : Control
{
    private Image? _image;

    public Image? Image
    {
        get => _image;
        set
        {
            _image = value;
            Invalidate();
        }
    }

    public AuroraBrandBadge()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = AuroraGeometry.CutPath(ClientRectangle,
            AuroraGeometry.CutDepth(ClientRectangle, 15, 6));
        using var fill = new SolidBrush(AuroraPalette.Canvas);
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var cut = AuroraGeometry.CutDepth(ClientRectangle, 15, 6);
        var outerBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var outerPath = AuroraGeometry.CutPath(outerBounds, Math.Max(5, cut - 1));
        using var outerPen = new Pen(AuroraPalette.Border, 1.15f);
        e.Graphics.DrawPath(outerPen, outerPath);

        var innerBounds = Rectangle.Inflate(ClientRectangle, -6, -6);
        using var innerPath = AuroraGeometry.CutPath(innerBounds, Math.Max(4, cut - 2));
        using var innerPen = new Pen(AuroraPalette.BorderSoft);
        e.Graphics.DrawPath(innerPen, innerPath);

        if (_image is not null)
        {
            const int padding = 14;
            var imageBounds = Rectangle.Inflate(ClientRectangle, -padding, -padding);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(_image, imageBounds);
        }

        using var scribe = new Pen(AuroraPalette.ScribeStrong);
        e.Graphics.DrawLine(scribe, cut + 3, 7, cut + 24, 7);
        e.Graphics.DrawLine(scribe, Width - cut - 25, Height - 8, Width - cut - 4, Height - 8);
        e.Graphics.DrawRectangle(scribe, Width - 18, 15, 4, 4);

        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = AuroraGeometry.CutPath(ClientRectangle,
            AuroraGeometry.CutDepth(ClientRectangle, 15, 6));
        Region = new Region(path);
    }
}

internal sealed class AuroraCheckBox : CheckBox
{
    private bool _hovered;

    public AuroraCheckBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        AutoSize = false;
        Height = 28;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        Font = new Font("Space Grotesk", 10f, FontStyle.Regular);
        ForeColor = AuroraPalette.Text;
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs eventargs)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(eventargs);
    }

    protected override void OnMouseLeave(EventArgs eventargs)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(eventargs);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(AuroraPalette.Surface);

        var box = new Rectangle(1, 5, 18, 18);
        using var boxPath = AuroraGeometry.CutPath(box, 3);
        var fillColor = Checked
            ? AuroraPalette.Text
            : (_hovered ? AuroraPalette.SurfaceRaised : AuroraPalette.Canvas);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(Checked ? AuroraPalette.Accent : AuroraPalette.Border);
        e.Graphics.FillPath(fill, boxPath);
        e.Graphics.DrawPath(border, boxPath);

        if (Checked)
        {
            using var check = new Pen(AuroraPalette.Canvas, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.DrawLines(check, new Point[]
            {
                new Point(5, 14),
                new Point(9, 17),
                new Point(15, 9)
            });
        }

        var textColor = Enabled ? ForeColor : AuroraPalette.Muted;
        TextRenderer.DrawText(e.Graphics, Text, Font,
            new Rectangle(30, 1, Width - 31, Height - 2), textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            using var focus = new Pen(AuroraPalette.AccentDim) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawRectangle(focus, 28, 3, Width - 30, Height - 7);
        }
    }
}

internal sealed class AuroraButton : Button
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private float _hoverAmount;
    private float _hoverTarget;
    private bool _pressed;

    public bool IsDanger { get; set; }

    public AuroraButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Space Grotesk Medium", 8.8f, FontStyle.Bold);
        ForeColor = AuroraPalette.Text;
        BackColor = Color.Transparent;
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += (_, _) => Animate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hoverTarget = 1f;
        _animationTimer.Start();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverTarget = 0f;
        _pressed = false;
        _animationTimer.Start();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    private void Animate()
    {
        const float step = 0.1f;
        if (Math.Abs(_hoverAmount - _hoverTarget) <= step)
        {
            _hoverAmount = _hoverTarget;
            _animationTimer.Stop();
        }
        else
        {
            _hoverAmount += _hoverAmount < _hoverTarget ? step : -step;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(ResolveParentBackground());
        var restingCut = AuroraGeometry.CutDepth(ClientRectangle, 12, 4);
        using var fillPath = AuroraGeometry.CutPath(ClientRectangle, restingCut);

        var borderBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var borderPath = AuroraGeometry.CutPath(borderBounds, Math.Max(3, restingCut - 1));

        var baseColor = IsDanger
            ? AuroraPalette.Danger
            : (Enabled ? AuroraPalette.SurfaceRaised : AuroraPalette.Surface);
        var hoverColor = IsDanger
            ? AuroraPalette.DangerHover
            : (Enabled ? AuroraPalette.SurfaceHover : AuroraPalette.Surface);
        var fillColor = AuroraGeometry.Mix(baseColor, hoverColor, _hoverAmount);
        if (_pressed)
            fillColor = IsDanger ? Color.FromArgb(132, 49, 49) : AuroraPalette.Canvas;

        using var fill = new SolidBrush(fillColor);
        var borderColor = IsDanger
            ? AuroraGeometry.Mix(AuroraPalette.Danger, Color.White, 0.18f + _hoverAmount * 0.12f)
            : AuroraGeometry.Mix(AuroraPalette.Border, AuroraPalette.AccentDim, _hoverAmount);
        using var border = new Pen(borderColor);
        pevent.Graphics.FillPath(fill, fillPath);
        pevent.Graphics.DrawPath(border, borderPath);

        var textBounds = ClientRectangle;
        if (_pressed)
            textBounds.Offset(0, 1);
        if (Image is not null && string.IsNullOrWhiteSpace(Text))
        {
            var imageSize = Math.Min(18, Math.Min(Width - 8, Height - 8));
            var imageBounds = new Rectangle((Width - imageSize) / 2, (Height - imageSize) / 2,
                imageSize, imageSize);
            if (_pressed) imageBounds.Offset(0, 1);
            pevent.Graphics.DrawImage(Image, imageBounds);
        }
        else if (Image is not null)
        {
            var imageSize = Math.Min(18, Height - 12);
            var imageBounds = new Rectangle(13, (Height - imageSize) / 2, imageSize, imageSize);
            var labelBounds = new Rectangle(42, 0, Width - 52, Height);
            if (_pressed)
            {
                imageBounds.Offset(0, 1);
                labelBounds.Offset(0, 1);
            }
            pevent.Graphics.DrawImage(Image, imageBounds);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, labelBounds,
                Enabled ? ForeColor : AuroraPalette.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else
        {
            TextRenderer.DrawText(pevent.Graphics, Text, Font, textBounds,
                Enabled ? (IsDanger ? Color.White : ForeColor) : AuroraPalette.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        if (Focused && ShowFocusCues)
        {
            using var focus = new Pen(IsDanger ? Color.White : AuroraPalette.Accent) { DashStyle = DashStyle.Dot };
            pevent.Graphics.DrawRectangle(focus, 4, 4, Width - 9, Height - 9);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = AuroraGeometry.CutPath(ClientRectangle,
            AuroraGeometry.CutDepth(ClientRectangle, 12, 4));
        Region = new Region(path);
    }

    private Color ResolveParentBackground()
    {
        for (Control? control = Parent; control is not null; control = control.Parent)
        {
            if (control.BackColor.A == 255)
                return control.BackColor;
        }
        return AuroraPalette.Canvas;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _animationTimer.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class AuroraSlider : Control
{
    private int _minimum = 15;
    private int _maximum = 120;
    private int _value = 32;
    private bool _dragging;

    public event EventHandler? ValueChanged;

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum <= _minimum) _maximum = _minimum + 1;
            Value = _value;
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum + 1);
            Value = _value;
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, _minimum, _maximum);
            if (next == _value) return;
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AuroraSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable |
                 ControlStyles.SupportsTransparentBackColor, true);
        TabStop = true;
        Height = 34;
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Down)
        {
            Value--;
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.Right or Keys.Up)
        {
            Value++;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        _dragging = true;
        SetFromPointer(e.X);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
            SetFromPointer(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        base.OnMouseUp(e);
    }

    private void SetFromPointer(int x)
    {
        const int inset = 9;
        var width = Math.Max(1, Width - inset * 2);
        var ratio = Math.Clamp((x - inset) / (double)width, 0d, 1d);
        Value = _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(AuroraPalette.Surface);

        const int inset = 9;
        var centerY = Height / 2;
        var trackWidth = Math.Max(1, Width - inset * 2);
        var ratio = (_value - _minimum) / (double)(_maximum - _minimum);
        var thumbX = inset + (int)Math.Round(trackWidth * ratio);

        using var inactive = new Pen(AuroraPalette.Border, 2f);
        using var active = new Pen(AuroraPalette.Text, 2f);
        e.Graphics.DrawLine(inactive, inset, centerY, Width - inset, centerY);
        e.Graphics.DrawLine(active, inset, centerY, thumbX, centerY);

        using var tick = new Pen(AuroraPalette.BorderSoft);
        for (var i = 0; i <= 7; i++)
        {
            var x = inset + (int)Math.Round(trackWidth * (i / 7d));
            e.Graphics.DrawLine(tick, x, centerY + 8, x, centerY + 11);
        }

        var thumb = new Rectangle(thumbX - 6, centerY - 8, 12, 16);
        using var thumbPath = AuroraGeometry.CutPath(thumb, 3);
        using var thumbFill = new SolidBrush(Focused || _dragging ? AuroraPalette.Accent : AuroraPalette.Text);
        e.Graphics.FillPath(thumbFill, thumbPath);

        if (Focused && ShowFocusCues)
        {
            using var focus = new Pen(AuroraPalette.AccentDim) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawRectangle(focus, 1, 2, Width - 3, Height - 5);
        }
    }
}

internal sealed class AuroraValueBox : UserControl
{
    private readonly TextBox _textBox;
    private int _value;
    private bool _updating;

    public event EventHandler? ValueChanged;
    public int Minimum { get; set; } = 15;
    public int Maximum { get; set; } = 120;

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (next == _value && _textBox.Text == next.ToString()) return;
            _value = next;
            _updating = true;
            _textBox.Text = next.ToString();
            _updating = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AuroraValueBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Height = 34;
        Width = 92;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = AuroraPalette.SurfaceRaised,
            ForeColor = AuroraPalette.Text,
            Font = new Font("Space Grotesk Medium", 10f, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Center,
            Location = new Point(7, 8),
            Width = 48,
            TabStop = true
        };
        _textBox.TextChanged += TextBoxOnTextChanged;
        _textBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            Commit();
            e.SuppressKeyPress = true;
        };
        _textBox.Leave += (_, _) => Commit();
        Controls.Add(_textBox);
        Value = 32;
    }

    private void TextBoxOnTextChanged(object? sender, EventArgs e)
    {
        if (_updating || !int.TryParse(_textBox.Text, out var parsed))
            return;
        if (parsed < Minimum || parsed > Maximum || parsed == _value)
            return;
        _value = parsed;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Commit()
    {
        Value = int.TryParse(_textBox.Text, out var parsed)
            ? Math.Clamp(parsed, Minimum, Maximum)
            : _value;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = AuroraGeometry.CutPath(ClientRectangle, AuroraGeometry.CutDepth(ClientRectangle, 10, 4));
        using var fill = new SolidBrush(AuroraPalette.SurfaceRaised);
        e.Graphics.FillPath(fill, path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var cut = AuroraGeometry.CutDepth(ClientRectangle, 10, 4);
        var borderBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var path = AuroraGeometry.CutPath(borderBounds, Math.Max(3, cut - 1));
        using var border = new Pen(_textBox.Focused ? AuroraPalette.AccentDim : AuroraPalette.Border);
        e.Graphics.DrawPath(border, path);
        e.Graphics.DrawLine(border, 60, 6, 60, Height - 7);
        using var suffixFont = new Font("Space Grotesk", 8f);
        TextRenderer.DrawText(e.Graphics, "ms", suffixFont,
            new Rectangle(63, 0, Width - 65, Height), AuroraPalette.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = AuroraGeometry.CutPath(ClientRectangle, AuroraGeometry.CutDepth(ClientRectangle, 10, 4));
        Region = new Region(path);
    }
}

internal sealed class AuroraWindowFrame : Control
{
    public AuroraWindowFrame()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var path = AuroraGeometry.CutPath(bounds, AuroraGeometry.CutDepth(bounds, 24, 4));
        using var outer = new Pen(AuroraPalette.Border, 1f);
        using var inner = new Pen(Color.FromArgb(48, 48, 48), 1f);
        e.Graphics.DrawPath(outer, path);

        var innerBounds = Rectangle.Inflate(bounds, -3, -3);
        using var innerPath = AuroraGeometry.CutPath(innerBounds,
            Math.Max(3, AuroraGeometry.CutDepth(bounds, 24, 4) - 1));
        e.Graphics.DrawPath(inner, innerPath);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        using var path = AuroraGeometry.CutPath(bounds, AuroraGeometry.CutDepth(bounds, 24, 4));
        using var outline = (GraphicsPath)path.Clone();
        using var pen = new Pen(Color.Black, 8f);
        outline.Widen(pen);
        Region = new Region(outline);
    }

    protected override void WndProc(ref Message message)
    {
        const int WmNcHitTest = 0x0084;
        const int HtTransparent = -1;
        if (message.Msg == WmNcHitTest)
        {
            message.Result = new IntPtr(HtTransparent);
            return;
        }
        base.WndProc(ref message);
    }
}

internal static class MechaScribePainter
{
    public static void Draw(Graphics graphics, Rectangle bounds)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(AuroraPalette.Scribe);
        using var strong = new Pen(AuroraPalette.ScribeStrong);

        graphics.DrawLines(pen, new Point[]
        {
            new Point(bounds.Right - 170, bounds.Top + 58),
            new Point(bounds.Right - 108, bounds.Top + 58),
            new Point(bounds.Right - 88, bounds.Top + 78),
            new Point(bounds.Right - 28, bounds.Top + 78)
        });
        graphics.DrawLines(pen, new Point[]
        {
            new Point(bounds.Left + 18, bounds.Bottom - 126),
            new Point(bounds.Left + 64, bounds.Bottom - 126),
            new Point(bounds.Left + 78, bounds.Bottom - 112),
            new Point(bounds.Left + 142, bounds.Bottom - 112)
        });
        graphics.DrawLine(pen, bounds.Right - 22, bounds.Top + 108, bounds.Right - 22, bounds.Top + 178);
        graphics.DrawLine(strong, bounds.Right - 25, bounds.Top + 130, bounds.Right - 19, bounds.Top + 130);
        graphics.DrawLines(pen, new Point[]
        {
            new Point(bounds.Left + 16, bounds.Top + 354),
            new Point(bounds.Left + 32, bounds.Top + 354),
            new Point(bounds.Left + 44, bounds.Top + 366),
            new Point(bounds.Left + 44, bounds.Top + 422),
            new Point(bounds.Left + 56, bounds.Top + 434),
            new Point(bounds.Left + 94, bounds.Top + 434)
        });
        graphics.DrawLines(strong, new Point[]
        {
            new Point(bounds.Right - 104, bounds.Bottom - 74),
            new Point(bounds.Right - 70, bounds.Bottom - 74),
            new Point(bounds.Right - 58, bounds.Bottom - 62),
            new Point(bounds.Right - 22, bounds.Bottom - 62)
        });
        graphics.DrawLine(pen, bounds.Left + 20, bounds.Top + 246, bounds.Left + 20, bounds.Top + 302);
        graphics.DrawLine(pen, bounds.Right - 20, bounds.Top + 382, bounds.Right - 20, bounds.Top + 456);
        graphics.DrawLine(strong, bounds.Left + 17, bounds.Top + 274, bounds.Left + 23, bounds.Top + 274);
        graphics.DrawLine(strong, bounds.Right - 23, bounds.Top + 412, bounds.Right - 17, bounds.Top + 412);
        graphics.DrawRectangle(pen, bounds.Left + 18, bounds.Top + 84, 5, 5);
        graphics.DrawRectangle(pen, bounds.Right - 29, bounds.Bottom - 162, 5, 5);
        graphics.DrawRectangle(strong, bounds.Left + 17, bounds.Bottom - 204, 6, 6);
        graphics.DrawRectangle(pen, bounds.Right - 30, bounds.Top + 274, 6, 6);
    }
}
