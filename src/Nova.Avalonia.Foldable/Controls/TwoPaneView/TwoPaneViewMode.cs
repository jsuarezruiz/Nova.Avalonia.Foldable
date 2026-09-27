namespace Nova.Avalonia.Foldable;

/// <summary>
/// How <see cref="TwoPaneView"/> is showing its panes.
/// </summary>
public enum TwoPaneViewMode
{
    /// <summary>
    /// One pane fills the view.
    /// </summary>
    SinglePane,

    /// <summary>
    /// The panes are side by side.
    /// </summary>
    Wide,

    /// <summary>
    /// The panes are stacked.
    /// </summary>
    Tall,
}
