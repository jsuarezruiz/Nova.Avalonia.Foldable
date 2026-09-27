namespace Nova.Avalonia.Foldable;

/// <summary>
/// Direction of the fold line.
/// </summary>
public enum FoldOrientation
{
    /// <summary>
    /// No fold is present.
    /// </summary>
    None,

    /// <summary>
    /// The fold runs top to bottom; content is split left and right.
    /// </summary>
    Vertical,

    /// <summary>
    /// The fold runs left to right; content is split top and bottom.
    /// </summary>
    Horizontal,
}
