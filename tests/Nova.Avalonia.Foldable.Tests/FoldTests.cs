using System;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Nova.Avalonia.Foldable.Tests;

public class FoldTests
{
    [AvaloniaFact]
    public void Closing_The_Window_Disposes_Its_Provider()
    {
        var window = new Window();
        var provider = new CountingProvider();
        Fold.SetProvider(window, provider);
        window.Show();

        window.Close();

        Assert.Equal(1, provider.Disposals);
    }

    private sealed class CountingProvider : IFoldStateProvider
    {
        public int Disposals { get; private set; }

        public FoldState Current => FoldState.None;

        public event EventHandler<FoldState>? Changed { add { } remove { } }

        public void Refresh() { }

        public void Dispose() => Disposals++;
    }
}
