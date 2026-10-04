using System;
using Avalonia;
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

    [AvaloniaFact]
    public void Asking_For_The_Provider_During_Registration_Returns_It()
    {
        var window = new Window();
        IFoldStateProvider? seen = null;
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Fold.StateProperty)
                seen = Fold.GetProvider(window);
        };
        var provider = new SimulatedFoldStateProvider(
            new FoldState(true, DevicePosture.HalfOpened, 110, new Rect(380, 0, 40, 600), true));

        Fold.SetProvider(window, provider);

        Assert.Same(provider, seen);
        Assert.Same(provider, Fold.GetProvider(window));
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
