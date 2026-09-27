using System;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// Provider for platforms without a hinge.
/// </summary>
public sealed class NullFoldStateProvider : IFoldStateProvider
{
    /// <inheritdoc/>
    public FoldState Current => FoldState.None;

    /// <inheritdoc/>
    public event EventHandler<FoldState>? Changed { add { } remove { } }

    /// <inheritdoc/>
    public void Refresh() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
