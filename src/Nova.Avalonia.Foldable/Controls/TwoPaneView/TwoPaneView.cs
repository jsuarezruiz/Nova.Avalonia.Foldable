using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// Two panes side by side, stacked, or one at a time, split at the fold, following the WinUI and .NET MAUI API.
/// </summary>
public class TwoPaneView : Control
{
    /// <summary>
    /// Defines the <see cref="Pane1"/> property.
    /// </summary>
    public static readonly StyledProperty<Control?> Pane1Property =
        AvaloniaProperty.Register<TwoPaneView, Control?>(nameof(Pane1));

    /// <summary>
    /// Defines the <see cref="Pane2"/> property.
    /// </summary>
    public static readonly StyledProperty<Control?> Pane2Property =
        AvaloniaProperty.Register<TwoPaneView, Control?>(nameof(Pane2));

    /// <summary>
    /// Defines the <see cref="Pane1Length"/> property.
    /// </summary>
    public static readonly StyledProperty<GridLength> Pane1LengthProperty =
        AvaloniaProperty.Register<TwoPaneView, GridLength>(nameof(Pane1Length), GridLength.Star);

    /// <summary>
    /// Defines the <see cref="Pane2Length"/> property.
    /// </summary>
    public static readonly StyledProperty<GridLength> Pane2LengthProperty =
        AvaloniaProperty.Register<TwoPaneView, GridLength>(nameof(Pane2Length), GridLength.Star);

    /// <summary>
    /// Defines the <see cref="PanePriority"/> property.
    /// </summary>
    public static readonly StyledProperty<TwoPaneViewPriority> PanePriorityProperty =
        AvaloniaProperty.Register<TwoPaneView, TwoPaneViewPriority>(nameof(PanePriority));

    /// <summary>
    /// Defines the <see cref="MinWideModeWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MinWideModeWidthProperty =
        AvaloniaProperty.Register<TwoPaneView, double>(nameof(MinWideModeWidth), 641);

    /// <summary>
    /// Defines the <see cref="MinTallModeHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MinTallModeHeightProperty =
        AvaloniaProperty.Register<TwoPaneView, double>(nameof(MinTallModeHeight), 641);

    /// <summary>
    /// Defines the <see cref="WideModeConfiguration"/> property.
    /// </summary>
    public static readonly StyledProperty<TwoPaneViewWideModeConfiguration> WideModeConfigurationProperty =
        AvaloniaProperty.Register<TwoPaneView, TwoPaneViewWideModeConfiguration>(
            nameof(WideModeConfiguration), TwoPaneViewWideModeConfiguration.LeftRight);

    /// <summary>
    /// Defines the <see cref="TallModeConfiguration"/> property.
    /// </summary>
    public static readonly StyledProperty<TwoPaneViewTallModeConfiguration> TallModeConfigurationProperty =
        AvaloniaProperty.Register<TwoPaneView, TwoPaneViewTallModeConfiguration>(
            nameof(TallModeConfiguration), TwoPaneViewTallModeConfiguration.TopBottom);

    /// <summary>
    /// Defines the <see cref="SnapToFold"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> SnapToFoldProperty =
        AvaloniaProperty.Register<TwoPaneView, bool>(nameof(SnapToFold), true);

    /// <summary>
    /// Defines the <see cref="Mode"/> property.
    /// </summary>
    public static readonly DirectProperty<TwoPaneView, TwoPaneViewMode> ModeProperty =
        AvaloniaProperty.RegisterDirect<TwoPaneView, TwoPaneViewMode>(nameof(Mode), o => o.Mode);

    private readonly List<Visual> _transformSources = [];
    private readonly PaneHost _host1 = new();
    private readonly PaneHost _host2 = new();
    private TwoPaneViewMode _mode;
    private PaneLayout? _arranged;
    private Size _arrangedSize;
    private Matrix _fromTopLevel = Matrix.Identity;
    private Matrix? _settlingFromTopLevel;
    private bool _isSettling;
    private TopLevel? _topLevel;

    static TwoPaneView()
    {
        AffectsMeasure<TwoPaneView>(
            Pane1LengthProperty, Pane2LengthProperty, PanePriorityProperty,
            MinWideModeWidthProperty, MinTallModeHeightProperty,
            WideModeConfigurationProperty, TallModeConfigurationProperty,
            SnapToFoldProperty);
    }

    /// <summary>
    /// Creates a two-pane view.
    /// </summary>
    public TwoPaneView()
    {
        VisualChildren.Add(_host1);
        VisualChildren.Add(_host2);
    }

    /// <summary>
    /// Gets or sets the first pane: left, top, or the only one by default.
    /// </summary>
    public Control? Pane1
    {
        get => GetValue(Pane1Property);
        set => SetValue(Pane1Property, value);
    }

    /// <summary>
    /// Gets or sets the second pane: right or bottom.
    /// </summary>
    public Control? Pane2
    {
        get => GetValue(Pane2Property);
        set => SetValue(Pane2Property, value);
    }

    /// <summary>
    /// Gets or sets Pane1's share of the space when no fold decides the split.
    /// </summary>
    public GridLength Pane1Length
    {
        get => GetValue(Pane1LengthProperty);
        set => SetValue(Pane1LengthProperty, value);
    }

    /// <summary>
    /// Gets or sets Pane2's share of the space when no fold decides the split.
    /// </summary>
    public GridLength Pane2Length
    {
        get => GetValue(Pane2LengthProperty);
        set => SetValue(Pane2LengthProperty, value);
    }

    /// <summary>
    /// Gets or sets the pane shown in SinglePane mode.
    /// </summary>
    public TwoPaneViewPriority PanePriority
    {
        get => GetValue(PanePriorityProperty);
        set => SetValue(PanePriorityProperty, value);
    }

    /// <summary>
    /// Gets or sets the width above which the panes go side by side.
    /// </summary>
    public double MinWideModeWidth
    {
        get => GetValue(MinWideModeWidthProperty);
        set => SetValue(MinWideModeWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the height above which the panes stack.
    /// </summary>
    public double MinTallModeHeight
    {
        get => GetValue(MinTallModeHeightProperty);
        set => SetValue(MinTallModeHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the pane order, or single pane, when the view is wide.
    /// </summary>
    public TwoPaneViewWideModeConfiguration WideModeConfiguration
    {
        get => GetValue(WideModeConfigurationProperty);
        set => SetValue(WideModeConfigurationProperty, value);
    }

    /// <summary>
    /// Gets or sets the pane order, or single pane, when the view is tall.
    /// </summary>
    public TwoPaneViewTallModeConfiguration TallModeConfiguration
    {
        get => GetValue(TallModeConfigurationProperty);
        set => SetValue(TallModeConfigurationProperty, value);
    }

    /// <summary>
    /// Gets or sets whether a flat device splits at the fold too; a half-open device always does.
    /// </summary>
    public bool SnapToFold
    {
        get => GetValue(SnapToFoldProperty);
        set => SetValue(SnapToFoldProperty, value);
    }

    /// <summary>
    /// Gets how the panes are currently shown.
    /// </summary>
    /// <remarks>Updated after the panes have been arranged, normally when the layout pass ends.</remarks>
    public TwoPaneViewMode Mode
    {
        get => _mode;
        private set
        {
            if (SetAndRaise(ModeProperty, ref _mode, value))
                ModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Occurs when <see cref="Mode"/> changes, once the panes are arranged in the new mode.
    /// </summary>
    public event EventHandler? ModeChanged;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == Pane1Property || change.Property == Pane2Property)
        {
            // Swapping the panes passes each control through both properties; keep one still in use.
            if (change.OldValue is Control oldPane && oldPane != Pane1 && oldPane != Pane2)
                LogicalChildren.Remove(oldPane);

            if (change.NewValue is Control newPane && !LogicalChildren.Contains(newPane))
                LogicalChildren.Add(newPane);

            // A pane moving to the other host, as when the panes are swapped, leaves its old one first.
            // The same control in both properties is shown once.
            if (_host1.Pane != Pane1 && _host2.Pane == Pane1)
                _host2.Pane = null;
            if (_host2.Pane != Pane2 && _host1.Pane == Pane2)
                _host1.Pane = null;
            _host1.Pane = Pane1;
            _host2.Pane = Pane2 == Pane1 ? null : Pane2;

            InvalidateMeasure();
        }
        else if (change.Property == Fold.StateProperty
                 && change.OldValue is FoldState oldState && change.NewValue is FoldState newState
                 && AffectsLayout(oldState, newState))
        {
            InvalidateMeasure();
        }
    }

    // The hinge angle changes many times a second while folding and never moves the panes.
    private static bool AffectsLayout(FoldState a, FoldState b) =>
        a.Posture != b.Posture || a.FoldBounds != b.FoldBounds || a.IsSeparating != b.IsSeparating;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            Fold.GetProvider(_topLevel);
        LayoutUpdated += OnLayoutUpdated;

        // A render transform moves the view without a layout pass; watch this one and every one above it.
        for (Visual? visual = this; visual is not null && visual != _topLevel; visual = visual.GetVisualParent())
        {
            visual.PropertyChanged += OnTransformSourcePropertyChanged;
            if (visual.RenderTransform is IMutableTransform transform)
                transform.Changed += OnRenderTransformChanged;
            _transformSources.Add(visual);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;
        foreach (var visual in _transformSources)
        {
            visual.PropertyChanged -= OnTransformSourcePropertyChanged;
            if (visual.RenderTransform is IMutableTransform transform)
                transform.Changed -= OnRenderTransformChanged;
        }
        _transformSources.Clear();
        _settlingFromTopLevel = null;
        _isSettling = false;
        _topLevel = null;
        _fromTopLevel = Matrix.Identity;
        _arranged = null;
    }

    private void OnTransformSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RenderTransformProperty)
        {
            if (e.OldValue is IMutableTransform oldTransform)
                oldTransform.Changed -= OnRenderTransformChanged;
            if (e.NewValue is IMutableTransform newTransform)
                newTransform.Changed += OnRenderTransformChanged;
            WaitForTransformToSettle();
        }
        else if (e.Property == RenderTransformOriginProperty)
        {
            WaitForTransformToSettle();
        }
    }

    private void OnRenderTransformChanged(object? sender, EventArgs e) => WaitForTransformToSettle();

    // An animated transform changes every frame, and mapping the fold each time would lay the panes out
    // again on every one of them. The fold is mapped once the transform has held still for a frame;
    // a layout pass in the meantime still picks up where the view is (see OnLayoutUpdated).
    // Without a fold there is nothing to map, and nothing to wait for.
    private void WaitForTransformToSettle()
    {
        if (_isSettling || _topLevel is null || GetValue(Fold.StateProperty).FoldBounds is null)
            return;
        _isSettling = true;
        _topLevel.RequestAnimationFrame(OnTransformFrame);
    }

    private void OnTransformFrame(TimeSpan time)
    {
        _isSettling = false;
        if (_topLevel?.TransformToVisual(this) is not { } transform)
            return;

        if (transform == _settlingFromTopLevel)
        {
            _settlingFromTopLevel = null;
            UpdateFoldMapping();
        }
        else
        {
            _settlingFromTopLevel = transform;
            WaitForTransformToSettle();
        }
    }

    // Mode is published once the fold mapping has settled and the panes are arranged.
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!UpdateFoldMapping() && IsMeasureValid && IsArrangeValid && _arranged is { } arranged)
            Mode = arranged.Mode;
    }

    // The fold is reported in top-level coordinates; track where this view sits (and any scaling)
    // so it can be mapped into local coordinates on the next layout pass. Returns whether one was requested.
    private bool UpdateFoldMapping()
    {
        if (_topLevel?.TransformToVisual(this) is not { } transform || transform == _fromTopLevel)
            return false;
        _fromTopLevel = transform;

        // Without a fold the position doesn't matter; skip the extra pass while scrolling or animating.
        if (GetValue(Fold.StateProperty).FoldBounds is null)
            return false;

        // Nor does a move that leaves the panes where they are, such as scrolling along the fold or past it.
        if (_arranged is { } arranged && ComputeLayout(_arrangedSize) == arranged)
            return false;

        InvalidateMeasure();
        return true;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Unconstrained, use the last arranged size, as WinUI does, so measure and arrange agree on the mode.
        var layoutSize = new Size(
            double.IsInfinity(availableSize.Width) ? Bounds.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? Bounds.Height : availableSize.Height);
        var layout = ComputeLayout(layoutSize);
        ShowHosts(layout);

        var desired = default(Size);
        if (layout.Pane1 is { } r1 && _host1.Pane is not null)
        {
            _host1.Measure(MeasureConstraint(availableSize, r1, layout.Mode));
            desired = Combine(desired, _host1.DesiredSize, layout.Mode);
        }
        if (layout.Pane2 is { } r2 && _host2.Pane is not null)
        {
            _host2.Measure(MeasureConstraint(availableSize, r2, layout.Mode));
            desired = Combine(desired, _host2.DesiredSize, layout.Mode);
        }

        return new Size(
            double.IsInfinity(availableSize.Width) ? desired.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? desired.Height : availableSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = ComputeLayout(finalSize);
        ShowHosts(layout);

        // The hosts cover the whole view, so the panes' bounds are in the view's coordinates.
        var bounds = new Rect(finalSize);
        if (layout.Pane1 is { } r1)
        {
            _host1.PaneRect = r1;
            _host1.Arrange(bounds);
        }
        if (layout.Pane2 is { } r2)
        {
            _host2.PaneRect = r2;
            _host2.Arrange(bounds);
        }

        // Mode follows the arranged layout only, published when the layout pass ends (see OnLayoutUpdated).
        // Outside a top level there is no fold to map, so it is published right away.
        _arranged = layout;
        _arrangedSize = finalSize;
        if (_topLevel is null)
            Mode = layout.Mode;

        return finalSize;
    }

    // An unconstrained direction stays unconstrained, so the panes report their natural size there.
    private static Size MeasureConstraint(Size available, Rect paneRect, TwoPaneViewMode mode) => mode switch
    {
        TwoPaneViewMode.Wide => new Size(double.IsInfinity(available.Width) ? available.Width : paneRect.Width, available.Height),
        TwoPaneViewMode.Tall => new Size(available.Width, double.IsInfinity(available.Height) ? available.Height : paneRect.Height),
        _ => available,
    };

    // Side by side the widths add up, stacked the heights do.
    private static Size Combine(Size a, Size b, TwoPaneViewMode mode) => mode switch
    {
        TwoPaneViewMode.Wide => new Size(a.Width + b.Width, Math.Max(a.Height, b.Height)),
        TwoPaneViewMode.Tall => new Size(Math.Max(a.Width, b.Width), a.Height + b.Height),
        _ => new Size(Math.Max(a.Width, b.Width), Math.Max(a.Height, b.Height)),
    };

    private void ShowHosts(PaneLayout layout)
    {
        _host1.IsVisible = layout.Pane1 is not null;
        _host2.IsVisible = layout.Pane2 is not null;
    }

    private readonly record struct PaneLayout(TwoPaneViewMode Mode, Rect? Pane1, Rect? Pane2);

    // Holds a pane in the visual tree for good: a pane that isn't shown is hidden with its host, as WinUI
    // collapses it, instead of being detached. Detaching and attaching again restarts the pane's controls,
    // and a TransitioningContentControl attached again can show a page it had faded out.
    private sealed class PaneHost : Control
    {
        private Control? _pane;
        private Rect _paneRect;

        public Control? Pane
        {
            get => _pane;
            set
            {
                if (_pane == value)
                    return;
                if (_pane is not null)
                    VisualChildren.Remove(_pane);
                _pane = value;
                if (_pane is not null)
                    VisualChildren.Add(_pane);
                InvalidateMeasure();
            }
        }

        // Where the pane goes in the view.
        public Rect PaneRect
        {
            get => _paneRect;
            set
            {
                if (_paneRect == value)
                    return;
                _paneRect = value;
                InvalidateArrange();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _pane?.Measure(availableSize);
            return _pane?.DesiredSize ?? default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _pane?.Arrange(_paneRect);
            return finalSize;
        }
    }

    private PaneLayout ComputeLayout(Size size)
    {
        var bounds = new Rect(size);
        var state = GetValue(Fold.StateProperty);

        // A separating fold decides the layout on its own, as spanning two screens does in WinUI: with
        // SinglePane configured for its direction, one pane is shown rather than the size rules laying
        // both panes across the fold.
        if (LocalFold(state, size) is { } fold)
        {
            if (state.Orientation == FoldOrientation.Vertical
                && (state.IsSeparating || (SnapToFold && size.Width > MinWideModeWidth)))
            {
                if (WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
                {
                    return Wide(
                        new Rect(0, 0, fold.X, size.Height),
                        new Rect(fold.Right, 0, Math.Max(0, size.Width - fold.Right), size.Height));
                }

                if (state.IsSeparating)
                    return SinglePane(bounds);
            }

            if (state.Orientation == FoldOrientation.Horizontal
                && (state.IsSeparating || (SnapToFold && size.Height > MinTallModeHeight)))
            {
                if (TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
                {
                    return Tall(
                        new Rect(0, 0, size.Width, fold.Y),
                        new Rect(0, fold.Bottom, size.Width, Math.Max(0, size.Height - fold.Bottom)));
                }

                if (state.IsSeparating)
                    return SinglePane(bounds);
            }
        }

        if (size.Width > MinWideModeWidth && WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
        {
            var (left, right) = WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight
                ? Split(size.Width, Pane1Length, Pane2Length)
                : Split(size.Width, Pane2Length, Pane1Length);
            return Wide(new Rect(0, 0, left, size.Height), new Rect(left, 0, right, size.Height));
        }

        if (size.Height > MinTallModeHeight && TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
        {
            var (top, bottom) = TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom
                ? Split(size.Height, Pane1Length, Pane2Length)
                : Split(size.Height, Pane2Length, Pane1Length);
            return Tall(new Rect(0, 0, size.Width, top), new Rect(0, top, size.Width, bottom));
        }

        return SinglePane(bounds);
    }

    private PaneLayout SinglePane(Rect bounds) =>
        PanePriority == TwoPaneViewPriority.Pane1
            ? new(TwoPaneViewMode.SinglePane, bounds, null)
            : new(TwoPaneViewMode.SinglePane, null, bounds);

    private PaneLayout Wide(Rect left, Rect right) =>
        WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight
            ? new(TwoPaneViewMode.Wide, left, right)
            : new(TwoPaneViewMode.Wide, right, left);

    private PaneLayout Tall(Rect top, Rect bottom) =>
        TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom
            ? new(TwoPaneViewMode.Tall, top, bottom)
            : new(TwoPaneViewMode.Tall, bottom, top);

    // The fold in local coordinates, when it crosses this view. Closed devices show their cover display.
    private Rect? LocalFold(FoldState state, Size size)
    {
        if (state.FoldBounds is not { } fold || state.Posture == DevicePosture.Closed)
            return null;

        var local = fold.TransformToAABB(_fromTopLevel);
        return state.Orientation switch
        {
            FoldOrientation.Vertical when local.X > 0 && local.Right < size.Width
                                        && local.Y < size.Height && local.Bottom > 0 => local,
            FoldOrientation.Horizontal when local.Y > 0 && local.Bottom < size.Height
                                          && local.X < size.Width && local.Right > 0 => local,
            _ => null,
        };
    }

    // The lengths of the pane on the left or top and the one after it, as a WinUI grid sizes its columns:
    // pixel lengths are honored, the first pane's first when they don't fit, and star lengths share the
    // rest; Auto counts as 1*. Two pixel lengths can leave space empty.
    private static (double First, double Second) Split(double total, GridLength first, GridLength second)
    {
        var firstPixels = first.IsAbsolute ? Math.Min(first.Value, total) : 0;
        var secondPixels = second.IsAbsolute ? Math.Min(second.Value, total - firstPixels) : 0;
        if (first.IsAbsolute && second.IsAbsolute)
            return (firstPixels, secondPixels);

        var rest = total - firstPixels - secondPixels;
        var firstStars = first.IsAbsolute ? 0 : first.IsStar ? first.Value : 1;
        var secondStars = second.IsAbsolute ? 0 : second.IsStar ? second.Value : 1;
        var stars = firstStars + secondStars;

        // With no star weight to go by, the panes that aren't a pixel length share the rest equally.
        double Share(GridLength length, double weight) =>
            length.IsAbsolute ? 0
            : stars > 0 ? rest * weight / stars
            : rest / ((first.IsAbsolute ? 0 : 1) + (second.IsAbsolute ? 0 : 1));

        return (firstPixels + Share(first, firstStars), secondPixels + Share(second, secondStars));
    }
}
