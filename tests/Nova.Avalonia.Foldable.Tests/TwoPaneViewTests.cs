using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Nova.Avalonia.Foldable.Tests;

public class TwoPaneViewTests
{
    private static (Window Window, TwoPaneView View, SimulatedFoldStateProvider Fold) Create(double width, double height, FoldState? state = null)
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var window = new Window { Width = width, Height = height, Content = view };
        var fold = new SimulatedFoldStateProvider(state);
        Fold.SetProvider(window, fold);
        window.Show();
        return (window, view, fold);
    }

    private static FoldState FoldAt(DevicePosture posture, Rect bounds, bool separating) =>
        new(true, posture, posture == DevicePosture.HalfOpened ? 110 : 180, bounds, separating);

    // Runs frames until the condition holds: a render transform is followed once it has held still for one.
    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    // Transform.Changed is a field-like event; counts the handlers the view has on it.
    private static int Watchers(Transform transform, TwoPaneView view)
    {
        var field = typeof(Transform).GetField(nameof(Transform.Changed), BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field is not null, "Transform.Changed is no longer a field-like event; this helper needs another way to count.");
        return ((Delegate?)field!.GetValue(transform))?.GetInvocationList().Count(d => ReferenceEquals(d.Target, view)) ?? 0;
    }

    [AvaloniaFact]
    public void Wide_Without_Fold_Splits_By_Star_Lengths()
    {
        var (_, view, _) = Create(1000, 500);
        view.Pane1Length = new GridLength(1, GridUnitType.Star);
        view.Pane2Length = new GridLength(3, GridUnitType.Star);
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 250, 500), view.Pane1!.Bounds);
        Assert.Equal(new Rect(250, 0, 750, 500), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Min_Wide_Mode_Width_Is_Exclusive_Like_WinUI()
    {
        var (window, view, _) = Create(641, 500);

        Assert.Equal(TwoPaneViewWideModeConfiguration.LeftRight, view.WideModeConfiguration);
        Assert.Equal(TwoPaneViewTallModeConfiguration.TopBottom, view.TallModeConfiguration);
        Assert.Equal(TwoPaneViewMode.SinglePane, view.Mode);

        // The headless window applies the new size on the dispatcher.
        window.Width = 642;
        Dispatcher.UIThread.RunJobs();
        view.UpdateLayout();

        Assert.Equal(642, view.Bounds.Width);
        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
    }

    [AvaloniaFact]
    public void Narrow_Without_Fold_Shows_Priority_Pane_Only()
    {
        var (_, view, _) = Create(400, 500);
        view.PanePriority = TwoPaneViewPriority.Pane2;
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.SinglePane, view.Mode);
        Assert.Null(view.Pane1!.GetVisualParent());
        Assert.Same(view, view.Pane2!.GetVisualParent());
        Assert.Equal(new Rect(0, 0, 400, 500), view.Pane2!.Bounds);
        // The hidden pane stays a logical child so its state survives.
        Assert.Same(view, view.Pane1!.Parent);
    }

    [AvaloniaFact]
    public void Vertical_Fold_Splits_Panes_Either_Side_Of_The_Fold()
    {
        var (_, view, _) = Create(1000, 700, FoldAt(DevicePosture.Flat, new Rect(600, 0, 20, 700), separating: false));
        view.Pane1Length = new GridLength(1, GridUnitType.Star);
        view.Pane2Length = new GridLength(3, GridUnitType.Star);
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 600, 700), view.Pane1!.Bounds);
        Assert.Equal(new Rect(620, 0, 380, 700), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Half_Opened_Splits_At_Fold_Even_Below_Min_Width()
    {
        var (_, view, _) = Create(500, 700, FoldAt(DevicePosture.HalfOpened, new Rect(250, 0, 0, 700), separating: true));
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 250, 700), view.Pane1!.Bounds);
        Assert.Equal(new Rect(250, 0, 250, 700), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Tabletop_Posture_Stacks_Panes_Above_And_Below_Fold()
    {
        var (_, view, _) = Create(900, 600, FoldAt(DevicePosture.HalfOpened, new Rect(0, 300, 900, 0), separating: true));
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Tall, view.Mode);
        Assert.Equal(new Rect(0, 0, 900, 300), view.Pane1!.Bounds);
        Assert.Equal(new Rect(0, 300, 900, 300), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Closed_Posture_Ignores_Fold()
    {
        var (_, view, _) = Create(400, 700, FoldAt(DevicePosture.Closed, new Rect(200, 0, 0, 700), separating: false));
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Tall, view.Mode);
    }

    [AvaloniaFact]
    public void Snap_To_Fold_Can_Be_Disabled_When_Flat()
    {
        var (_, view, _) = Create(1000, 700, FoldAt(DevicePosture.Flat, new Rect(600, 0, 0, 700), separating: false));
        view.SnapToFold = false;
        view.UpdateLayout();

        Assert.Equal(new Rect(0, 0, 500, 700), view.Pane1!.Bounds);
    }

    [AvaloniaFact]
    public void Right_Left_Configuration_Swaps_Panes()
    {
        var (_, view, _) = Create(1000, 700, FoldAt(DevicePosture.HalfOpened, new Rect(600, 0, 0, 700), separating: true));
        view.WideModeConfiguration = TwoPaneViewWideModeConfiguration.RightLeft;
        view.UpdateLayout();

        Assert.Equal(new Rect(600, 0, 400, 700), view.Pane1!.Bounds);
        Assert.Equal(new Rect(0, 0, 600, 700), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Fold_Changes_Update_Layout_And_Attached_Properties()
    {
        var (window, view, fold) = Create(500, 700);
        view.UpdateLayout();
        Assert.Equal(TwoPaneViewMode.Tall, view.Mode);

        fold.Current = FoldAt(DevicePosture.HalfOpened, new Rect(250, 0, 0, 700), separating: true);
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(DevicePosture.HalfOpened, Fold.GetPosture(view.Pane1!));
        Assert.Equal(FoldOrientation.Vertical, Fold.GetFoldOrientation(window));
        Assert.True(Fold.GetState(view).IsBook);
    }

    [AvaloniaFact]
    public void Fold_Is_Mapped_Into_Local_Coordinates()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var window = new Window
        {
            Width = 1000, Height = 700,
            Content = new DockPanel { Children = { new Border { Width = 200, [DockPanel.DockProperty] = Dock.Left }, view } },
        };
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(600, 0, 0, 700), separating: true)));
        window.Show();
        // The first pass discovers the view's offset in the window, the second applies it.
        view.UpdateLayout();
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 400, 700), view.Pane1!.Bounds);
        Assert.Equal(new Rect(400, 0, 400, 700), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Fold_Is_Mapped_Through_Scaling()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border(), Width = 800, Height = 600 };
        var window = new Window
        {
            Width = 400, Height = 300,
            Content = new LayoutTransformControl { LayoutTransform = new ScaleTransform(0.5, 0.5), Child = view },
        };
        // In the window the view is half size, so its fold at x=500 shows up at x=250.
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(250, 0, 20, 300), separating: true)));
        window.Show();
        view.UpdateLayout();
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 500, 600), view.Pane1!.Bounds);
        Assert.Equal(new Rect(540, 0, 260, 600), view.Pane2!.Bounds);
    }

    [AvaloniaFact]
    public void Swapping_Panes_Keeps_Both_As_Children()
    {
        var (_, view, _) = Create(1000, 500);
        var first = view.Pane1!;
        var second = view.Pane2!;

        (view.Pane1, view.Pane2) = (view.Pane2, view.Pane1);
        view.UpdateLayout();

        Assert.Same(view, first.Parent);
        Assert.Same(view, second.Parent);
        Assert.Same(view, first.GetVisualParent());
        Assert.Same(view, second.GetVisualParent());
        Assert.Equal(new Rect(0, 0, 500, 500), second.Bounds);
    }

    [AvaloniaFact]
    public void Unconstrained_Width_Settles_On_One_Mode()
    {
        var view = new TwoPaneView { Pane1 = new Border { Width = 900 }, Pane2 = new Border() };
        var window = new Window
        {
            Width = 1200, Height = 500,
            Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { view } },
        };
        Fold.SetProvider(window, new SimulatedFoldStateProvider());
        window.Show();
        view.UpdateLayout();

        var changes = 0;
        view.ModeChanged += (_, _) => changes++;
        view.InvalidateMeasure();
        view.UpdateLayout();
        view.InvalidateMeasure();
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public void Hinge_Angle_Alone_Does_Not_Invalidate_Layout()
    {
        var state = FoldAt(DevicePosture.HalfOpened, new Rect(480, 0, 40, 500), separating: true);
        var (_, view, fold) = Create(1000, 500, state);
        view.UpdateLayout();

        fold.Current = state with { HingeAngle = 95 };
        Assert.True(view.IsMeasureValid);

        fold.Current = state with { Posture = DevicePosture.Flat, IsSeparating = false };
        Assert.False(view.IsMeasureValid);
    }

    [AvaloniaFact]
    public void Mode_Changed_Fires_After_The_Panes_Are_Arranged()
    {
        var (_, view, fold) = Create(400, 700);
        view.MinTallModeHeight = double.PositiveInfinity;
        view.UpdateLayout();
        Assert.Equal(TwoPaneViewMode.SinglePane, view.Mode);

        Rect pane1 = default, pane2 = default;
        view.ModeChanged += (_, _) => (pane1, pane2) = (view.Pane1!.Bounds, view.Pane2!.Bounds);
        fold.Current = FoldAt(DevicePosture.HalfOpened, new Rect(190, 0, 20, 700), separating: true);
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
        Assert.Equal(new Rect(0, 0, 190, 700), pane1);
        Assert.Equal(new Rect(210, 0, 190, 700), pane2);
    }

    [AvaloniaFact]
    public void Fold_Outside_An_Offset_View_Never_Changes_The_Mode()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border(), MinWideModeWidth = 10000, MinTallModeHeight = 10000 };
        var changes = 0;
        view.ModeChanged += (_, _) => changes++;
        var window = new Window
        {
            Width = 1000, Height = 700,
            Content = new DockPanel { Children = { new Border { Width = 700, [DockPanel.DockProperty] = Dock.Left }, view } },
        };
        // The fold crosses the sidebar. Until the view's offset is known, it seems to cross the view instead.
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(100, 0, 20, 700), separating: true)));
        window.Show();
        view.UpdateLayout();
        view.UpdateLayout();

        Assert.Equal(TwoPaneViewMode.SinglePane, view.Mode);
        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public async Task Fold_Follows_An_Ancestor_Render_Transform_Once_It_Settles()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var slide = new TranslateTransform(300, 0);
        var host = new Border { Child = view, RenderTransform = slide };
        var window = new Window { Width = 1000, Height = 700, Content = host };
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(480, 0, 40, 700), separating: true)));
        window.Show();
        view.UpdateLayout();
        view.UpdateLayout();
        Assert.Equal(new Rect(0, 0, 180, 700), view.Pane1!.Bounds);

        // Moving a render transform doesn't run a layout pass on its own.
        slide.X = 0;
        await WaitFor(() => view.Pane1.Bounds.Width == 480);

        Assert.Equal(new Rect(0, 0, 480, 700), view.Pane1.Bounds);
        Assert.Equal(new Rect(520, 0, 480, 700), view.Pane2!.Bounds);

        host.RenderTransform = new TranslateTransform(100, 0);
        await WaitFor(() => view.Pane1.Bounds.Width == 380);

        Assert.Equal(new Rect(0, 0, 380, 700), view.Pane1.Bounds);
    }

    [AvaloniaFact]
    public async Task Animated_Ancestor_Transform_Lays_Out_Once_It_Stops()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var slide = new TranslateTransform(300, 0);
        var window = new Window { Width = 1000, Height = 700, Content = new Border { Child = view, RenderTransform = slide } };
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(480, 0, 40, 700), separating: true)));
        window.Show();
        view.UpdateLayout();
        view.UpdateLayout();
        var layouts = 0;
        view.Pane1!.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.BoundsProperty)
                layouts++;
        };

        // Move the transform on every frame, as a running animation does.
        const int Steps = 20;
        var step = 0;
        void Tick(TimeSpan time)
        {
            step++;
            slide.X = 300 - (300d * step / Steps);
            if (step < Steps)
                window.RequestAnimationFrame(Tick);
        }
        window.RequestAnimationFrame(Tick);
        await WaitFor(() => step == Steps);
        var whileMoving = layouts;
        await WaitFor(() => view.Pane1.Bounds.Width == 480);

        Assert.InRange(whileMoving, 0, 1);
        Assert.Equal(new Rect(0, 0, 480, 700), view.Pane1.Bounds);
    }

    [AvaloniaFact]
    public async Task Fold_Follows_A_Render_Transform_Origin()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var host = new Border { Child = view, RenderTransform = new ScaleTransform(0.5, 1), RenderTransformOrigin = RelativePoint.TopLeft };
        var window = new Window { Width = 1000, Height = 700, Content = host };
        // Scaled from the left edge the view covers window x 0 to 500, so the fold at 240 is at 480 in the view.
        Fold.SetProvider(window, new SimulatedFoldStateProvider(FoldAt(DevicePosture.HalfOpened, new Rect(240, 0, 20, 700), separating: true)));
        window.Show();
        view.UpdateLayout();
        view.UpdateLayout();
        Assert.Equal(new Rect(0, 0, 480, 700), view.Pane1!.Bounds);

        // Scaled from the right edge it covers 500 to 1000 instead: the fold misses it and the size rules split it.
        host.RenderTransformOrigin = new RelativePoint(1, 0, RelativeUnit.Relative);
        await WaitFor(() => view.Pane1.Bounds.Width == 500);

        Assert.Equal(new Rect(0, 0, 500, 700), view.Pane1.Bounds);
    }

    [AvaloniaFact]
    public void Detaching_Stops_Watching_The_Ancestor_Transform()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
        var slide = new TranslateTransform(300, 0);
        var host = new Border { Child = view, RenderTransform = slide };
        var window = new Window { Width = 1000, Height = 700, Content = host };
        window.Show();
        Assert.Equal(1, Watchers(slide, view));

        host.Child = null;
        Assert.Equal(0, Watchers(slide, view));

        host.Child = view;
        Assert.Equal(1, Watchers(slide, view));

        host.RenderTransform = new TranslateTransform(100, 0);
        Assert.Equal(0, Watchers(slide, view));
    }

    [AvaloniaFact]
    public void Mode_Is_Published_Without_A_Top_Level()
    {
        var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };

        view.Measure(new Size(1000, 500));
        view.Arrange(new Rect(0, 0, 1000, 500));

        Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
    }
}
