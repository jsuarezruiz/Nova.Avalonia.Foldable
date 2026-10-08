using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Nova.Avalonia.Foldable.Showcase.Animation;
using Nova.Avalonia.Foldable.Showcase.Controls;
using Nova.Avalonia.Foldable.Showcase.Models;

namespace Nova.Avalonia.Foldable.Showcase.Views;

/// <summary>
/// The app shell: list and detail on either side of the fold over one star field.
/// </summary>
public partial class MainView : UserControl
{
    private const double IdleSpeed = 0.2d;

    private const double MaxSpeed = 10d;

    // How much star speed one degree of hinge travel is worth.
    private const double HingeWarp = 0.6d;

    // Keeps the entries nudged toward the fold clear of it.
    private const double FoldGutter = 32d;

    // The width the guide was designed for; narrower panes get the list scaled to fit.
    private const double DesignWidth = 375d;

    // Scroll reports stop when the list stops, so the field returns to its drift after this.
    private static readonly TimeSpan ScrollRest = TimeSpan.FromMilliseconds(120);

    private static readonly TimeSpan FlightDuration = TimeSpan.FromMilliseconds(3000);
    private static readonly TimeSpan PageFade = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PaneFade = TimeSpan.FromMilliseconds(400);

    private static readonly TimeSpan ContentDelay = FlightDuration + TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan FirstReveal = TimeSpan.FromMilliseconds(1600);

    private readonly AnimationController _starFlight;
    private readonly AnimationController _cardFlight;
    private readonly AnimationController _paneFade;
    private readonly DispatcherTimer _scrollRest = new() { Interval = ScrollRest };
    private readonly IPageTransition? _detailTransition;

    private DetailPage? _detail;
    private Control? _fadingPane;
    private ConstellationTitleCard? _flyingCard;
    private Control? _flightSource;
    private Control? _flightTarget;
    private MaterialRectArcTween? _cardRectTween;
    private int _flightId;

    public MainView()
    {
        InitializeComponent();

        _detailTransition = DetailHost.PageTransition;

        _starFlight = new AnimationController(this, OnStarFlightChanged) { Duration = FlightDuration };
        _cardFlight = new AnimationController(this, OnCardFlightChanged) { Duration = PageFade };
        _paneFade = new AnimationController(this, OnPaneFadeChanged) { Duration = PaneFade };

        _scrollRest.Tick += OnScrollRested;
        List.Scrolled += OnListScrolled;
        List.EntryTapped += OnEntryTapped;
        Panes.ModeChanged += OnModeChanged;
        ListHost.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
            {
                UpdateListScale();
            }
        };

        UpdateCaption();
    }

    private bool IsSinglePane => Panes.Mode == TwoPaneViewMode.SinglePane;

    // Pulled back, thrown forward, then settled: legs of a fifth, a third and a half of the run.
    private static double FlightSpeedAt(double progress) => progress switch
    {
        < 0.2d => Lerp(IdleSpeed, -2d, FlutterEasings.EaseOut.Ease(progress / 0.2d)),
        < 0.5d => Lerp(-2d, 20d, FlutterEasings.EaseOut.Ease((progress - 0.2d) / 0.3d)),
        _ => Lerp(20d, 0d, FlutterEasings.EaseOut.Ease((progress - 0.5d) / 0.5d)),
    };

    private static double Lerp(double from, double to, double t) => from + ((to - from) * t);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Both inherited from the top level, where Nova.Avalonia.Foldable publishes the fold.
        if (change.Property == Fold.HingeAngleProperty
            && change.OldValue is double oldAngle
            && change.NewValue is double newAngle)
        {
            DriveStars((newAngle - oldAngle) * HingeWarp);
        }

        if (change.Property == Fold.StateProperty)
        {
            UpdateCaption();
        }
    }

    private void OnListScrolled(object? sender, double delta) => DriveStars(delta);

    private void DriveStars(double delta)
    {
        if (_starFlight.IsAnimating)
        {
            return;
        }

        // A still source drifts; a moving one drives the field at its own velocity.
        Stars.Speed = delta == 0d ? IdleSpeed : Math.Clamp(delta, -MaxSpeed, MaxSpeed);

        // Restarted on every report, so it only fires once the source has actually stopped.
        _scrollRest.Stop();
        _scrollRest.Start();
    }

    private void OnScrollRested(object? sender, EventArgs e)
    {
        _scrollRest.Stop();

        if (!_starFlight.IsAnimating)
        {
            Stars.Speed = IdleSpeed;
        }
    }

    private void OnModeChanged(object? sender, EventArgs e)
    {
        // Once two panes are on screen the detail is visible, so give it something to show.
        if (!IsSinglePane && _detail is null)
        {
            ShowDetail(DemoData.Constellations[0], isRedMode: false).Reveal(FirstReveal);
        }

        if (_detail is { } detail)
        {
            detail.IsReturnVisible = IsSinglePane;
        }

        List.Padding = Panes.Mode == TwoPaneViewMode.Wide ? new Thickness(0, 0, FoldGutter, 0) : default;
        UpdateListScale();

        UpdateCaption();
    }

    private void OnEntryTapped(object? sender, ConstellationTitleCard card)
    {
        if (card.Constellation is not { } constellation)
        {
            return;
        }

        // Read before the list leaves the visual tree on one screen.
        var origin = RectOf(card);

        // Also remembered with two panes, so closing the device keeps the constellation.
        ShowPane(TwoPaneViewPriority.Pane2);

        var detail = ShowDetail(constellation, card.IsRedMode);
        detail.Reveal(ContentDelay);

        _starFlight.SetValue(0d);
        _starFlight.Forward();

        BeginFlight(origin, card, detail.TitleCardControl, constellation, card.IsRedMode);
    }

    private void ReturnToList(DetailPage detail)
    {
        var origin = RectOf(detail.TitleCardControl);

        ShowPane(TwoPaneViewPriority.Pane1);

        // Reverse the flight if it is still running; otherwise just settle back to the drift.
        if (_starFlight.IsAnimating)
        {
            _starFlight.Reverse();
        }
        else
        {
            Stars.Speed = IdleSpeed;
        }

        if (detail.Constellation is { } constellation && _flightSource is { } source)
        {
            BeginFlight(origin, detail.TitleCardControl, source, constellation, detail.IsRedMode);
        }
    }

    private DetailPage ShowDetail(Constellation constellation, bool isRedMode)
    {
        var detail = new DetailPage
        {
            Constellation = constellation,
            IsRedMode = isRedMode,
            IsReturnVisible = IsSinglePane,
        };

        detail.ReturnRequested += (_, _) => ReturnToList(detail);

        // A cross-fade started while the pane is out of the tree never ticks; ShowPane fades it instead.
        DetailHost.PageTransition = TopLevel.GetTopLevel(DetailHost) is null ? null : _detailTransition;

        _detail?.Retire();
        _detail = detail;
        DetailHost.Content = detail;
        return detail;
    }

    private void UpdateListScale()
    {
        var available = ListHost.Bounds.Width - List.Padding.Left - List.Padding.Right;
        var scale = available > 0d ? Math.Min(1d, available / DesignWidth) : 1d;

        if (ListHost.LayoutTransform is ScaleTransform current && current.ScaleX == scale)
        {
            return;
        }

        ListHost.LayoutTransform = scale < 1d ? new ScaleTransform(scale, scale) : null;
    }

    // Picks the pane shown on one screen, fading it in. With two panes both are already visible.
    private void ShowPane(TwoPaneViewPriority priority)
    {
        if (Panes.PanePriority == priority)
        {
            return;
        }

        Panes.PanePriority = priority;

        if (IsSinglePane)
        {
            // One pane fades at a time; a fade this one interrupts ends at full opacity.
            if (_fadingPane is { } previous)
                previous.Opacity = 1d;

            _fadingPane = priority == TwoPaneViewPriority.Pane1 ? List : DetailHost;
            _paneFade.SetValue(0d);
            _paneFade.Forward();
        }
    }

    private void OnPaneFadeChanged(double progress)
    {
        if (_fadingPane is { } pane)
        {
            pane.Opacity = FlutterEasings.EaseOut.Ease(progress);
        }
    }

    private void UpdateCaption()
    {
        var state = Fold.GetState(this);

        var posture = state.IsFoldable
            ? state switch
            {
                { IsBook: true } => "BOOK",
                { IsTabletop: true } => "TABLETOP",
                { Posture: DevicePosture.Closed } => "CLOSED",
                { Posture: DevicePosture.Flat } => "OPEN",
                _ => "FOLDABLE",
            }
            : "SINGLE SCREEN";

        var angle = state.HingeAngle is { } degrees ? $" · {degrees:0}°" : string.Empty;
        var panes = IsSinglePane ? "ONE PANE" : "TWO PANES";

        PostureCaption.Text = $"{posture}{angle} · {panes}";
    }

    // Hides both ends now and starts the flight after layout: the target page may be brand new.
    private void BeginFlight(
        Rect origin,
        Control from,
        Control to,
        Constellation constellation,
        bool isRedMode)
    {
        if (origin.Width <= 0d)
        {
            return;
        }

        // Land any flight still in the air, which shows its cards again.
        _cardFlight.Stop();
        EndCardFlight();

        from.Opacity = 0d;
        to.Opacity = 0d;

        // Only the latest tap flies; an earlier one still queued is dropped.
        var id = ++_flightId;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (id == _flightId)
                {
                    StartCardFlight(origin, from, to, constellation, isRedMode);
                }
            },
            DispatcherPriority.Loaded);
    }

    private Rect RectOf(Control control) =>
        new(
            control.TranslatePoint(default, this) ?? default,
            control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), this) ?? default);

    // Flies a copy of the card between the pages' slots; with two panes it crosses the fold.
    private void StartCardFlight(
        Rect origin,
        Control from,
        Control to,
        Constellation constellation,
        bool isRedMode)
    {
        // Without a laid-out target, use where the detail page docks its card.
        var host = IsSinglePane ? new Rect(Bounds.Size) : RectOf(DetailHost);
        var destination = to.Bounds.Width > 0d
            ? RectOf(to)
            : new Rect(host.X + ((host.Width - origin.Width) / 2d), host.Y + 24d, origin.Width, origin.Height);
        _cardRectTween = new MaterialRectArcTween(origin, destination);

        _flyingCard ??= new ConstellationTitleCard();
        _flyingCard.Constellation = constellation;
        _flyingCard.IsRedMode = isRedMode;

        if (!FlightLayer.Children.Contains(_flyingCard))
        {
            FlightLayer.Children.Add(_flyingCard);
        }

        _flightSource = from;
        _flightTarget = to;

        _cardFlight.SetValue(0d);
        _cardFlight.Forward();
    }

    private void OnCardFlightChanged(double progress)
    {
        if (_flyingCard is not { } card || _cardRectTween is not { } tween)
        {
            return;
        }

        // Flutter's Hero default: opposite corners travel on arcs.
        var bounds = tween.Lerp(progress);

        Canvas.SetLeft(card, bounds.X);
        Canvas.SetTop(card, bounds.Y);
        card.Width = bounds.Width;
        card.Height = bounds.Height;

        if (progress < 1d)
        {
            return;
        }

        EndCardFlight();
    }

    // Lands the flight: the copy leaves the overlay and both real cards show again.
    private void EndCardFlight()
    {
        if (_flyingCard is { } card)
        {
            FlightLayer.Children.Remove(card);
        }

        if (_flightTarget is { } target)
        {
            target.Opacity = 1d;
        }

        if (_flightSource is { } source)
        {
            source.Opacity = 1d;
        }

        // The target can be a detail page's card; don't hold on to the page after it's replaced.
        _flightTarget = null;
    }

    private void OnStarFlightChanged(double progress) => Stars.Speed = FlightSpeedAt(progress);
}
