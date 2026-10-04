using Avalonia;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// Snapshot of the fold and hinge for a <see cref="global::Avalonia.Controls.TopLevel"/>.
/// </summary>
/// <param name="IsFoldable">True when the device has a hinge.</param>
/// <param name="Posture">Current posture reported by the hinge.</param>
/// <param name="HingeAngle">Hinge angle in degrees (0 closed, 180 flat), or null when unknown.</param>
/// <param name="FoldBounds">The fold in top-level coordinates, or null when there is none.</param>
/// <param name="IsSeparating">True when the fold currently divides the content, as while half open.</param>
public sealed record FoldState(
    bool IsFoldable,
    DevicePosture Posture,
    double? HingeAngle,
    Rect? FoldBounds,
    bool IsSeparating)
{
    /// <summary>
    /// Gets the state reported on devices that don't fold.
    /// </summary>
    public static FoldState None { get; } = new(false, DevicePosture.Unknown, null, null, false);

    /// <summary>
    /// Gets the direction of <see cref="FoldBounds"/>.
    /// </summary>
    public FoldOrientation Orientation => FoldBounds is { } fold
        ? fold.Width <= fold.Height ? FoldOrientation.Vertical : FoldOrientation.Horizontal
        : FoldOrientation.None;

    /// <summary>
    /// Gets whether the device is half open with a vertical fold (book posture).
    /// </summary>
    public bool IsBook => Posture == DevicePosture.HalfOpened && Orientation == FoldOrientation.Vertical;

    /// <summary>
    /// Gets whether the device is half open with a horizontal fold (tabletop posture).
    /// </summary>
    public bool IsTabletop => Posture == DevicePosture.HalfOpened && Orientation == FoldOrientation.Horizontal;
}
