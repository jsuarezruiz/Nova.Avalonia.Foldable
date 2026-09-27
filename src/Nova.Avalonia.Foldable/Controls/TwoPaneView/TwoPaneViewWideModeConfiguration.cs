namespace Nova.Avalonia.Foldable;

/// <summary>
/// Pane order when the panes are side by side.
/// </summary>
public enum TwoPaneViewWideModeConfiguration
{
    /// <summary>
    /// Never side by side; show one pane instead.
    /// </summary>
    SinglePane,

    /// <summary>
    /// Pane1 on the left, Pane2 on the right.
    /// </summary>
    LeftRight,

    /// <summary>
    /// Pane1 on the right, Pane2 on the left.
    /// </summary>
    RightLeft,
}
