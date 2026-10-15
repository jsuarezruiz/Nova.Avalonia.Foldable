using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
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

    [AvaloniaFact]
    public async Task Hinge_Angle_Updates_Are_Applied_At_Most_Once_Per_Frame()
    {
        var window = new Window();
        var state = new FoldState(true, DevicePosture.HalfOpened, 90, new Rect(380, 0, 40, 600), true);
        var fold = new SimulatedFoldStateProvider(state);
        Fold.SetProvider(window, fold);
        window.Show();
        var angles = new List<double?>();
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Fold.HingeAngleProperty)
                angles.Add(Fold.GetHingeAngle(window));
        };

        // The first update goes out at once; the rest of the frame's updates collapse into the last one.
        for (var angle = 91; angle <= 100; angle++)
            fold.Current = state with { HingeAngle = angle };
        Assert.Equal([91d], angles);

        await NextFrames(2);
        Assert.Equal([91d, 100d], angles);
        Assert.Equal(100, Fold.GetState(window).HingeAngle);
    }

    [AvaloniaFact]
    public void Other_Changes_Are_Applied_At_Once_With_The_Latest_Angle()
    {
        var window = new Window();
        var state = new FoldState(true, DevicePosture.HalfOpened, 90, new Rect(380, 0, 40, 600), true);
        var fold = new SimulatedFoldStateProvider(state);
        Fold.SetProvider(window, fold);
        window.Show();

        fold.Current = state with { HingeAngle = 120 };
        fold.Current = state with { HingeAngle = 150 };
        fold.Current = state with { HingeAngle = 180, Posture = DevicePosture.Flat, IsSeparating = false };

        Assert.Equal(DevicePosture.Flat, Fold.GetPosture(window));
        Assert.Equal(180, Fold.GetHingeAngle(window));
    }

    private static async Task NextFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void A_Closed_Top_Level_Gets_No_New_Provider()
    {
        var window = new Window();
        window.Show();
        window.Close();
        var created = 0;
        Fold.ProviderFactory = _ =>
        {
            created++;
            return new CountingProvider();
        };

        try
        {
            Assert.Equal(FoldState.None, Fold.GetProvider(window).Current);
            Assert.Equal(0, created);

            // Nothing would ever dispose it, so it is disposed at once.
            var late = new CountingProvider();
            Fold.SetProvider(window, late);
            Assert.Equal(1, late.Disposals);
        }
        finally
        {
            Fold.ProviderFactory = null;
        }
    }

    [AvaloniaFact]
    public void A_Null_Top_Level_Throws()
    {
        Assert.Equal("topLevel", Assert.Throws<ArgumentNullException>(() => Fold.GetProvider(null!)).ParamName);
        Assert.Equal("topLevel", Assert.Throws<ArgumentNullException>(() => Fold.SetProvider(null!, new CountingProvider())).ParamName);
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
