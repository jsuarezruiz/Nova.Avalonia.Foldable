using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nova.Avalonia.Foldable.Sample;

/// <summary>
/// A TwoPaneView with a live readout of the fold state; changes are logged as FOLDDIAG.
/// </summary>
public sealed class SampleView : UserControl
{
    private static readonly FontFamily Mono = new("Menlo, Courier New, monospace");

    private readonly TwoPaneView _panes;
    private readonly Canvas _overlay = new() { IsHitTestVisible = false };
    private readonly Border _fold = new()
    {
        Background = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0x3B, 0x30)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30)),
        BorderThickness = new Thickness(1),
    };
    private readonly TextBlock _readout1 = Readout();
    private readonly TextBlock _readout2 = Readout();
    private readonly TextBlock _log = Readout();
    private readonly Queue<string> _events = new();
    private string _last = string.Empty;

    public SampleView()
    {
        Fold.SetIsEnabled(this, true);

        var priority = new Button { Content = "Priority: Pane1" };
        var snap = new Button { Content = "SnapToFold: on" };

        _panes = new TwoPaneView
        {
            MinWideModeWidth = 600,
            MinTallModeHeight = 100000,
            Pane1 = Pane("PANE 1", Color.FromRgb(0x0A, 0x84, 0xFF), _readout1, priority, snap),
            Pane2 = Pane("PANE 2", Color.FromRgb(0x30, 0xD1, 0x58), _readout2, _log),
        };

        priority.Click += (_, _) =>
        {
            _panes.PanePriority = _panes.PanePriority == TwoPaneViewPriority.Pane1
                ? TwoPaneViewPriority.Pane2
                : TwoPaneViewPriority.Pane1;
            priority.Content = $"Priority: {_panes.PanePriority}";
        };
        snap.Click += (_, _) =>
        {
            _panes.SnapToFold = !_panes.SnapToFold;
            snap.Content = $"SnapToFold: {(_panes.SnapToFold ? "on" : "off")}";
        };

        _overlay.Children.Add(_fold);
        Content = new Panel
        {
            Background = Brushes.Black,
            Children = { _panes, _overlay },
        };

        _panes.ModeChanged += (_, _) => Update("mode");
        LayoutUpdated += (_, _) => Update("layout");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == Fold.StateProperty && change.NewValue is FoldState state)
        {
            // Phones shouldn't stack the panes just for being tall, but a horizontal fold needs a reachable
            // MinTallModeHeight for SnapToFold to split at it on a flat device.
            _panes.MinTallModeHeight = state.Orientation == FoldOrientation.Horizontal ? 641 : 100000;

            _events.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {state.Posture} {Angle(state)} sep={state.IsSeparating} fold={FormatRect(state.FoldBounds)}");
            while (_events.Count > 12)
                _events.Dequeue();
            _log.Text = "HINGE UPDATES\n" + string.Join('\n', _events);
            Update("fold");
        }
    }

    private void Update(string why)
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var state = Fold.GetState(this);
        var safe = topLevel.InsetsManager?.SafeAreaPadding ?? default;

        var lines = new List<string>
        {
            $"Posture       {state.Posture}",
            $"HingeAngle    {Angle(state)}",
            $"IsFoldable    {state.IsFoldable}",
            $"IsSeparating  {state.IsSeparating}",
            $"Orientation   {state.Orientation}  book={state.IsBook} tabletop={state.IsTabletop}",
            $"FoldBounds    {FormatRect(state.FoldBounds)}",
            "",
            $"TopLevel      {FormatSize(topLevel.ClientSize)}  scale={topLevel.RenderScaling:0.##}",
            $"SafeArea      {safe}",
            $"View          {FormatRect(RectIn(this, topLevel))}",
            $"TwoPaneView   {FormatRect(RectIn(_panes, topLevel))}",
            $"Mode          {_panes.Mode}  priority={_panes.PanePriority}  snap={_panes.SnapToFold}",
            $"Pane1         {FormatRect(RectIn(_panes.Pane1, topLevel))}",
            $"Pane2         {FormatRect(RectIn(_panes.Pane2, topLevel))}",
        };
        var text = string.Join('\n', lines);

        // Zero-width folds get 2pt so they show.
        if (state.FoldBounds is { } fold && topLevel.TranslatePoint(fold.TopLeft, _overlay) is { } origin)
        {
            _fold.IsVisible = true;
            Canvas.SetLeft(_fold, origin.X);
            Canvas.SetTop(_fold, origin.Y);
            _fold.Width = Math.Max(2, fold.Width);
            _fold.Height = Math.Max(2, fold.Height);
        }
        else
        {
            _fold.IsVisible = false;
        }

        if (text == _last)
            return;
        _last = text;
        _readout1.Text = text;
        _readout2.Text = text;
        Console.WriteLine($"FOLDDIAG [{why}]\n{text}");
    }

    // Null for a pane TwoPaneView has hidden.
    private static Rect? RectIn(Visual? visual, TopLevel topLevel)
    {
        if (visual is null || !visual.IsEffectivelyVisible)
            return null;
        return visual.TranslatePoint(default, topLevel) is { } p ? new Rect(p, visual.Bounds.Size) : null;
    }

    private static string Angle(FoldState state) =>
        state.HingeAngle is { } a ? a.ToString("0.0", CultureInfo.InvariantCulture) + "°" : "null";

    private static string FormatRect(Rect? r) => r is { } v
        ? string.Create(CultureInfo.InvariantCulture, $"x={v.X:0.#} y={v.Y:0.#} w={v.Width:0.#} h={v.Height:0.#}")
        : "null (hidden / none)";

    private static string FormatSize(Size s) => string.Create(CultureInfo.InvariantCulture, $"{s.Width:0.#}×{s.Height:0.#}");

    private static TextBlock Readout() => new()
    {
        FontFamily = Mono,
        FontSize = 11,
        Foreground = Brushes.White,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 8, 0, 0),
    };

    private static Control Pane(string title, Color color, params Control[] content)
    {
        var stack = new StackPanel { Spacing = 6, Margin = new Thickness(16) };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(color),
        });
        foreach (var control in content)
        {
            if (control is Button button)
                button.HorizontalAlignment = HorizontalAlignment.Left;
            stack.Children.Add(control);
        }

        return new Border
        {
            Background = new SolidColorBrush(color, 0.18),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(2),
            Child = new ScrollViewer { Content = stack },
        };
    }
}
