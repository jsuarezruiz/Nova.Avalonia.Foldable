namespace Nova.Avalonia.Foldable;

/// <summary>
/// Physical posture of a foldable device.
/// </summary>
public enum DevicePosture
{
    /// <summary>
    /// The device doesn't fold, or the hinge hasn't reported yet.
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
