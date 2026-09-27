using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Nova.Avalonia.Foldable.Bindings.iOS;
using ObjCRuntime;
using UIKit;

namespace Nova.Avalonia.Foldable.Platforms.iOS;

/// <summary>
/// Hinge from <see cref="UIHingeInteraction"/>, fold from the view's division region. Occlusion
/// regions are ignored: they also report the camera housing.
/// </summary>
[SupportedOSPlatform("ios27.1")]
[UnsupportedOSPlatform("maccatalyst")]
internal sealed class UIKitFoldStateProvider : IFoldStateProvider
{
    private readonly UIView _view;
    private readonly UIHingeInteraction _interaction;
    private UIHingeStatus? _hingeStatus;
    private double? _hingeAngle;

    private UIKitFoldStateProvider(UIView view)
    {
        _view = view;
        _interaction = new UIHingeInteraction(OnHingeUpdate);
        _view.AddInteraction(_interaction);
        Current = ReadState();
    }

    public FoldState Current { get; private set; }

    public event EventHandler<FoldState>? Changed;

    public static IFoldStateProvider? TryCreate(TopLevel topLevel)
    {
        // The AvaloniaView; its points map 1:1 to top-level DIPs.
        if (topLevel.TryGetPlatformHandle() is not { HandleDescriptor: "UIView" } handle)
            return null;

        return Runtime.GetNSObject<UIView>(handle.Handle) is { } view
            ? new UIKitFoldStateProvider(view)
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

    public void Dispose()
    {
        _view.RemoveInteraction(_interaction);
        _interaction.Dispose();
    }

    // UIKit calls this on the main thread, with the initial state first.
    private void OnHingeUpdate(UIHingeInteraction interaction, UIHingeInteractionUpdate update)
    {
        if (update.Hinge is { } hinge)
        {
            _hingeStatus = hinge.Status;
            _hingeAngle = hinge.Angle * 180.0 / Math.PI;
        }
        else
        {
            // The view left a hierarchy that delivers hinge updates.
            _hingeStatus = null;
            _hingeAngle = null;
        }

        Refresh();
    }

    private FoldState ReadState()
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

        var hasHinge = _hingeStatus is { } status && status != UIHingeStatus.Unknown;
        if (!hasHinge && fold is null)
            return FoldState.None;

        var posture = _hingeStatus switch
        {
            UIHingeStatus.Closed => DevicePosture.Closed,
            UIHingeStatus.PartiallyOpen => DevicePosture.HalfOpened,
            UIHingeStatus.FullyOpen => DevicePosture.Flat,
            _ => DevicePosture.Unknown,
        };

        Rect? bounds = fold is null
            ? null
            : new Rect(fold.Frame.X, fold.Frame.Y, fold.Frame.Width, fold.Frame.Height);

        return new FoldState(true, posture, _hingeAngle, bounds, fold?.Active == true);
    }
}
