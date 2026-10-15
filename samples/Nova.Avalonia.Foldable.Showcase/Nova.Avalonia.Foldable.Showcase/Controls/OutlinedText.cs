using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Nova.Avalonia.Foldable.Showcase.Controls;

/// <summary>
/// Draws a line of text either filled or as an outline.
/// </summary>
/// <remarks>Avalonia's <see cref="TextBlock"/> can't stroke, so the text is drawn as geometry.</remarks>
public sealed class OutlinedText : Control
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<OutlinedText, string?>(nameof(Text));

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<OutlinedText>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<OutlinedText>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<OutlinedText>();

    public static readonly StyledProperty<bool> IsOutlinedProperty =
        AvaloniaProperty.Register<OutlinedText, bool>(nameof(IsOutlined));

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<OutlinedText, double>(nameof(LineHeight), double.NaN);

    private const double StrokeWidth = 1d;

    private FormattedText? _formatted;
    private Geometry? _geometry;
    private ImmutablePen? _stroke;
    private double _geometryTop = double.NaN;

    // Text that changes without changing size still needs drawing again.
    static OutlinedText()
    {
        AffectsRender<OutlinedText>(
            TextProperty, FontFamilyProperty, FontSizeProperty, ForegroundProperty, IsOutlinedProperty, LineHeightProperty);
        AffectsMeasure<OutlinedText>(TextProperty, FontFamilyProperty, FontSizeProperty, LineHeightProperty);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the text is stroked rather than filled.
    /// </summary>
    public bool IsOutlined
    {
        get => GetValue(IsOutlinedProperty);
        set => SetValue(IsOutlinedProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the line box; the font's own when not set.
    /// </summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Format() is not { } formatted || Foreground is not { } brush)
        {
            return;
        }

        // The line box may be taller than the glyphs, so the text is centered in it.
        var top = double.IsNaN(LineHeight) ? 0d : (LineHeight - formatted.Height) / 2d;

        // The geometry is cached until the text or its position changes.
        if (_geometry is null || _geometryTop != top)
        {
            _geometry = formatted.BuildGeometry(new Point(0d, top));
            _geometryTop = top;
        }

        if (_geometry is null)
        {
            return;
        }

        if (IsOutlined)
        {
            _stroke ??= new ImmutablePen(brush.ToImmutable(), StrokeWidth);

            context.DrawGeometry(null, _stroke, _geometry);
        }
        else
        {
            context.DrawGeometry(brush, null, _geometry);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Reshaped glyphs are laid out again; a new brush only needs a new pen.
        if (change.Property == TextProperty || change.Property == FontFamilyProperty || change.Property == FontSizeProperty)
        {
            _formatted = null;
            _geometry = null;
        }
        else if (change.Property == LineHeightProperty)
        {
            _geometry = null;
        }
        else if (change.Property == ForegroundProperty)
        {
            _stroke = null;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Format() is not { } formatted)
        {
            return default;
        }

        return new Size(formatted.WidthIncludingTrailingWhitespace, double.IsNaN(LineHeight) ? formatted.Height : LineHeight);
    }

    private FormattedText? Format()
    {
        if (Text is not { Length: > 0 } text || FontSize <= 0d)
        {
            return null;
        }

        return _formatted ??= new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily),
            FontSize,
            Foreground);
    }
}
