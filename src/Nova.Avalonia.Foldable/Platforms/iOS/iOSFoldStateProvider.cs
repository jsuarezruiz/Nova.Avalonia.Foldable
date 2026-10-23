using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace Nova.Avalonia.Foldable.Platforms.iOS;

/// <summary>
/// Hinge from <see cref="UIHingeInteraction"/>, fold from the view's division region.
/// </summary>
[SupportedOSPlatform("ios27.1")]
[UnsupportedOSPlatform("maccatalyst")]
internal sealed class iOSFoldStateProvider : IFoldStateProvider
{
    // How long after the last hinge update or refresh the fold is read once more.
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(250);

    private readonly UIView _view;
    private readonly UIHingeInteraction _interaction;
    private readonly UIViewReservedRegionKind _division = UIViewReservedRegionKind.CreateDivision();
    private readonly DispatcherTimer _settle = new() { Interval = SettleDelay };
    private UIHingeStatus? _hingeStatus;
    private double? _hingeAngle;
    private Rect? _foldBounds;
    private bool _foldActive;
    private bool _disposed;

    private iOSFoldStateProvider(UIView view)
    {
        _view = view;
        _settle.Tick += OnSettled;
        _interaction = new UIHingeInteraction(OnHingeUpdate);
        _view.AddInteraction(_interaction);
        ReadFold();
        Current = BuildState();
    }

    public FoldState Current { get; private set; }

    public event EventHandler<FoldState>? Changed;

    public static IFoldStateProvider? TryCreate(TopLevel topLevel)
    {
        // The version check passes for an iPhone app on a Mac too, where the hinge classes may be missing.
        if (Class.GetHandle("UIHingeInteraction") == IntPtr.Zero)
            return null;

        // The AvaloniaView; its points map 1:1 to top-level DIPs.
        if (topLevel.TryGetPlatformHandle() is not { HandleDescriptor: "UIView" } handle)
            return null;

        return Runtime.GetNSObject<UIView>(handle.Handle) is { } view
            ? new iOSFoldStateProvider(view)
            : null;
    }

    // The division region changes on its own schedule, not with the hinge status or the view's size: it
    // turns active or inactive while the hinge travels or just after its last update, and appears just
    // after the view moves from the cover to the inner display. Read it now, and once more when things settle.
    public void Refresh()
    {
        if (_disposed)
            return;

        Update();
        _settle.Stop();
        _settle.Start();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _settle.Stop();
        _settle.Tick -= OnSettled;
        _view.RemoveInteraction(_interaction);
        _interaction.Dispose();
        _division.Dispose();
    }

    // UIKit calls this on the main thread, with the initial state first.
    private void OnHingeUpdate(UIHingeInteraction interaction, UIHingeInteractionUpdate update)
    {
        // Its wrapper is released here rather than left to the GC, once per update.
        using var hinge = update.Hinge;
        _hingeStatus = hinge?.Status;
        _hingeAngle = hinge is null ? null : hinge.Angle * 180.0 / Math.PI;
        Refresh();
    }

    private void OnSettled(object? sender, EventArgs e)
    {
        _settle.Stop();
        Update();
    }

    private void Update()
    {
        ReadFold();
        Publish();
    }

    private void Publish()
    {
        var state = BuildState();
        if (state == Current)
            return;
        Current = state;
        Changed?.Invoke(this, state);
    }

    // Occlusion regions are ignored: they also report the camera housing. Prefers an active region;
    // the values are copied out so the region objects can be released right away.
    private void ReadFold()
    {
        var screen = ScreenBounds();
        Rect? bounds = null;
        var active = false;
        foreach (var region in _view.GetReservedRegions(_division, UIViewReservedRegionQueryOptions.IncludeInactive))
        {
            using (region)
            {
                var frame = region.Frame;
                if (!screen.Contains(frame))
                    continue;

                if (bounds is null || (region.Active && !active))
                {
                    bounds = new Rect(frame.X, frame.Y, frame.Width, frame.Height);
                    active = region.Active;
                }
            }
        }

        _foldBounds = bounds;
        _foldActive = active;
    }

    // The view's screen in view coordinates, plus a point for rounding. A fold always fits on its screen,
    // but while the app moves to the cover display, UIKit briefly reports the inner display's fold
    // turned sideways, wider than the cover display.
    private CGRect ScreenBounds()
    {
        var bounds = _view.Window?.WindowScene?.Screen is { } screen
            ? _view.ConvertRectFromCoordinateSpace(screen.Bounds, screen.CoordinateSpace)
            : _view.Bounds;
        return bounds.Inset(-1, -1);
    }

    private FoldState BuildState()
    {
        var hasHinge = _hingeStatus is { } status && status != UIHingeStatus.Unknown;
        if (!hasHinge && _foldBounds is null)
            return FoldState.None;

        var posture = _hingeStatus switch
        {
            UIHingeStatus.Closed => DevicePosture.Closed,
            UIHingeStatus.PartiallyOpen => DevicePosture.HalfOpened,
            UIHingeStatus.FullyOpen => DevicePosture.Flat,
            _ => DevicePosture.Unknown,
        };

        return new FoldState(true, posture, _hingeAngle, _foldBounds, _foldActive);
    }
}
