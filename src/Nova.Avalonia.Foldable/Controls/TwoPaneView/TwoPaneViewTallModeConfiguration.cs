namespace Nova.Avalonia.Foldable;

/// <summary>
/// Pane order when the panes are stacked.
/// </summary>
public enum TwoPaneViewTallModeConfiguration
{
    /// <summary>
    /// Never stacked; show one pane instead.
    /// </summary>
    SinglePane,

    /// <summary>
    /// Pane1 on top, Pane2 below.
    /// </summary>
    TopBottom,

    /// <summary>
    /// Pane1 below, Pane2 on top.
    /// </summary>
    BottomTop,
}
