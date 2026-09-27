using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AndroidView = Android.Views.View;
using JavaObject = Java.Lang.Object;

namespace Nova.Avalonia.Foldable.Platforms.Android;

/// <summary>
/// Fold from Jetpack WindowManager's <see cref="IFoldingFeature"/>, hinge angle from the sensor where
/// the device has one.
/// </summary>
internal sealed class AndroidFoldStateProvider : JavaObject, IConsumer, ISensorEventListener, IFoldStateProvider
{
    private static readonly Java.Lang.Class FoldingFeatureClass = Java.Lang.Class.FromType(typeof(IFoldingFeature));

    private readonly TopLevel _topLevel;
    private readonly AndroidView _view;
    private readonly WindowInfoTrackerCallbackAdapter _tracker;
    private readonly SensorManager? _sensors;
    private readonly Sensor? _hinge;
    private readonly IActivatableLifetime? _lifetime;
    private WindowLayoutInfo? _layout;
    private double? _hingeAngle;
    private bool _seenFold;

    private AndroidFoldStateProvider(TopLevel topLevel, AndroidView view, Activity activity)
    {
        _topLevel = topLevel;
        _view = view;

        _tracker = new WindowInfoTrackerCallbackAdapter(WindowInfoTracker.Companion.GetOrCreate(activity));
        _tracker.AddWindowLayoutInfoListener(activity, ContextCompat.GetMainExecutor(activity)!, this);

        // WindowManager has no hinge angle; some foldables expose it as a sensor (API 30+).
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            _sensors = activity.GetSystemService(Context.SensorService) as SensorManager;
            _hinge = _sensors?.GetDefaultSensor(SensorType.HingeAngle);
            StartSensor();
        }

        // Sensor listeners keep running in the background unless removed; pause while the app is stopped.
        _lifetime = global::Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>();
        if (_lifetime is not null)
        {
            _lifetime.Activated += OnActivated;
            _lifetime.Deactivated += OnDeactivated;
        }

        Current = ReadState();
    }

    public FoldState Current { get; private set; }

    public event EventHandler<FoldState>? Changed;

    public static IFoldStateProvider? TryCreate(TopLevel topLevel)
    {
        if (topLevel.TryGetPlatformHandle() is not { } handle || handle.Handle == IntPtr.Zero)
            return null;
        if (GetObject<AndroidView>(handle.Handle, JniHandleOwnership.DoNotTransfer) is not { } view)
            return null;
        return FindActivity(view.Context) is { } activity
            ? new AndroidFoldStateProvider(topLevel, view, activity)
            : null;
    }

    public void Refresh()
    {
        var state = ReadState();
        if (state == Current)
            return;
        Current = state;
        Changed?.Invoke(this, state);
    }

    // WindowLayoutInfo updates, delivered on the main thread.
    public void Accept(JavaObject? value)
    {
        // The generic Consumer hands over a plain Java object; cast on the Java side.
        _layout = value?.JavaCast<WindowLayoutInfo>();
        Refresh();
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Values is { Count: > 0 } values)
        {
            _hingeAngle = values[0];
            Refresh();
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
    {
    }

    private void StartSensor()
    {
        if (_hinge is not null)
            _sensors?.RegisterListener(this, _hinge, SensorDelay.Ui);
    }

    private void OnActivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind == ActivationKind.Background)
            StartSensor();
    }

    private void OnDeactivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind == ActivationKind.Background)
            _sensors?.UnregisterListener(this);
    }

    private FoldState ReadState()
    {
        IFoldingFeature? fold = null;
        if (_layout?.DisplayFeatures is { } features)
        {
            // A C# type test fails on these interface wrappers; ask the Java side.
            foreach (var feature in features)
            {
                if (feature is JavaObject javaFeature && FoldingFeatureClass.IsInstance(javaFeature))
                {
                    fold = javaFeature.JavaCast<IFoldingFeature>();
                    break;
                }
            }
        }

        _seenFold |= fold is not null;
        var isFoldable = _seenFold || _hinge is not null;
        if (!isFoldable)
            return FoldState.None;

        DevicePosture posture;
        if (fold is not null)
            posture = Equals(fold.State, FoldingFeatureState.HalfOpened) ? DevicePosture.HalfOpened : DevicePosture.Flat;
        else if (_hingeAngle is { } angle)
            posture = angle < 30 ? DevicePosture.Closed : DevicePosture.Unknown;
        else if (_layout is null)
            posture = DevicePosture.Unknown; // WindowManager hasn't reported yet.
        else
            posture = DevicePosture.Closed; // A foldable with no fold in the window: the cover display.

        return new FoldState(true, posture, _hingeAngle, fold is null ? null : ToTopLevel(fold.Bounds), fold?.IsSeparating == true);
    }

    // Window pixels to top-level DIPs.
    private Rect? ToTopLevel(global::Android.Graphics.Rect? bounds)
    {
        if (bounds is null)
            return null;
        var location = new int[2];
        _view.GetLocationInWindow(location);
        var scale = _topLevel.RenderScaling;
        return new Rect(
            (bounds.Left - location[0]) / scale,
            (bounds.Top - location[1]) / scale,
            bounds.Width() / scale,
            bounds.Height() / scale);
    }

    private static Activity? FindActivity(Context? context)
    {
        while (context is not null)
        {
            if (context is Activity activity)
                return activity;
            context = (context as ContextWrapper)?.BaseContext;
        }
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tracker.RemoveWindowLayoutInfoListener(this);
            _sensors?.UnregisterListener(this);
            if (_lifetime is not null)
            {
                _lifetime.Activated -= OnActivated;
                _lifetime.Deactivated -= OnDeactivated;
            }
        }
        base.Dispose(disposing);
    }
}
