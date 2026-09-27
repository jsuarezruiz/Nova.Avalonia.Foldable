using System;

namespace Nova.Avalonia.Foldable;

/// <summary>
/// A provider driven from code, for desktop, the previewer and tests.
/// </summary>
public sealed class SimulatedFoldStateProvider : IFoldStateProvider
{
    private FoldState _current;

    /// <summary>
    /// Creates a provider that starts at <paramref name="initial"/>, or no fold.
    /// </summary>
    public SimulatedFoldStateProvider(FoldState? initial = null)
    {
        _current = initial ?? FoldState.None;
    }

    /// <summary>
    /// The simulated state. Setting a new value raises <see cref="Changed"/>.
    /// </summary>
    public FoldState Current
    {
        get => _current;
        set
        {
            if (_current == value)
                return;
            _current = value;
            Changed?.Invoke(this, value);
        }
    }

    /// <inheritdoc/>
    public event EventHandler<FoldState>? Changed;

    /// <inheritdoc/>
    public void Refresh() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
