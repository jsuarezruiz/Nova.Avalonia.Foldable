using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Nova.Avalonia.Foldable.Showcase.Animation;
using Nova.Avalonia.Foldable.Showcase.Models;

namespace Nova.Avalonia.Foldable.Showcase.Views;

/// <summary>
/// One constellation, full size. Port of <c>constellation_detail_view.dart</c>.
/// </summary>
/// <remarks>Chart and lettering appear near the end of the flight, timed by the delay passed in.</remarks>
public partial class DetailPage : UserControl
{
    public static readonly StyledProperty<Constellation?> ConstellationProperty =
        AvaloniaProperty.Register<DetailPage, Constellation?>(nameof(Constellation));

    public static readonly DirectProperty<DetailPage, Bitmap?> ChartImageProperty =
        AvaloniaProperty.RegisterDirect<DetailPage, Bitmap?>(nameof(ChartImage), o => o.ChartImage);

    public static readonly DirectProperty<DetailPage, Bitmap?> LabelImageProperty =
        AvaloniaProperty.RegisterDirect<DetailPage, Bitmap?>(nameof(LabelImage), o => o.LabelImage);

    public static readonly StyledProperty<bool> IsRedModeProperty =
        AvaloniaProperty.Register<DetailPage, bool>(nameof(IsRedMode));

    public static readonly StyledProperty<bool> IsReturnVisibleProperty =
        AvaloniaProperty.Register<DetailPage, bool>(nameof(IsReturnVisible), true);

    private static readonly Easing ChartEasing = new IntervalEasing(0.4d, 0.8d, FlutterEasings.EaseOutQuad);

    private static readonly Easing LabelEasing = new IntervalEasing(0.6d, 1d, FlutterEasings.EaseOutQuad);

    private readonly ScaleTransform _chartScale = new(0d, 0d);

    // Wide enough for the chart at full pane width on a 3x display; the source art is over 2000px.
    private const int ArtworkDecodeWidth = 1400;

    private Bitmap? _chartImage;
    private Bitmap? _labelImage;
    private bool _retired;
    private int _artworkVersion;
    private readonly AnimationController _reveal;

    public DetailPage()
    {
        InitializeComponent();

        DataContext = this;
        Chart.RenderTransform = _chartScale;

        _reveal = new AnimationController(this, OnRevealProgressChanged);
    }

    static DetailPage() =>
        ConstellationProperty.Changed.AddClassHandler<DetailPage>((x, _) => x.LoadArtwork());

    /// <summary>
    /// Occurs when the return button is pressed.
    /// </summary>
    public event EventHandler? ReturnRequested;

    /// <summary>
    /// Gets or sets the constellation being shown.
    /// </summary>
    public Constellation? Constellation
    {
        get => GetValue(ConstellationProperty);
        set => SetValue(ConstellationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the title is the red, outlined kind.
    /// </summary>
    public bool IsRedMode
    {
        get => GetValue(IsRedModeProperty);
        set => SetValue(IsRedModeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the return button is shown.
    /// </summary>
    public bool IsReturnVisible
    {
        get => GetValue(IsReturnVisibleProperty);
        set => SetValue(IsReturnVisibleProperty, value);
    }

    /// <summary>
    /// Gets the star chart.
    /// </summary>
    public Bitmap? ChartImage
    {
        get => _chartImage;
        private set => SetAndRaise(ChartImageProperty, ref _chartImage, value);
    }

    /// <summary>
    /// Gets the lettering drawn over the chart.
    /// </summary>
    public Bitmap? LabelImage
    {
        get => _labelImage;
        private set => SetAndRaise(LabelImageProperty, ref _labelImage, value);
    }

    /// <summary>
    /// Gets the card the title is drawn in, so a caller can fly one to it.
    /// </summary>
    public Control TitleCardControl => TitleCard;

    /// <summary>
    /// Starts the reveal, spread over <paramref name="duration"/>.
    /// </summary>
    public void Reveal(TimeSpan duration)
    {
        _reveal.Duration = duration;
        _reveal.SetValue(0d);

        // The clock needs the top level's frames; off screen, the ticker waits until the page is attached.
        _reveal.Forward();
    }

    /// <summary>
    /// Frees the artwork once the page has left the screen; a cross-fade may still be drawing it.
    /// </summary>
    public void Retire()
    {
        _retired = true;
        if (TopLevel.GetTopLevel(this) is null)
            ReleaseArtwork();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_retired)
            ReleaseArtwork();
    }

    private void ReleaseArtwork()
    {
        var chart = ChartImage;
        var label = LabelImage;
        ChartImage = LabelImage = null;
        chart?.Dispose();
        label?.Dispose();
    }

    // Loaded in code because binding a Uri to Image.Source isn't converted at runtime.
    private async void LoadArtwork()
    {
        ReleaseArtwork();
        var version = ++_artworkVersion;
        if (Constellation is not { } constellation)
        {
            return;
        }

        // Decoded off the UI thread so the flight starts without a hitch.
        var (chart, label) = await Task.Run(() => (Decode(constellation.ChartUri), Decode(constellation.LabelUri)));
        if (version != _artworkVersion || _retired)
        {
            chart.Dispose();
            label.Dispose();
            return;
        }

        ChartImage = chart;
        LabelImage = label;
    }

    private static Bitmap Decode(Uri uri)
    {
        using var stream = AssetLoader.Open(uri);
        return Bitmap.DecodeToWidth(stream, ArtworkDecodeWidth);
    }

    private void OnRevealProgressChanged(double progress)
    {
        _chartScale.ScaleX = _chartScale.ScaleY = ChartEasing.Ease(progress);

        Label.Opacity = LabelEasing.Ease(progress);
    }

    private void OnReturnClicked(object? sender, RoutedEventArgs e) =>
        ReturnRequested?.Invoke(this, EventArgs.Empty);
}
