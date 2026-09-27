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

    /// <summary>
    /// Replaces the platform provider; return null to keep the default.
    /// </summary>
    public static Func<TopLevel, IFoldStateProvider?>? ProviderFactory { get; set; }

    /// <summary>
    /// Tracks the fold of the control's top level (TwoPaneView does it for you).
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(Fold));

    /// <summary>
    /// Defines the State attached property.
    /// </summary>
    public static readonly AttachedProperty<FoldState> StateProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, FoldState>("State", typeof(Fold), FoldState.None, inherits: true);

    /// <summary>
    /// Defines the Posture attached property.
    /// </summary>
    public static readonly AttachedProperty<DevicePosture> PostureProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, DevicePosture>("Posture", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the IsFoldable attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsFoldableProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsFoldable", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the IsSeparating attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSeparatingProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("IsSeparating", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the FoldOrientation attached property.
    /// </summary>
    public static readonly AttachedProperty<FoldOrientation> FoldOrientationProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, FoldOrientation>("FoldOrientation", typeof(Fold), inherits: true);

    /// <summary>
    /// Defines the HingeAngle attached property.
    /// </summary>
    public static readonly AttachedProperty<double?> HingeAngleProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double?>("HingeAngle", typeof(Fold), inherits: true);

    static Fold()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    /// <summary>
    /// Gets whether the fold of the control's top level is tracked.
    /// </summary>
    public static bool GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);

    /// <summary>
    /// Sets whether the fold of the control's top level is tracked.
    /// </summary>
    public static void SetIsEnabled(Control element, bool value) => element.SetValue(IsEnabledProperty, value);

    /// <summary>
    /// Gets the fold state inherited from the top level.
    /// </summary>
    public static FoldState GetState(StyledElement element) => element.GetValue(StateProperty);

    /// <summary>
    /// Gets the device posture inherited from the top level.
    /// </summary>
    public static DevicePosture GetPosture(StyledElement element) => element.GetValue(PostureProperty);

    /// <summary>
    /// Gets whether the device has a hinge.
    /// </summary>
    public static bool GetIsFoldable(StyledElement element) => element.GetValue(IsFoldableProperty);

    /// <summary>
    /// Gets whether the fold currently divides the content.
    /// </summary>
    public static bool GetIsSeparating(StyledElement element) => element.GetValue(IsSeparatingProperty);

    /// <summary>
    /// Gets the direction of the fold.
    /// </summary>
    public static FoldOrientation GetFoldOrientation(StyledElement element) => element.GetValue(FoldOrientationProperty);

    /// <summary>
    /// Gets the hinge angle in degrees, or null when unknown.
    /// </summary>
    public static double? GetHingeAngle(StyledElement element) => element.GetValue(HingeAngleProperty);

    /// <summary>
    /// Returns the provider for <paramref name="topLevel"/>, creating it on first use.
    /// </summary>
    public static IFoldStateProvider GetProvider(TopLevel topLevel)
    {
        if (s_registrations.TryGetValue(topLevel, out var existing))
            return existing.Provider;

        var provider = ProviderFactory?.Invoke(topLevel) ?? CreatePlatformProvider(topLevel) ?? new NullFoldStateProvider();
        SetProvider(topLevel, provider);
        return provider;
    }

    /// <summary>
    /// Sets the provider of <paramref name="topLevel"/>, disposing the old one.
    /// </summary>
    public static void SetProvider(TopLevel topLevel, IFoldStateProvider provider)
    {
        if (s_registrations.TryGetValue(topLevel, out var existing))
        {
            if (ReferenceEquals(existing.Provider, provider))
                return;
            existing.Dispose();
            s_registrations.Remove(topLevel);
        }

        s_registrations.Add(topLevel, new Registration(topLevel, provider));
    }

    private static IFoldStateProvider? CreatePlatformProvider(TopLevel topLevel)
    {
#if IOS
        if (OperatingSystem.IsMacCatalyst() || !OperatingSystem.IsIOSVersionAtLeast(27, 1))
            return null;
#pragma warning disable CA1416 // Guarded above; the analyzer doesn't narrow iOS vs Mac Catalyst here.
        return Platforms.iOS.UIKitFoldStateProvider.TryCreate(topLevel);
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

        public Registration(TopLevel topLevel, IFoldStateProvider provider)
        {
            _topLevel = topLevel;
            Provider = provider;
            Provider.Changed += OnChanged;
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
            Apply(Provider.Current);
        }

        public IFoldStateProvider Provider { get; }

        public void Dispose()
        {
            Provider.Changed -= OnChanged;
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
            Provider.Dispose();
        }

        private void OnChanged(object? sender, FoldState state) => Apply(state);

        // Resizing or rotating moves the fold without a hinge change.
        private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.ClientSizeProperty)
                Provider.Refresh();
        }

        private void Apply(FoldState state)
        {
            _topLevel.SetValue(StateProperty, state);
            _topLevel.SetValue(PostureProperty, state.Posture);
            _topLevel.SetValue(IsFoldableProperty, state.IsFoldable);
            _topLevel.SetValue(IsSeparatingProperty, state.IsSeparating);
            _topLevel.SetValue(FoldOrientationProperty, state.Orientation);
            _topLevel.SetValue(HingeAngleProperty, state.HingeAngle);
        }
    }
}
