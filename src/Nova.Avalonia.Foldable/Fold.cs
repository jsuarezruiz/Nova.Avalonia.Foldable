using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// The fold state of a <see cref="TopLevel"/> as inherited attached properties.
/// </summary>
public static class Fold
{
    private static readonly ConditionalWeakTable<TopLevel, Registration> s_registrations = new();
    private static readonly NullFoldStateProvider s_closedProvider = new();

    /// <summary>
    /// Creates the provider for each new top level; return null to keep the platform's default.
    /// </summary>
    /// <remarks>The top level owns the provider and disposes it when it closes, so return a new instance each time.</remarks>
    public static Func<TopLevel, IFoldStateProvider?>? ProviderFactory { get; set; }

    /// <summary>
    /// Defines the IsEnabled attached property, which starts tracking the fold of the control's top level.
    /// </summary>
    /// <remarks>
    /// Tracking is shared by everything on the top level, <see cref="TwoPaneView"/> included, so it lasts until
    /// the top level closes; setting the property back to false doesn't stop it.
    /// </remarks>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(Fold));

    /// <summary>
    /// Defines the State attached property, read with <see cref="GetState"/>.
    /// </summary>
    public static readonly AttachedProperty<FoldState> StateProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, FoldState>("State", typeof(Fold), FoldState.None, inherits: true);

    /// <summary>
    /// Defines the Posture attached property, read with <see cref="GetPosture"/>.
    /// </summary>
    public static readonly AttachedProperty<DevicePosture> PostureProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, DevicePosture>("Posture", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the IsFoldable attached property, read with <see cref="GetIsFoldable"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> IsFoldableProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsFoldable", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the IsSeparating attached property, read with <see cref="GetIsSeparating"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSeparatingProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsSeparating", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the Orientation attached property, read with <see cref="GetOrientation"/>.
    /// </summary>
    public static readonly AttachedProperty<FoldOrientation> OrientationProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, FoldOrientation>("Orientation", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the HingeAngle attached property, read with <see cref="GetHingeAngle"/>.
    /// </summary>
    public static readonly AttachedProperty<double?> HingeAngleProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double?>("HingeAngle", typeof(Fold), inherits: true);

    static Fold()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    /// <summary>
    /// Gets whether the control asked to track the fold of its top level.
    /// </summary>
    public static bool GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);

    /// <summary>
    /// Starts tracking the fold of the control's top level when <paramref name="value"/> is true.
    /// </summary>
    /// <remarks>Tracking lasts until the top level closes; false doesn't stop it.</remarks>
    public static void SetIsEnabled(Control element, bool value) => element.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// Gets the fold state inherited from the top level.
    /// </summary>
    public static FoldState GetState(StyledElement element) => element.GetValue(StateProperty);

    // The accessors below read the state rather than their own property, so they agree with it
    // even inside a change handler that runs before every property has been updated.

    /// <summary>
    /// Gets the device posture inherited from the top level.
    /// </summary>
    public static DevicePosture GetPosture(StyledElement element) => GetState(element).Posture;

    /// <summary>
    /// Gets whether the device has a hinge.
    /// </summary>
    public static bool GetIsFoldable(StyledElement element) => GetState(element).IsFoldable;

    /// <summary>
    /// Gets whether the fold currently divides the content.
    /// </summary>
    public static bool GetIsSeparating(StyledElement element) => GetState(element).IsSeparating;

    /// <summary>
    /// Gets the direction of the fold.
    /// </summary>
    public static FoldOrientation GetOrientation(StyledElement element) => GetState(element).Orientation;

    /// <summary>
    /// Gets the hinge angle in degrees, or null when unknown.
    /// </summary>
    public static double? GetHingeAngle(StyledElement element) => GetState(element).HingeAngle;

    /// <summary>
    /// Returns the provider for <paramref name="topLevel"/>, creating it on first use.
    /// </summary>
    /// <remarks>Once the top level has closed, returns a provider that reports no fold and tracks nothing.</remarks>
    public static IFoldStateProvider GetProvider(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);

        if (s_registrations.TryGetValue(topLevel, out var existing))
            return existing.Provider;

        // A closed top level would never dispose a new provider, or the platform listeners it starts.
        if (topLevel.PlatformImpl is null)
            return s_closedProvider;

        var provider = ProviderFactory?.Invoke(topLevel) ?? CreatePlatformProvider(topLevel) ?? new NullFoldStateProvider();
        SetProvider(topLevel, provider);
        return provider;
    }

    /// <summary>
    /// Sets the provider of <paramref name="topLevel"/>, disposing the old one.
    /// </summary>
    /// <remarks>
    /// The top level owns the provider and disposes it when replaced or closed, so don't share one instance between
    /// top levels. A top level that has already closed disposes it right away.
    /// </remarks>
    public static void SetProvider(TopLevel topLevel, IFoldStateProvider provider)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        ArgumentNullException.ThrowIfNull(provider);

        if (topLevel.PlatformImpl is null)
        {
            provider.Dispose();
            return;
        }

        if (s_registrations.TryGetValue(topLevel, out var existing))
        {
            if (ReferenceEquals(existing.Provider, provider))
                return;
            existing.Dispose();
            s_registrations.Remove(topLevel);
        }

        // Registered before the first state goes out, so a handler that asks for the provider finds this one.
        var registration = new Registration(topLevel, provider);
        s_registrations.Add(topLevel, registration);
        registration.Apply(provider.Current);
    }

    private static IFoldStateProvider? CreatePlatformProvider(TopLevel topLevel)
    {
#if IOS
        if (!OperatingSystem.IsIOSVersionAtLeast(27, 1))
            return null;
#pragma warning disable CA1416 // The iOS build never loads on Mac Catalyst, which the analyzer can't tell.
        return Platforms.iOS.iOSFoldStateProvider.TryCreate(topLevel);
#pragma warning restore CA1416
#elif ANDROID
        return Platforms.Android.AndroidFoldStateProvider.TryCreate(topLevel);
#else
        return null;
#endif
    }

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.AttachedToVisualTree -= OnAttachedToVisualTree;
        if (e.NewValue is not true)
            return;

        control.AttachedToVisualTree += OnAttachedToVisualTree;
        if (TopLevel.GetTopLevel(control) is { } topLevel)
            GetProvider(topLevel);
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control control && TopLevel.GetTopLevel(control) is { } topLevel)
            GetProvider(topLevel);
    }

    private sealed class Registration : IDisposable
    {
        private readonly TopLevel _topLevel;
        private FoldState _applied = FoldState.None;
        private FoldState? _pendingAngle;
        private bool _throttling;
        private bool _disposed;

        public Registration(TopLevel topLevel, IFoldStateProvider provider)
        {
            _topLevel = topLevel;
            Provider = provider;
            Provider.Changed += OnChanged;
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
            _topLevel.Closed += OnTopLevelClosed;
            _topLevel.ScalingChanged += OnScalingChanged;
        }

        public IFoldStateProvider Provider { get; }

        public void Dispose()
        {
            _disposed = true;
            Provider.Changed -= OnChanged;
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
            _topLevel.Closed -= OnTopLevelClosed;
            _topLevel.ScalingChanged -= OnScalingChanged;
            Provider.Dispose();
        }

        // A closed window, or an Android activity's destroyed view, no longer needs the platform listeners.
        private void OnTopLevelClosed(object? sender, EventArgs e)
        {
            s_registrations.Remove(_topLevel);
            Dispose();
        }

        // The hinge angle can change several times a frame while the device folds, and every change
        // reaches each control below the top level. Angle-only changes go out at most once a frame: the
        // first at once, the rest as the latest one on the next frame. Anything else goes out at once.
        private void OnChanged(object? sender, FoldState state)
        {
            if (state with { HingeAngle = _applied.HingeAngle } == _applied)
            {
                if (_throttling)
                {
                    _pendingAngle = state;
                    return;
                }
                StartThrottle();
            }

            _pendingAngle = null;
            Apply(state);
        }

        private void StartThrottle()
        {
            _throttling = true;
            _topLevel.RequestAnimationFrame(OnFrame);
        }

        // Stays on while the angle keeps changing, so a steady stream is applied once per frame.
        private void OnFrame(TimeSpan time)
        {
            _throttling = false;
            if (_disposed || _pendingAngle is not { } state)
                return;

            _pendingAngle = null;
            StartThrottle();
            Apply(state);
        }

        // A density change rescales the fold and arrives after the resize, so it gets its own refresh.
        private void OnScalingChanged(object? sender, EventArgs e) => Provider.Refresh();

        // Resizing or rotating moves the fold without a hinge change.
        private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.ClientSizeProperty)
                Provider.Refresh();
        }

        // State goes first: the Get accessors read it, so a handler on any of these properties sees the whole snapshot.
        public void Apply(FoldState state)
        {
            _applied = state;
            _topLevel.SetValue(StateProperty, state);
            _topLevel.SetValue(PostureProperty, state.Posture);
            _topLevel.SetValue(IsFoldableProperty, state.IsFoldable);
            _topLevel.SetValue(IsSeparatingProperty, state.IsSeparating);
            _topLevel.SetValue(OrientationProperty, state.Orientation);
            _topLevel.SetValue(HingeAngleProperty, state.HingeAngle);
        }
    }
}
