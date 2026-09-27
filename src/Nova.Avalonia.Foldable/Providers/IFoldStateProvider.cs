using System;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// Fold state source for one top level. Raises <see cref="Changed"/> on the UI thread.
/// </summary>
public interface IFoldStateProvider : IDisposable
{
    /// <summary>
    /// The latest fold state.
    /// </summary>
    FoldState Current { get; }

    /// <summary>
    /// Raised when <see cref="Current"/> changes.
    /// </summary>
    event EventHandler<FoldState>? Changed;

    /// <summary>
    /// Re-reads the fold, e.g. after the top level was resized or rotated.
    /// </summary>
    void Refresh();
}
