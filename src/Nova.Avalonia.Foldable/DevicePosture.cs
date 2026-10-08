namespace Nova.Avalonia.Foldable;

/// <summary>
/// Physical posture of a foldable device.
/// </summary>
public enum DevicePosture
{
    /// <summary>
    /// The device doesn't fold, the hinge hasn't reported yet, or the platform can't tell,
    /// as on Android when the window doesn't span the fold.
    /// </summary>
    Unknown,

    /// <summary>
    /// Folded shut; the app is on the cover display.
    /// </summary>
    Closed,

    /// <summary>
    /// Partially open (book or tabletop posture).
    /// </summary>
    HalfOpened,

    /// <summary>
    /// Fully open, flat.
    /// </summary>
    Flat,
}
