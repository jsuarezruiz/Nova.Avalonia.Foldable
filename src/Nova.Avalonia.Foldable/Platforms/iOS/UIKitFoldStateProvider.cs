using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Nova.Avalonia.Foldable.Bindings.iOS;
using ObjCRuntime;
using UIKit;

namespace Nova.Avalonia.Foldable.Platforms.iOS;

/// <summary>
/// Hinge from <see cref="UIHingeInteraction"/>, fold from the view's division region.
/// </summary>
[SupportedOSPlatform("ios27.1")]
[UnsupportedOSPlatform("maccatalyst")]
internal sealed class UIKitFoldStateProvider : IFoldStateProvider
{
    // How long after the last hinge update the fold is read once more.
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(250);

    private readonly UIView _view;
    private readonly UIHingeInteraction _interaction;
    private readonly DispatcherTimer _settle = new() { Interval = SettleDelay };
    private UIHingeStatus? _hingeStatus;
    private double? _hingeAngle;
    private Rect? _foldBounds;
    private bool _foldActive;

    private UIKitFoldStateProvider(UIView view)
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
            ? new UIKitFoldStateProvider(view)
            : null;
    }

    public void Refresh()
    {
        ReadFold();
        Publish();
    }

    public void Dispose()
    {
        _settle.Stop();
        _view.RemoveInteraction(_interaction);
        _interaction.Dispose();
    }

    // UIKit calls this on the main thread, with the initial state first.
    private void OnHingeUpdate(UIHingeInteraction interaction, UIHingeInteractionUpdate update)
    {
        var hinge = update.Hinge;
        _hingeStatus = hinge?.Status;
        _hingeAngle = hinge is null ? null : hinge.Angle * 180.0 / Math.PI;

        // The division region turns active or inactive on its own schedule, not with the hinge status:
        // while the hinge travels, or just after its last update. Read it with every update, and once
        // more when they stop.
        Refresh();
        _settle.Stop();
        _settle.Start();
    }

    private void OnSettled(object? sender, EventArgs e)
    {
        _settle.Stop();
        Refresh();
    }

    private void Publish()
    {
        var state = BuildState();
        if (state == Current)
            return;
        Current = state;
        Changed?.Invoke(this, state);
    }

    // Occlusion regions are ignored: they also report the camera housing.
    private void ReadFold()
    {
        var regions = _view.GetReservedRegions(
            UIViewReservedRegionKind.CreateDivision(),
            UIViewReservedRegionQueryOptions.IncludeInactive);

        UIViewReservedRegion? fold = null;
        foreach (var region in regions)
        {
            if (fold is null || (region.Active && !fold.Active))
                fold = region;
        }

        _foldBounds = fold is null ? null : new Rect(fold.Frame.X, fold.Frame.Y, fold.Frame.Width, fold.Frame.Height);
        _foldActive = fold?.Active == true;
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
