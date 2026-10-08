using System;
using System.Collections.Generic;
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

    [AvaloniaFact]
    public void Accessors_Agree_With_The_State_In_Any_Change_Handler()
    {
        var window = new Window();
        var fold = new SimulatedFoldStateProvider();
        Fold.SetProvider(window, fold);
        var state = new FoldState(true, DevicePosture.HalfOpened, 110, new Rect(380, 0, 40, 600), true);
        var seen = new List<(AvaloniaProperty Property, FoldState State, DevicePosture Posture, bool IsSeparating, double? HingeAngle)>();
        window.PropertyChanged += (_, e) =>
        {
            // The first, a middle and the last property to change.
            if (e.Property == Fold.StateProperty || e.Property == Fold.PostureProperty || e.Property == Fold.HingeAngleProperty)
                seen.Add((e.Property, Fold.GetState(window), Fold.GetPosture(window), Fold.GetIsSeparating(window), Fold.GetHingeAngle(window)));
        };

        fold.Current = state;

        Assert.Equal(3, seen.Count);
        Assert.All(seen, s => Assert.Equal((state, DevicePosture.HalfOpened, true, 110d), (s.State, s.Posture, s.IsSeparating, s.HingeAngle)));
    }

    [AvaloniaFact]
    public void Setting_A_Null_Provider_Throws_And_Keeps_The_Current_One()
    {
        var window = new Window();
        var provider = new CountingProvider();
        Fold.SetProvider(window, provider);

        Assert.Throws<ArgumentNullException>(() => Fold.SetProvider(window, null!));

        Assert.Same(provider, Fold.GetProvider(window));
        Assert.Equal(0, provider.Disposals);
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
