using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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

        window.Width = 642;
        view.UpdateLayout();

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
}
