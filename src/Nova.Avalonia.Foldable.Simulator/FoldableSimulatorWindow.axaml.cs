using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nova.Avalonia.Foldable.Simulator;

/// <summary>
/// A desktop window that runs an app inside a simulated iPhone Duo. It feeds the fold to the app through a
/// <see cref="SimulatedFoldStateProvider"/>, draws the device folding in 3D, and animates between postures.
/// </summary>
/// <remarks>
/// The hinge moves from the posture buttons, the slider, the keyboard, or by dragging the device itself.
/// Every step of a move reaches the app as a hinge update, as it would from a real hinge.
/// </remarks>
public partial class FoldableSimulatorWindow : Window
{
    private static readonly TimeSpan RotateDuration = TimeSpan.FromMilliseconds(450);
    private static readonly TimeSpan CoverPause = TimeSpan.FromMilliseconds(800);

    // Released this close to shut or flat, the hinge settles the rest of the way.
    private const double SnapAngle = 6;

    private readonly SimulatedFoldStateProvider _fold = new();
    private readonly Control _view;
    private readonly TextBlock[] _readout = new TextBlock[6];
    private int _index = DevicePreset.OpenIndex;
    private DevicePreset _preset = DevicePreset.All[DevicePreset.OpenIndex];
    private bool _rotated;
    private bool _checking;
    private bool _updatingSlider;
    private bool _rotating;
    private DevicePreset? _publishedPreset;
    private Matrix? _publishedTransform;
    private CancellationTokenSource? _motion;

    /// <summary>
    /// Creates an empty simulator, for the previewer.
    /// </summary>
    public FoldableSimulatorWindow()
        : this(new TextBlock { Text = "Your app here", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center })
    {
    }

    /// <summary>
    /// Creates a simulator that runs <paramref name="view"/>, the app's root view.
    /// </summary>
    /// <param name="view">The app's root view. It's laid out at the size of the screen it's on.</param>
    /// <param name="appName">The app's name, shown in the window title.</param>
    public FoldableSimulatorWindow(Control view, string? appName = null)
    {
        InitializeComponent();

        _view = view;
        Device.Screen = view;
        if (appName is not null)
            Title = $"{appName} · Foldable simulator";

        Fold.SetProvider(this, _fold);

        for (var i = 0; i < DevicePreset.All.Length; i++)
        {
            var index = i;
            var preset = DevicePreset.All[index];
            var button = new RadioButton
            {
                GroupName = "Presets",
                // The simulator is the hardware: picking a posture mustn't take the app's focus.
                Focusable = false,
                IsChecked = index == _index,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = preset.Title, Classes = { "sim-preset-title" } },
                        new TextBlock { Text = preset.Hint, Classes = { "sim-preset-hint" } },
                    },
                },
            };
            button.Classes.Add("sim-preset");
            // The content is a panel, so screen readers get the title to announce.
            AutomationProperties.SetName(button, preset.Title);
            button.IsCheckedChanged += (_, _) =>
            {
                if (button.IsChecked == true && !_checking)
                    GoTo(index);
            };
            PresetList.Children.Add(button);
        }

        for (var i = 0; i < _readout.Length; i++)
        {
            _readout[i] = new TextBlock { Classes = { "sim-readout" } };
            Readout.Children.Add(_readout[i]);
        }

        AngleSlider.ValueChanged += (_, e) =>
        {
            if (_updatingSlider)
                return;
            StopMotion();
            MoveHinge(e.NewValue);
        };

        Device.HingeDragged += angle =>
        {
            StopMotion();
            // Only the cover's half closes over the other; turned sideways, the device can't close at all.
            MoveHinge(_rotated || Device.MovingSide != FoldSide.First ? Math.Max(angle, 1) : angle);
        };
        Device.HingeReleased += OnHingeReleased;

        PlayButton.Click += OnPlayClicked;

        // Tunnel, so the shortcuts work whichever control has focus.
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);

        // After layout, once the device has its final position in the window.
        LayoutUpdated += (_, _) => Publish();

        Apply(_index);
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        StopMotion();
        base.OnClosed(e);
    }

    // An angle is passed when the hinge itself picked the preset, and kept instead of the preset's own.
    private void Apply(int index, double? angle = null)
    {
        _index = index;
        var preset = DevicePreset.All[index];
        if (preset.Axis != FoldAxis.None)
            _rotated = preset.IsRotated;

        _preset = angle is { } a ? preset with { Angle = a } : preset;
        SetSlider(_preset.Angle);
        AngleSlider.IsEnabled = preset.Foldable;
        PlayButton.IsEnabled = preset.Foldable;
        if (!preset.Foldable)
            StopMotion();

        // However it closed, the device shows the cover, on the back of the first half; that's the half
        // that swings open again.
        if (preset.IsClosed)
            Device.MovingSide = FoldSide.First;

        Device.Show(_preset, _preset.Angle);
        Publish();
    }

    // The hinge decides the posture, as on the device: shut at 0°, flat at 180°, half open in between.
    private void MoveHinge(double angle)
    {
        if (!_preset.Foldable)
            return;

        var index = angle <= 0 ? DevicePreset.ClosedIndex
            : angle >= 180 ? (_rotated ? DevicePreset.OpenRotatedIndex : DevicePreset.OpenIndex)
            : (_rotated ? DevicePreset.TabletopIndex : DevicePreset.BookIndex);

        if (index == _index)
        {
            _preset = _preset with { Angle = angle };
            SetSlider(angle);
            Device.Show(_preset, angle);
            Publish();
            return;
        }

        Check(index, angle);
    }

    private void OnHingeReleased(double angle)
    {
        if (angle <= SnapAngle && !_rotated && Device.MovingSide == FoldSide.First)
            Animate(token => TurnHinge(0, token));
        else if (angle >= 180 - SnapAngle)
            Animate(token => TurnHinge(180, token));
    }

    // Moves to a posture the way the device would get there: unfolding before it turns sideways, and
    // folding the cover's half shut to close.
    private void GoTo(int index) => Animate(token => GoToAsync(index, token));

    private async Task GoToAsync(int index, CancellationToken token)
    {
        var target = DevicePreset.All[index];

        // No hinge on one side or the other: nothing to animate.
        if (!target.Foldable || !_preset.Foldable)
        {
            Check(index);
            return;
        }

        if (target.IsClosed)
        {
            if (_preset.IsClosed)
                return;

            if (_rotated)
            {
                await TurnHinge(180, token);
                await Rotate(false, token);
            }

            // The cover display is on the back of the first half, so that's the half that closes.
            if (Device.MovingSide != FoldSide.First)
            {
                await TurnHinge(180, token);
                Device.MovingSide = FoldSide.First;
            }

            await TurnHinge(0, token);
            return;
        }

        if (_preset.IsClosed)
        {
            Device.MovingSide = FoldSide.First;
            await TurnHinge(target.IsRotated ? 180 : target.Angle, token);
            if (!target.IsRotated)
                return;
        }

        if (target.IsRotated != _rotated)
        {
            await TurnHinge(180, token);
            await Rotate(target.IsRotated, token);
        }

        await TurnHinge(target.Angle, token);
    }

    // Swings the hinge to an angle, the app receiving every step on the way.
    private async Task TurnHinge(double to, CancellationToken token)
    {
        var from = _preset.IsClosed ? 0 : _preset.Angle;
        if (Math.Abs(to - from) < 0.5)
        {
            MoveHinge(to);
            return;
        }

        var duration = TimeSpan.FromMilliseconds(Math.Min(250 + (3.5 * Math.Abs(to - from)), 900));
        await Tween.Run(this, duration, t => MoveHinge(from + ((to - from) * t)), token);
    }

    // Turns the device a quarter turn, as Device Hub does, then lays the app out for the new orientation.
    // Counterclockwise to the horizontal fold, so the cover's half ends up at the bottom.
    private async Task Rotate(bool toRotated, CancellationToken token)
    {
        _rotating = true;
        try
        {
            var sign = toRotated ? -1 : 1;
            await Tween.Run(this, RotateDuration, t => Rotator.LayoutTransform = new RotateTransform(sign * 90 * t), token);
        }
        finally
        {
            Rotator.LayoutTransform = null;
            _rotating = false;
        }

        // The first half, the cover's, ends up at the bottom.
        Device.MovingSide = toRotated ? FoldSide.Second : FoldSide.First;
        Check(toRotated ? DevicePreset.OpenRotatedIndex : DevicePreset.OpenIndex);
    }

    // Runs one move at a time: a new one, or a hand on the slider or the device, stops the last.
    private async void Animate(Func<CancellationToken, Task> move)
    {
        StopMotion();
        var motion = _motion = new CancellationTokenSource();
        try
        {
            await move(motion.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_motion == motion)
                _motion = null;
            motion.Dispose();
        }
    }

    private void StopMotion() => _motion?.Cancel();

    // 1 to 6 pick a posture; left and right move the hinge by 5°, or 1° with Shift.
    private void OnShortcut(object? sender, KeyEventArgs e)
    {
        // Leave system shortcuts alone.
        if ((e.KeyModifiers & ~KeyModifiers.Shift) != KeyModifiers.None)
            return;

        // While the app takes text, digits and arrows are the text's, not the hinge's.
        if (FocusManager?.GetFocusedElement() is TextBox)
            return;

        var preset = e.Key switch
        {
            >= Key.D1 and <= Key.D6 => e.Key - Key.D1,
            >= Key.NumPad1 and <= Key.NumPad6 => e.Key - Key.NumPad1,
            _ => -1,
        };

        if (preset >= 0)
        {
            SelectButton(preset);
            GoTo(preset);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Right && _preset.Foldable)
        {
            var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 1 : 5;
            StopMotion();
            MoveHinge(Math.Clamp(_preset.Angle + (e.Key == Key.Right ? step : -step), 0, 180));
            e.Handled = true;
        }
    }

    private void SetSlider(double angle)
    {
        _updatingSlider = true;
        AngleSlider.Value = angle;
        _updatingSlider = false;
        AngleText.Text = _preset.Foldable ? $"{angle:0}°" : "–";
    }

    // Publishes the fold in window (top-level) coordinates, like the platform providers.
    private void Publish()
    {
        // Mid-turn the device is sideways in the window; the app gets the new orientation once it lands.
        if (_rotating)
            return;

        // Runs after every layout pass; only the preset and the device's place in the window matter.
        var toWindow = _view.TransformToVisual(this);
        if (_preset == _publishedPreset && toWindow == _publishedTransform)
            return;
        _publishedPreset = _preset;
        _publishedTransform = toWindow;

        var size = _preset.Screen;
        Rect? local = _preset.Axis switch
        {
            FoldAxis.Vertical => new Rect((size.Width - DevicePreset.FoldThickness) / 2, 0, DevicePreset.FoldThickness, size.Height),
            FoldAxis.Horizontal => new Rect(0, (size.Height - DevicePreset.FoldThickness) / 2, size.Width, DevicePreset.FoldThickness),
            _ => null,
        };

        // Through the view's transform, so the fold stays right when the device is scaled to fit.
        var bounds = local is { } l && toWindow is { } transform ? l.TransformToAABB(transform) : (Rect?)null;
        var state = _preset.Foldable
            ? new FoldState(true, _preset.Posture, _preset.Angle, bounds, _preset.Posture == DevicePosture.HalfOpened)
            : FoldState.None;

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

    // "Open the device": a beat on the cover display, then the hinge swings open to flat.
    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        Check(DevicePreset.ClosedIndex);
        Animate(async token =>
        {
            await Task.Delay(CoverPause, token);
            Device.MovingSide = FoldSide.First;
            await TurnHinge(180, token);
        });
    }

    // Selects the preset's button and applies it at once, whether or not the button was already selected.
    private void Check(int index, double? angle = null)
    {
        SelectButton(index);
        Apply(index, angle);
    }

    private void SelectButton(int index)
    {
        if (PresetList.Children[index] is not RadioButton button)
            return;

        _checking = true;
        button.IsChecked = true;
        _checking = false;
    }
}
