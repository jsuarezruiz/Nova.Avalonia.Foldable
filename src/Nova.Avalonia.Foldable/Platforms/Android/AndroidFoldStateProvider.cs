using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;
using Avalonia;
using Avalonia.Controls;
using AndroidView = Android.Views.View;
using JavaObject = Java.Lang.Object;

namespace Nova.Avalonia.Foldable.Platforms.Android;

/// <summary>
/// Fold from Jetpack WindowManager's <see cref="IFoldingFeature"/>, hinge angle from the sensor when present.
/// </summary>
internal sealed class AndroidFoldStateProvider : JavaObject, IConsumer, ISensorEventListener, global::Android.App.Application.IActivityLifecycleCallbacks, IFoldStateProvider
{
    private static readonly Java.Lang.Class FoldingFeatureClass = Java.Lang.Class.FromType(typeof(IFoldingFeature));

    private readonly TopLevel _topLevel;
    private readonly AndroidView _view;
    private readonly Activity _activity;
    private readonly WindowInfoTrackerCallbackAdapter _tracker;
    private readonly SensorManager? _sensors;
    private readonly Sensor? _hinge;
    private readonly int[] _location = new int[2];
    private FoldInfo? _fold;
    private double? _hingeAngle;
    private bool _seenFold;

    private AndroidFoldStateProvider(TopLevel topLevel, AndroidView view, Activity activity)
    {
        _topLevel = topLevel;
        _view = view;
        _activity = activity;

        _tracker = new WindowInfoTrackerCallbackAdapter(WindowInfoTracker.Companion.GetOrCreate(activity));
        _tracker.AddWindowLayoutInfoListener(activity, ContextCompat.GetMainExecutor(activity)!, this);

        // WindowManager has no hinge angle; some foldables expose it as a sensor (API 30+).
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            _sensors = activity.GetSystemService(Context.SensorService) as SensorManager;
            _hinge = _sensors?.GetDefaultSensor(SensorType.HingeAngle);
            StartSensor();
        }

        // Sensor listeners keep running in the background unless removed; pause while the activity is stopped.
        activity.Application?.RegisterActivityLifecycleCallbacks(this);

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

    // WindowLayoutInfo updates, delivered on the main thread. The fold is copied out here, so
    // hinge angle updates never go back to Java.
    public void Accept(JavaObject? value)
    {
        _fold = null;

        // The generic Consumer hands over a plain Java object; cast on the Java side.
        using var layout = value?.JavaCast<WindowLayoutInfo>();
        if (layout?.DisplayFeatures is { } features)
        {
            foreach (var feature in features)
            {
                // A C# type test fails on these interface wrappers; ask the Java side.
                if (_fold is null && feature is JavaObject javaFeature && FoldingFeatureClass.IsInstance(javaFeature))
                {
                    using var fold = javaFeature.JavaCast<IFoldingFeature>()!;
                    using var bounds = fold.Bounds;
                    _fold = new FoldInfo(
                        bounds.Left, bounds.Top, bounds.Width(), bounds.Height(),
                        Equals(fold.State, FoldingFeatureState.HalfOpened),
                        fold.IsSeparating);
                }

                (feature as IDisposable)?.Dispose();
            }
        }

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

    public void OnActivityStarted(Activity activity)
    {
        if (activity == _activity)
            StartSensor();
    }

    public void OnActivityStopped(Activity activity)
    {
        if (activity == _activity)
            _sensors?.UnregisterListener(this);
    }

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {
    }

    public void OnActivityResumed(Activity activity)
    {
    }

    public void OnActivityPaused(Activity activity)
    {
    }

    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {
    }

    public void OnActivityDestroyed(Activity activity)
    {
    }

    private void StartSensor()
    {
        if (_hinge is not null)
            _sensors?.RegisterListener(this, _hinge, SensorDelay.Ui);
    }

    private FoldState ReadState()
    {
        _seenFold |= _fold is not null;
        var isFoldable = _seenFold || _hinge is not null;
        if (!isFoldable)
            return FoldState.None;

        DevicePosture posture;
        if (_fold is { } fold)
            posture = fold.IsHalfOpened ? DevicePosture.HalfOpened : DevicePosture.Flat;
        else if (_hingeAngle is { } angle)
            posture = angle < 30 ? DevicePosture.Closed : DevicePosture.Unknown;
        else
            posture = DevicePosture.Unknown; // No report yet, or no fold in the window and no sensor to tell why.

        return new FoldState(true, posture, _hingeAngle, _fold is { } f ? ToTopLevel(f) : null, _fold?.IsSeparating == true);
    }

    // Window pixels to top-level DIPs.
    private Rect ToTopLevel(FoldInfo fold)
    {
        _view.GetLocationInWindow(_location);
        var scale = _topLevel.RenderScaling;
        return new Rect(
            (fold.Left - _location[0]) / scale,
            (fold.Top - _location[1]) / scale,
            fold.Width / scale,
            fold.Height / scale);
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
            _activity.Application?.UnregisterActivityLifecycleCallbacks(this);
        }
        base.Dispose(disposing);
    }

    private readonly record struct FoldInfo(int Left, int Top, int Width, int Height, bool IsHalfOpened, bool IsSeparating);
}
