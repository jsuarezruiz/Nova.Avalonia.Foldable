using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Nova.Avalonia.Foldable.Showcase.Animation;
using Nova.Avalonia.Foldable.Showcase.Views;

namespace Nova.Avalonia.Foldable.Showcase.Desktop;

/// <summary>
/// Hosts the app in a mock foldable driven by a <see cref="SimulatedFoldStateProvider"/>.
/// </summary>
public partial class SimulatorWindow : Window
{
    private enum FoldAxis { None, Vertical, Horizontal }

    private sealed record Preset(string Title, string Hint, Size Screen, bool Foldable, FoldAxis Axis, DevicePosture Posture, double Angle);

    private const int ClosedIndex = 1;
    private const int OpenIndex = 2;
    private const int BookIndex = 3;
    private const int OpenRotatedIndex = 4;
    private const int TabletopIndex = 5;

    // Screen sizes in points, as the iPhone Duo simulator reports them: a 951x669 inner display and a 466x678 cover.
    private static readonly Preset[] Presets =
    [
        new("Regular phone", "No hinge", new Size(390, 844), false, FoldAxis.None, DevicePosture.Unknown, 0),
        new("Closed", "Cover display", new Size(466, 678), true, FoldAxis.None, DevicePosture.Closed, 0),
        new("Open", "Flat, vertical fold", new Size(951, 669), true, FoldAxis.Vertical, DevicePosture.Flat, 180),
        new("Book", "Half open, vertical fold", new Size(951, 669), true, FoldAxis.Vertical, DevicePosture.HalfOpened, 110),
        new("Open, rotated", "Flat, horizontal fold", new Size(669, 951), true, FoldAxis.Horizontal, DevicePosture.Flat, 180),
        new("Tabletop", "Half open, horizontal fold", new Size(669, 951), true, FoldAxis.Horizontal, DevicePosture.HalfOpened, 100),
    ];

    private static readonly IBrush VerticalFoldShade = Shade(new RelativePoint(0, 0.5, RelativeUnit.Relative), new RelativePoint(1, 0.5, RelativeUnit.Relative));
    private static readonly IBrush HorizontalFoldShade = Shade(new RelativePoint(0.5, 0, RelativeUnit.Relative), new RelativePoint(0.5, 1, RelativeUnit.Relative));

    private readonly SimulatedFoldStateProvider _fold = new();
    private readonly MainView _view;
    private readonly FrameTicker _play;
    private readonly TextBlock[] _readout = new TextBlock[6];
    private int _index = OpenIndex;
    private Preset _preset = Presets[OpenIndex];
    private bool _rotated;
    private bool _checking;
    private bool _updatingSlider;
    private Preset? _publishedPreset;
    private Matrix? _publishedTransform;

    public SimulatorWindow() : this(new MainView())
    {
    }

    public SimulatorWindow(MainView view)
    {
        InitializeComponent();

        _view = view;
        _play = new FrameTicker(this, OnPlayFrame);
        Screen.Content = view;
        Fold.SetProvider(this, _fold);

        for (var i = 0; i < Presets.Length; i++)
        {
            var index = i;
            var button = new RadioButton
            {
                GroupName = "Presets",
                IsChecked = index == _index,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = Presets[index].Title, Classes = { "presetTitle" } },
                        new TextBlock { Text = Presets[index].Hint, Classes = { "presetHint" } },
                    },
                },
            };
            button.Classes.Add("preset");
            // The content is a panel, so screen readers get the title to announce.
            AutomationProperties.SetName(button, Presets[index].Title);
            // A click, even on the posture already picked, takes over from "Open the device".
            button.Click += (_, _) => _play.Stop();
            button.IsCheckedChanged += (_, _) =>
            {
                if (button.IsChecked == true && !_checking)
                    Apply(index);
            };
            PresetList.Children.Add(button);
        }

        for (var i = 0; i < _readout.Length; i++)
        {
            _readout[i] = new TextBlock { Classes = { "readout" } };
            Readout.Children.Add(_readout[i]);
        }

        AngleSlider.ValueChanged += (_, e) =>
        {
            if (_updatingSlider)
                return;
            _play.Stop();
            MoveHinge(e.NewValue);
        };

        // Tunnel, so the shortcuts work whichever control has focus.
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);

        // After layout, once the device has its final position in the window.
        LayoutUpdated += (_, _) => Publish();

        Apply(_index);
    }

    private static LinearGradientBrush Shade(RelativePoint start, RelativePoint end) => new()
    {
        StartPoint = start,
        EndPoint = end,
        GradientStops =
        {
            new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.25),
            new GradientStop(Color.FromArgb(0xB0, 0, 0, 0), 0.5),
            new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.75),
        },
    };

    // An angle is passed when the hinge itself picked the preset, and kept instead of the preset's own.
    private void Apply(int index, double? angle = null)
    {
        _index = index;
        var preset = Presets[index];
        if (preset.Axis != FoldAxis.None)
            _rotated = preset.Axis == FoldAxis.Horizontal;

        _preset = angle is { } a ? preset with { Angle = a } : preset;
        Screen.Width = preset.Screen.Width;
        Screen.Height = preset.Screen.Height;
        SetSlider(_preset.Angle);
        AngleSlider.IsEnabled = preset.Foldable;
        PlayButton.IsEnabled = preset.Foldable;
        if (!preset.Foldable)
            _play.Stop();
        Publish();
    }

    // The hinge decides the posture, as on the device: shut at 0°, flat at 180°, half open in between.
    private void MoveHinge(double angle)
    {
        if (!_preset.Foldable)
            return;

        var index = angle <= 0 ? ClosedIndex
            : angle >= 180 ? (_rotated ? OpenRotatedIndex : OpenIndex)
            : (_rotated ? TabletopIndex : BookIndex);

        if (index == _index)
        {
            _preset = _preset with { Angle = angle };
            SetSlider(angle);
            Publish();
            return;
        }

        Check(index, angle);
    }

    // 1 to 6 pick a posture; left and right move the hinge by 5°, or 1° with Shift.
    private void OnShortcut(object? sender, KeyEventArgs e)
    {
        // Leave system shortcuts alone.
        if ((e.KeyModifiers & ~KeyModifiers.Shift) != KeyModifiers.None)
            return;

        var preset = e.Key switch
        {
            >= Key.D1 and <= Key.D6 => e.Key - Key.D1,
            >= Key.NumPad1 and <= Key.NumPad6 => e.Key - Key.NumPad1,
            _ => -1,
        };

        if (preset >= 0)
        {
            _play.Stop();
            Check(preset);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Right && _preset.Foldable)
        {
            var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 1 : 5;
            _play.Stop();
            MoveHinge(Math.Clamp(_preset.Angle + (e.Key == Key.Right ? step : -step), 0, 180));
            e.Handled = true;
        }
    }

    private void SetSlider(double angle)
    {
        _updatingSlider = true;
        AngleSlider.Value = angle;
        _updatingSlider = false;
    }

    // Publishes the fold in window (top-level) coordinates, like the platform providers.
    private void Publish()
    {
        // Runs after every layout pass; only the preset and the device's place in the window matter.
        var toWindow = _view.TransformToVisual(this);
        if (_preset == _publishedPreset && toWindow == _publishedTransform)
            return;
        _publishedPreset = _preset;
        _publishedTransform = toWindow;

        // The iPhone Duo simulator reports the fold as a 40pt band, not a line.
        const double foldThickness = 40;
        var size = _preset.Screen;
        var vertical = _preset.Axis == FoldAxis.Vertical;
        Rect? local = _preset.Axis switch
        {
            FoldAxis.Vertical => new Rect((size.Width - foldThickness) / 2, 0, foldThickness, size.Height),
            FoldAxis.Horizontal => new Rect(0, (size.Height - foldThickness) / 2, size.Width, foldThickness),
            _ => null,
        };

        // Through the view's transform, so the fold stays right when the device is scaled to fit.
        var bounds = local is { } l && toWindow is { } transform ? l.TransformToAABB(transform) : (Rect?)null;
        var state = _preset.Foldable
            ? new FoldState(true, _preset.Posture, _preset.Angle, bounds, _preset.Posture == DevicePosture.HalfOpened)
            : FoldState.None;

        FoldLine.IsVisible = FoldBand.IsVisible = local is not null;
        // The dashed line runs down the middle of the fold band.
        if (local is { } band)
        {
            Canvas.SetLeft(FoldBand, band.X);
            Canvas.SetTop(FoldBand, band.Y);
            FoldBand.Width = band.Width;
            FoldBand.Height = band.Height;
            FoldLine.StartPoint = vertical ? band.Center.WithY(band.Top) : band.Center.WithX(band.Left);
            FoldLine.EndPoint = vertical ? band.Center.WithY(band.Bottom) : band.Center.WithX(band.Right);
        }

        HingeShade.Fill = vertical ? VerticalFoldShade : HorizontalFoldShade;
        HingeShade.Opacity = _preset.Posture == DevicePosture.HalfOpened ? (180 - _preset.Angle) / 180 : 0;

        AngleText.Text = _preset.Foldable ? $"{_preset.Angle:0}°" : "–";

        _fold.Current = state;
        UpdateReadout(state);
    }

    private void UpdateReadout(FoldState state)
    {
        _readout[0].Text = $"IsFoldable      {state.IsFoldable}";
        _readout[1].Text = $"Posture         {state.Posture}";
        _readout[2].Text = $"HingeAngle      {(state.HingeAngle is { } a ? $"{a:0}°" : "null")}";
        _readout[3].Text = $"IsSeparating    {state.IsSeparating}";
        _readout[4].Text = $"Orientation     {state.Orientation}";
        _readout[5].Text = $"FoldBounds      {(state.FoldBounds is { } f ? $"{f.X:0},{f.Y:0} {f.Width:0}×{f.Height:0}" : "null")}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _play.Stop();
        base.OnClosed(e);
    }

    // "Open the device": a beat on the cover display, then the hinge swings open to flat.
    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        Check(ClosedIndex);
        _play.Start();
    }

    private void OnPlayFrame(TimeSpan elapsed)
    {
        var t = elapsed.TotalSeconds;
        if (t < 0.8)
            return;

        var progress = Math.Clamp((t - 0.8) / 1.6, 0, 1);
        MoveHinge(180 * (1 - Math.Pow(1 - progress, 3)));

        if (progress >= 1)
            _play.Stop();
    }

    // Selects the preset's button and applies it once, whether or not the button was already selected.
    private void Check(int index, double? angle = null)
    {
        if (PresetList.Children[index] is not RadioButton button)
            return;

        _checking = true;
        button.IsChecked = true;
        _checking = false;
        Apply(index, angle);
    }
}
