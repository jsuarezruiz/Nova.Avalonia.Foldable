using System;
using Avalonia;
using Avalonia.Controls;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// Two panes side by side, stacked, or one at a time, split at the fold when there is one.
/// Follows the WinUI and .NET MAUI TwoPaneView API.
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

    private TwoPaneViewMode _mode;
    private Matrix _fromTopLevel = Matrix.Identity;
    private TopLevel? _topLevel;

    static TwoPaneView()
    {
        AffectsMeasure<TwoPaneView>(
            Pane1LengthProperty, Pane2LengthProperty, PanePriorityProperty,
            MinWideModeWidthProperty, MinTallModeHeightProperty,
            WideModeConfigurationProperty, TallModeConfigurationProperty,
            SnapToFoldProperty, Fold.StateProperty);
    }

    /// <summary>
    /// The first pane: left, top, or the only one by default.
    /// </summary>
    public Control? Pane1
    {
        get => GetValue(Pane1Property);
        set => SetValue(Pane1Property, value);
    }

    /// <summary>
    /// The second pane: right or bottom.
    /// </summary>
    public Control? Pane2
    {
        get => GetValue(Pane2Property);
        set => SetValue(Pane2Property, value);
    }

    /// <summary>
    /// Pane1's share of the space when no fold decides the split.
    /// </summary>
    public GridLength Pane1Length
    {
        get => GetValue(Pane1LengthProperty);
        set => SetValue(Pane1LengthProperty, value);
    }

    /// <summary>
    /// Pane2's share of the space when no fold decides the split.
    /// </summary>
    public GridLength Pane2Length
    {
        get => GetValue(Pane2LengthProperty);
        set => SetValue(Pane2LengthProperty, value);
    }

    /// <summary>
    /// The pane shown in SinglePane mode.
    /// </summary>
    public TwoPaneViewPriority PanePriority
    {
        get => GetValue(PanePriorityProperty);
        set => SetValue(PanePriorityProperty, value);
    }

    /// <summary>
    /// Width above which the panes go side by side.
    /// </summary>
    public double MinWideModeWidth
    {
        get => GetValue(MinWideModeWidthProperty);
        set => SetValue(MinWideModeWidthProperty, value);
    }

    /// <summary>
    /// Height above which the panes stack.
    /// </summary>
    public double MinTallModeHeight
    {
        get => GetValue(MinTallModeHeightProperty);
        set => SetValue(MinTallModeHeightProperty, value);
    }

    /// <summary>
    /// Pane order, or single pane, when the view is wide.
    /// </summary>
    public TwoPaneViewWideModeConfiguration WideModeConfiguration
    {
        get => GetValue(WideModeConfigurationProperty);
        set => SetValue(WideModeConfigurationProperty, value);
    }

    /// <summary>
    /// Pane order, or single pane, when the view is tall.
    /// </summary>
    public TwoPaneViewTallModeConfiguration TallModeConfiguration
    {
        get => GetValue(TallModeConfigurationProperty);
        set => SetValue(TallModeConfigurationProperty, value);
    }

    /// <summary>
    /// Split at the fold on a flat device too. A half-opened device always splits there.
    /// </summary>
    public bool SnapToFold
    {
        get => GetValue(SnapToFoldProperty);
        set => SetValue(SnapToFoldProperty, value);
    }

    /// <summary>
    /// How the panes are currently shown.
    /// </summary>
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
    /// Raised when <see cref="Mode"/> changes.
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
            {
                LogicalChildren.Remove(oldPane);
                VisualChildren.Remove(oldPane);
            }

            if (change.NewValue is Control newPane && !LogicalChildren.Contains(newPane))
                LogicalChildren.Add(newPane);

            InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            Fold.GetProvider(_topLevel);
        LayoutUpdated += OnLayoutUpdated;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;
        _topLevel = null;
    }

    // The fold is reported in top-level coordinates; track where this view sits (and any scaling)
    // so it can be mapped into local coordinates on the next layout pass.
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_topLevel?.TransformToVisual(this) is not { } transform || transform == _fromTopLevel)
            return;
        _fromTopLevel = transform;
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Unconstrained, use the last arranged size, as WinUI does, so measure and arrange agree on the mode.
        var layoutSize = new Size(
            double.IsInfinity(availableSize.Width) ? Bounds.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? Bounds.Height : availableSize.Height);
        var layout = ComputeLayout(layoutSize);
        Mode = layout.Mode;
        UpdateVisualChildren(layout);

        var desired = default(Size);
        if (layout.Pane1 is { } r1 && Pane1 is { } p1)
        {
            p1.Measure(MeasureConstraint(availableSize, r1, layout.Mode));
            desired = Combine(desired, p1.DesiredSize, layout.Mode);
        }
        if (layout.Pane2 is { } r2 && Pane2 is { } p2)
        {
            p2.Measure(MeasureConstraint(availableSize, r2, layout.Mode));
            desired = Combine(desired, p2.DesiredSize, layout.Mode);
        }

        return new Size(
            double.IsInfinity(availableSize.Width) ? desired.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? desired.Height : availableSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = ComputeLayout(finalSize);
        Mode = layout.Mode;
        UpdateVisualChildren(layout);

        if (layout.Pane1 is { } r1)
            Pane1?.Arrange(r1);
        if (layout.Pane2 is { } r2)
            Pane2?.Arrange(r2);

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

    private void UpdateVisualChildren(PaneLayout layout)
    {
        Sync(Pane1, layout.Pane1 is not null);
        Sync(Pane2, layout.Pane2 is not null);

        void Sync(Control? pane, bool visible)
        {
            if (pane is null)
                return;
            var present = VisualChildren.Contains(pane);
            if (visible && !present)
                VisualChildren.Add(pane);
            else if (!visible && present)
                VisualChildren.Remove(pane);
        }
    }

    private readonly record struct PaneLayout(TwoPaneViewMode Mode, Rect? Pane1, Rect? Pane2);

    private PaneLayout ComputeLayout(Size size)
    {
        var bounds = new Rect(size);
        var state = GetValue(Fold.StateProperty);

        if (LocalFold(state, size) is { } fold)
        {
            if (state.Orientation == FoldOrientation.Vertical
                && WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane
                && (state.IsSeparating || (SnapToFold && size.Width > MinWideModeWidth)))
            {
                return Wide(
                    new Rect(0, 0, fold.X, size.Height),
                    new Rect(fold.Right, 0, Math.Max(0, size.Width - fold.Right), size.Height));
            }

            if (state.Orientation == FoldOrientation.Horizontal
                && TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane
                && (state.IsSeparating || (SnapToFold && size.Height > MinTallModeHeight)))
            {
                return Tall(
                    new Rect(0, 0, size.Width, fold.Y),
                    new Rect(0, fold.Bottom, size.Width, Math.Max(0, size.Height - fold.Bottom)));
            }
        }

        if (size.Width > MinWideModeWidth && WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
        {
            var (first, second) = Split(size.Width);
            return Wide(new Rect(0, 0, first, size.Height), new Rect(first, 0, second, size.Height));
        }

        if (size.Height > MinTallModeHeight && TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
        {
            var (first, second) = Split(size.Height);
            return Tall(new Rect(0, 0, size.Width, first), new Rect(0, first, size.Width, second));
        }

        return PanePriority == TwoPaneViewPriority.Pane1
            ? new(TwoPaneViewMode.SinglePane, bounds, null)
            : new(TwoPaneViewMode.SinglePane, null, bounds);
    }

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

    // Pixel lengths are honored, star lengths share the rest; Auto counts as 1*.
    private (double First, double Second) Split(double total)
    {
        var l1 = Pane1Length;
        var l2 = Pane2Length;

        if (l1.IsAbsolute)
        {
            var first = Math.Min(l1.Value, total);
            return (first, total - first);
        }
        if (l2.IsAbsolute)
        {
            var second = Math.Min(l2.Value, total);
            return (total - second, second);
        }

        var s1 = l1.IsStar ? l1.Value : 1;
        var s2 = l2.IsStar ? l2.Value : 1;
        var sum = s1 + s2;
        var firstStar = sum <= 0 ? total / 2 : total * s1 / sum;
        return (firstStar, total - firstStar);
    }
}
