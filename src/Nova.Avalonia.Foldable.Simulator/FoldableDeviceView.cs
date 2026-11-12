using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nova.Avalonia.Foldable.Simulator;

/// <summary>
/// One half of the inner display: the left or top one is first.
/// </summary>
internal enum FoldSide
{
    First,
    Second,
}

/// <summary>
/// The iPhone Duo drawn around the app. Open, one half lies flat and holds the live app; the other turns
/// around the hinge in perspective, showing a live picture of its half of the app, or the back of the
/// device once it has turned past 90°. Dragging that half, or the grips on the device's edges, moves the
/// hinge; clicking it brings it to face the viewer.
/// </summary>
/// <remarks>
/// The shading is the simulator's, not iOS's: iOS draws the app unchanged at any angle. The turning half
/// darkens and softens toward its free edge as it turns from the light, the still half dims as the screens
/// close on each other, and the free edge shows the device's thickness, so a half seen edge-on doesn't vanish.
/// </remarks>
internal sealed class FoldableDeviceView : Canvas
{
    private const double Bezel = 8;
    private const double BodyRadius = 40;
    private const double ScreenRadius = BodyRadius - Bezel;

    // The cover display's corners on the hinge side are nearly square.
    private const double HingeRadius = 10;
    private const double CoverHingeRadius = 4;

    // The camera's distance, in points, for the perspective of the turning half: about two and a half
    // device widths, so the near edge grows enough to read as 3D without leaving the window.
    private const double Depth = 2400;

    // Each half's thickness, in points: what shows of the free edge as the half turns.
    private const double Thickness = 7;

    // Turned less than this the device counts as flat: the whole screen is live.
    private const double FlatTurn = 0.5;

    // The free edge is drawn only where it shows; nearly flat, it would only cover the bezel.
    private const double EdgeFrom = 12;

    // How much the turning screen darkens at its hinge and at its free edge, fully turned.
    private const double HingeDarkening = 0.12;
    private const double EdgeDarkening = 0.75;

    // The softening at the free edge, fully turned.
    private const double MaxBlur = 10;

    // How much the still screen dims as the device closes past 90°.
    private const double ClosingDarkening = 0.55;

    // A press that moves less than this is a click, not a drag.
    private const double ClickSlop = 4;

    private const double GripLength = 72;
    private const double GripReach = 24;

    // The cover display's camera, from its top-left corner, in points.
    private static readonly Point CoverCamera = new(418, 47);
    private const double CameraSize = 36;

    private static readonly TimeSpan SwapLeg = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan CoverWake = TimeSpan.FromMilliseconds(350);

    private static readonly IBrush BodyBrush = new SolidColorBrush(Color.Parse("#FF2A2A33"));
    private static readonly IBrush BackBrush = new SolidColorBrush(Color.Parse("#FF303039"));
    private static readonly IBrush EdgeBrush = new SolidColorBrush(Color.Parse("#FF4A4A55"));
    private static readonly IBrush GlassBrush = new SolidColorBrush(Color.Parse("#FF07070A"));
    private static readonly IBrush LensBrush = new SolidColorBrush(Color.Parse("#FF15151C"));
    private static readonly IBrush GripBrush = new SolidColorBrush(Color.Parse("#80D4A1FF"));
    private static readonly IBrush FoldBandBrush = new SolidColorBrush(Color.Parse("#14D4A1FF"));
    private static readonly IBrush CreaseBrush = new SolidColorBrush(Color.Parse("#26FFFFFF"));

    private readonly ContentPresenter _screen = new();
    private readonly Panel _screenHost = new() { ClipToBounds = false };
    private readonly Border _body = new() { Background = BodyBrush, BoxShadow = BoxShadows.Parse("0 30 80 0 #A0000000") };
    private readonly Border _stillShade = new() { Background = Brushes.Black, IsHitTestVisible = false };
    private readonly Border _foldBand = new() { Background = FoldBandBrush, IsHitTestVisible = false };
    private readonly Border _hingeShadow = new() { IsHitTestVisible = false };
    private readonly Border _crease = new() { Background = CreaseBrush, IsHitTestVisible = false };
    private readonly Canvas _turning = new() { IsHitTestVisible = false };
    private readonly Border _turningBody = new() { Background = BodyBrush, BoxShadow = BoxShadows.Parse("0 16 40 0 #50000000") };
    private readonly Border _turningScreen = new();
    private readonly Border _turningBlur = new();
    private readonly Border _turningShade = new();
    private readonly Canvas _turningBack = new();
    private readonly Border _backBody = new() { Background = BackBrush };
    private readonly Border _backGlass = new() { Background = GlassBrush };
    private readonly Border _backLens = new() { Background = LensBrush, CornerRadius = new CornerRadius(CameraSize / 2) };
    private readonly Border _backShade = new() { Background = Brushes.Black };
    private readonly Path _edge = new() { Fill = EdgeBrush, IsHitTestVisible = false };
    private readonly Border _grabArea = new() { Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand) };
    private readonly Border _firstGrip = Grip();
    private readonly Border _secondGrip = Grip();
    private readonly Rotate3DTransform _turn = new() { Depth = Depth };
    private readonly BlurEffect _blur = new();
    private readonly FrameTicker _mirror;

    private DevicePreset _preset = DevicePreset.All[DevicePreset.OpenIndex];
    private double _angle = 180;

    // Set while a click brings the other half to the front: the drawn hinge moves, the device's doesn't.
    private double? _drawnAngle;

    private FoldSide _movingSide = FoldSide.First;
    private FoldSide? _dragSide;
    private Point _dragStart;
    private double _dragStartCos;
    private bool _dragMoved;

    public FoldableDeviceView()
    {
        _screenHost.Children.Add(_screen);
        _turningBlur.Effect = _blur;
        _turning.RenderTransform = _turn;
        _turning.Children.Add(_turningBody);
        _turning.Children.Add(_turningScreen);
        _turning.Children.Add(_turningBlur);
        _turning.Children.Add(_turningShade);
        _turning.Children.Add(_turningBack);
        _turningBack.Children.Add(_backBody);
        _turningBack.Children.Add(_backGlass);
        _turningBack.Children.Add(_backLens);
        _turningBack.Children.Add(_backShade);

        Children.Add(_body);
        Children.Add(_screenHost);
        Children.Add(_stillShade);
        Children.Add(_foldBand);
        Children.Add(_hingeShadow);
        Children.Add(_crease);
        Children.Add(_turning);
        Children.Add(_edge);
        Children.Add(_grabArea);
        Children.Add(_firstGrip);
        Children.Add(_secondGrip);

        _mirror = new FrameTicker(this, _ => MirrorTurningHalf());

        Watch(_grabArea, () => _movingSide);
        Watch(_firstGrip, () => FoldSide.First);
        Watch(_secondGrip, () => FoldSide.Second);

        UpdateDevice();
    }

    /// <summary>
    /// Gets or sets the app shown on the device's screen.
    /// </summary>
    public object? Screen
    {
        get => _screen.Content;
        set => _screen.Content = value;
    }

    /// <summary>
    /// Gets or sets the half that turns when the device folds. The cover display is on the back of the
    /// first half, so that one closes over the other.
    /// </summary>
    public FoldSide MovingSide
    {
        get => _movingSide;
        set
        {
            if (_movingSide == value)
                return;
            _movingSide = value;
            UpdateDevice();
        }
    }

    /// <summary>
    /// Occurs while a drag moves the hinge, with the angle it moved to.
    /// </summary>
    public event Action<double>? HingeDragged;

    /// <summary>
    /// Occurs when a drag lets go of the hinge, with its last angle.
    /// </summary>
    public event Action<double>? HingeReleased;

    /// <summary>
    /// Shows the device in a posture, with its hinge at <paramref name="angle"/>.
    /// </summary>
    public void Show(DevicePreset preset, double angle)
    {
        var wasClosed = _preset.IsClosed;
        _preset = preset;
        _angle = angle;
        UpdateDevice();

        // The cover wakes once the device closes, rather than snapping from black to the app.
        if (preset.IsClosed && !wasClosed)
            WakeCover();
        else if (!preset.IsClosed)
            _screenHost.Opacity = 1;
    }

    private static Border Grip() => new()
    {
        Background = Brushes.Transparent,
        Child = new Border { Background = GripBrush, CornerRadius = new CornerRadius(2) },
    };

    // How far the moving half has turned from flat, in degrees.
    private double Turn => Math.Clamp(180d - (_drawnAngle ?? _angle), 0d, 180d);

    private bool IsVertical => _preset.Axis == FoldAxis.Vertical;

    private static double SmoothStep(double t)
    {
        t = Math.Clamp(t, 0d, 1d);
        return t * t * (3d - (2d * t));
    }

    private async void WakeCover()
    {
        _screenHost.Opacity = 0;
        try
        {
            await Tween.Run(this, CoverWake, t => _screenHost.Opacity = t);
        }
        finally
        {
            _screenHost.Opacity = 1;
        }
    }

    private void UpdateDevice()
    {
        if (!_preset.Foldable)
            ShowSlab();
        else if (_preset.IsClosed)
            ShowCover();
        else
            ShowInner();
    }

    // A phone without a hinge.
    private void ShowSlab()
    {
        var screen = _preset.Screen;
        Width = screen.Width + (2 * Bezel);
        Height = screen.Height + (2 * Bezel);

        Place(_body, new Rect(0, 0, Width, Height));
        _body.CornerRadius = new CornerRadius(BodyRadius);
        PlaceScreen(new Rect(Bezel, Bezel, screen.Width, screen.Height), new Rect(screen), new CornerRadius(ScreenRadius));

        HideFold();
        _firstGrip.IsVisible = _secondGrip.IsVisible = false;
    }

    // Closed: the device lies where the second half was, showing the cover display on the back of the
    // first half, with the hinge along its left edge. The grip on the free edge swings it open.
    private void ShowCover()
    {
        var inner = DevicePreset.InnerScreen;
        Width = inner.Width + (2 * Bezel);
        Height = inner.Height + (2 * Bezel);

        var hinge = Width / 2;
        var body = new Rect(hinge, 0, Width - hinge, Height);
        Place(_body, body);
        _body.CornerRadius = new CornerRadius(HingeRadius, BodyRadius, BodyRadius, HingeRadius);
        PlaceScreen(CoverGlass(), new Rect(DevicePreset.CoverScreen), new CornerRadius(CoverHingeRadius, ScreenRadius, ScreenRadius, CoverHingeRadius));

        HideFold();
        _secondGrip.IsVisible = false;
        _firstGrip.IsVisible = true;
        Place(_firstGrip, new Rect(body.Right - Bezel, body.Center.Y - (GripLength / 2), Bezel + GripReach, GripLength));
        PlaceGripMark(_firstGrip, vertical: true, outerFirst: false);
    }

    // The cover display's glass in this control's coordinates: on the second half's footprint, with the
    // usual bezel at the free edge and the rest of the width at the hinge.
    private Rect CoverGlass()
    {
        var cover = DevicePreset.CoverScreen;
        return new Rect(Width - Bezel - cover.Width, (Height - cover.Height) / 2, cover.Width, cover.Height);
    }

    private void HideFold()
    {
        _foldBand.IsVisible = _hingeShadow.IsVisible = _crease.IsVisible = _stillShade.IsVisible = false;
        _turning.IsVisible = _edge.IsVisible = _grabArea.IsVisible = false;
        _mirror.Stop();
    }

    private void ShowInner()
    {
        var screen = _preset.Screen;
        var vertical = IsVertical;
        Width = screen.Width + (2 * Bezel);
        Height = screen.Height + (2 * Bezel);

        var turn = Turn;
        var screenRect = new Rect(Bezel, Bezel, screen.Width, screen.Height);
        PlaceGrips(vertical);

        if (turn < FlatTurn)
        {
            Place(_body, new Rect(0, 0, Width, Height));
            _body.CornerRadius = new CornerRadius(BodyRadius);
            PlaceScreen(screenRect, new Rect(screen), new CornerRadius(ScreenRadius));

            HideFold();

            // The band TwoPaneView keeps both panes out of.
            _foldBand.IsVisible = true;
            Place(_foldBand, vertical
                ? new Rect(Bezel + ((screen.Width - DevicePreset.FoldThickness) / 2), Bezel, DevicePreset.FoldThickness, screen.Height)
                : new Rect(Bezel, Bezel + ((screen.Height - DevicePreset.FoldThickness) / 2), screen.Width, DevicePreset.FoldThickness));
            _firstGrip.IsVisible = _secondGrip.IsVisible = true;
            return;
        }

        var moving = _movingSide;
        var still = moving == FoldSide.First ? FoldSide.Second : FoldSide.First;
        var movingArea = Area(moving);

        Place(_body, Area(still));
        _body.CornerRadius = OuterCorners(still, BodyRadius, 0);
        var stillScreen = Half(still, new Rect(screen));
        PlaceScreen(screenRect, stillScreen, OuterCorners(still, ScreenRadius, 0));

        // Folded, the turned half itself is the handle.
        _foldBand.IsVisible = false;
        _firstGrip.IsVisible = _secondGrip.IsVisible = false;

        // The turning half, laid out flat in its own coordinates, then turned around its hinge edge.
        Place(_turning, movingArea);
        Place(_turningBody, new Rect(movingArea.Size));
        _turningBody.CornerRadius = OuterCorners(moving, BodyRadius, 0);
        var movingScreen = Half(moving, new Rect(screen));
        var local = movingScreen.Translate(new Vector(Bezel, Bezel) - movingArea.Position);
        foreach (var layer in new[] { _turningScreen, _turningBlur, _turningShade })
        {
            Place(layer, local);
            layer.CornerRadius = OuterCorners(moving, ScreenRadius, 0);
        }

        PlaceBack(moving, movingArea);

        // Inward fold: the screens face each other, so the turning half comes toward the viewer.
        _turning.RenderTransformOrigin = (vertical, moving) switch
        {
            (true, FoldSide.First) => new RelativePoint(1, 0.5, RelativeUnit.Relative),
            (true, FoldSide.Second) => new RelativePoint(0, 0.5, RelativeUnit.Relative),
            (false, FoldSide.First) => new RelativePoint(0.5, 1, RelativeUnit.Relative),
            _ => new RelativePoint(0.5, 0, RelativeUnit.Relative),
        };
        _turn.AngleY = vertical ? (moving == FoldSide.First ? turn : -turn) : 0;
        _turn.AngleX = vertical ? 0 : (moving == FoldSide.First ? -turn : turn);
        _turning.IsVisible = true;

        // Past 90° the half shows its back.
        var facing = turn <= 90d;
        _turningScreen.IsVisible = _turningBlur.IsVisible = _turningShade.IsVisible = facing;
        _turningBack.IsVisible = !facing;

        // Turning from the light: darker and softer toward the free edge, the further it turns. The back
        // brightens as it comes to face the viewer.
        var away = SmoothStep(turn / 90d);
        _turningShade.Background = Ramp(moving, Color.FromArgb((byte)(255 * HingeDarkening * away), 0, 0, 0), Color.FromArgb((byte)(255 * EdgeDarkening * away), 0, 0, 0), 0);
        _turningBlur.OpacityMask = Ramp(moving, Colors.Transparent, Colors.Black, 0.25);
        _blur.Radius = MaxBlur * away;
        _backShade.Opacity = 0.6d * (180d - turn) / 90d;

        // The still half falls into the turning half's shadow by the hinge, then dims as the screens close.
        var closing = Math.Clamp((turn - 90d) / 90d, 0d, 1d);
        _hingeShadow.IsVisible = true;
        PlaceHingeShadow(still, stillScreen, Math.Sin(Math.Min(turn, 90d) * Math.PI / 180d));
        _stillShade.IsVisible = closing > 0;
        Place(_stillShade, stillScreen.Translate(new Vector(Bezel, Bezel)));
        _stillShade.CornerRadius = OuterCorners(still, ScreenRadius, 0);
        _stillShade.Opacity = ClosingDarkening * Math.Pow(closing, 1.2);

        // Light catching the bent glass along the fold.
        _crease.IsVisible = turn < 175d;
        _crease.Opacity = Math.Sin(turn * Math.PI / 180d);
        Place(_crease, vertical
            ? new Rect((Width / 2) - 1, Bezel, 2, screen.Height)
            : new Rect(Bezel, (Height / 2) - 1, screen.Width, 2));

        PlaceEdge(moving, turn);

        _grabArea.IsVisible = true;
        Place(_grabArea, movingArea);

        if (facing)
        {
            MirrorTurningHalf();
            _mirror.Start();
        }
        else
        {
            _mirror.Stop();
        }
    }

    // Copies the moving half of the live app onto the turning screen and its softened copy. A brush draws
    // the visual once, so new ones each frame keep the picture live.
    private void MirrorTurningHalf()
    {
        if (!_turningScreen.IsVisible)
            return;

        var half = new RelativeRect(Half(_movingSide, new Rect(_preset.Screen)), RelativeUnit.Absolute);
        _turningScreen.Background = new VisualBrush(_screen) { SourceRect = half, Stretch = Stretch.Fill };
        _turningBlur.Background = new VisualBrush(_screen) { SourceRect = half, Stretch = Stretch.Fill };
    }

    // A gradient across the turning half, from its hinge edge to its free edge.
    private LinearGradientBrush Ramp(FoldSide moving, Color atHinge, Color atEdge, double from)
    {
        var (hinge, edge) = (IsVertical, moving) switch
        {
            (true, FoldSide.First) => (new RelativePoint(1, 0.5, RelativeUnit.Relative), new RelativePoint(0, 0.5, RelativeUnit.Relative)),
            (true, FoldSide.Second) => (new RelativePoint(0, 0.5, RelativeUnit.Relative), new RelativePoint(1, 0.5, RelativeUnit.Relative)),
            (false, FoldSide.First) => (new RelativePoint(0.5, 1, RelativeUnit.Relative), new RelativePoint(0.5, 0, RelativeUnit.Relative)),
            _ => (new RelativePoint(0.5, 0, RelativeUnit.Relative), new RelativePoint(0.5, 1, RelativeUnit.Relative)),
        };

        return new LinearGradientBrush
        {
            StartPoint = hinge,
            EndPoint = edge,
            GradientStops = { new GradientStop(atHinge, from), new GradientStop(atEdge, 1) },
        };
    }

    // The free edge's thickness, projected the way Rotate3DTransform projects the turning half: rotated
    // about the hinge toward the viewer, then divided by 1 - z/Depth around the hinge's middle. The band
    // lies on the side away from the face that shows.
    private void PlaceEdge(FoldSide moving, double turn)
    {
        if (turn < EdgeFrom || turn > 180d - EdgeFrom)
        {
            _edge.IsVisible = false;
            return;
        }

        var vertical = IsVertical;
        var reach = vertical ? Width / 2 : Height / 2;
        var span = (vertical ? Height : Width) / 2 - BodyRadius;
        var hinge = new Point(Width / 2, Height / 2);
        var outward = (vertical, moving) switch
        {
            (true, FoldSide.First) => new Vector(-1, 0),
            (true, FoldSide.Second) => new Vector(1, 0),
            (false, FoldSide.First) => new Vector(0, -1),
            _ => new Vector(0, 1),
        };
        var along = vertical ? new Vector(0, 1) : new Vector(1, 0);
        var behind = turn <= 90d ? Thickness : -Thickness;
        var a = turn * Math.PI / 180d;

        Point Project(double across, double offset, double depth)
        {
            var flat = (across * Math.Cos(a)) + (depth * Math.Sin(a));
            var z = (across * Math.Sin(a)) - (depth * Math.Cos(a));
            var k = 1d / (1d - (z / Depth));
            return hinge + (outward * flat * k) + (along * offset * k);
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(Project(reach, -span, 0), true);
            g.LineTo(Project(reach, -span, behind));
            g.LineTo(Project(reach, span, behind));
            g.LineTo(Project(reach, span, 0));
            g.EndFigure(true);
        }

        _edge.Data = geometry;
        _edge.IsVisible = true;
    }

    // The part of the device a half takes up, bezel included, in this control's coordinates.
    private Rect Area(FoldSide side)
    {
        var hinge = IsVertical ? Width / 2 : Height / 2;
        return (IsVertical, side) switch
        {
            (true, FoldSide.First) => new Rect(0, 0, hinge, Height),
            (true, FoldSide.Second) => new Rect(hinge, 0, Width - hinge, Height),
            (false, FoldSide.First) => new Rect(0, 0, Width, hinge),
            _ => new Rect(0, hinge, Width, Height - hinge),
        };
    }

    // One half of a rectangle split at the fold.
    private Rect Half(FoldSide side, Rect rect) => (IsVertical, side) switch
    {
        (true, FoldSide.First) => new Rect(rect.X, rect.Y, rect.Width / 2, rect.Height),
        (true, FoldSide.Second) => new Rect(rect.X + (rect.Width / 2), rect.Y, rect.Width / 2, rect.Height),
        (false, FoldSide.First) => new Rect(rect.X, rect.Y, rect.Width, rect.Height / 2),
        _ => new Rect(rect.X, rect.Y + (rect.Height / 2), rect.Width, rect.Height / 2),
    };

    // Rounds a half's corners: the outer ones by radius, the ones on the hinge side by hinge.
    private CornerRadius OuterCorners(FoldSide side, double radius, double hinge) => (IsVertical, side) switch
    {
        (true, FoldSide.First) => new CornerRadius(radius, hinge, hinge, radius),
        (true, FoldSide.Second) => new CornerRadius(hinge, radius, radius, hinge),
        (false, FoldSide.First) => new CornerRadius(radius, radius, hinge, hinge),
        _ => new CornerRadius(hinge, hinge, radius, radius),
    };

    // The app is laid out at the full screen size and clipped to the part that shows.
    private void PlaceScreen(Rect screen, Rect shown, CornerRadius corners)
    {
        _screen.Width = _preset.Screen.Width;
        _screen.Height = _preset.Screen.Height;
        Place(_screenHost, screen);
        _screenHost.Clip = RoundedRect(shown, corners);
    }

    // The device's back on the turning half, seen mirrored once the half turns past 90°. On the first half
    // of a vertical fold it carries the cover display, placed so it lands exactly where the closed device
    // shows it.
    private void PlaceBack(FoldSide moving, Rect area)
    {
        Place(_turningBack, new Rect(area.Size));
        Place(_backBody, new Rect(area.Size));
        Place(_backShade, new Rect(area.Size));
        _backBody.CornerRadius = _backShade.CornerRadius = OuterCorners(moving, BodyRadius, HingeRadius);

        var hasCover = IsVertical && moving == FoldSide.First;
        _backGlass.IsVisible = _backLens.IsVisible = hasCover;
        if (!hasCover)
            return;

        // Mirrored about the hinge: x on the closed device is Width - x here.
        var glass = CoverGlass();
        Place(_backGlass, new Rect(Width - glass.Right, glass.Y, glass.Width, glass.Height));
        _backGlass.CornerRadius = new CornerRadius(ScreenRadius, CoverHingeRadius, CoverHingeRadius, ScreenRadius);
        var lens = new Point(glass.X + CoverCamera.X, glass.Y + CoverCamera.Y);
        Place(_backLens, new Rect(Width - lens.X - (CameraSize / 2), lens.Y - (CameraSize / 2), CameraSize, CameraSize));
    }

    private void PlaceHingeShadow(FoldSide still, Rect stillScreen, double strength)
    {
        var shadow = Color.FromArgb((byte)(150 * strength), 0, 0, 0);
        var clear = Color.FromArgb(0, 0, 0, 0);
        var depth = 0.35;
        Rect rect;
        RelativePoint start, end;
        switch (IsVertical, still)
        {
            case (true, FoldSide.Second):
                rect = new Rect(stillScreen.X, stillScreen.Y, stillScreen.Width * depth, stillScreen.Height);
                start = new RelativePoint(0, 0.5, RelativeUnit.Relative);
                end = new RelativePoint(1, 0.5, RelativeUnit.Relative);
                break;
            case (true, FoldSide.First):
                rect = new Rect(stillScreen.Right - (stillScreen.Width * depth), stillScreen.Y, stillScreen.Width * depth, stillScreen.Height);
                start = new RelativePoint(1, 0.5, RelativeUnit.Relative);
                end = new RelativePoint(0, 0.5, RelativeUnit.Relative);
                break;
            case (false, FoldSide.Second):
                rect = new Rect(stillScreen.X, stillScreen.Y, stillScreen.Width, stillScreen.Height * depth);
                start = new RelativePoint(0.5, 0, RelativeUnit.Relative);
                end = new RelativePoint(0.5, 1, RelativeUnit.Relative);
                break;
            default:
                rect = new Rect(stillScreen.X, stillScreen.Bottom - (stillScreen.Height * depth), stillScreen.Width, stillScreen.Height * depth);
                start = new RelativePoint(0.5, 1, RelativeUnit.Relative);
                end = new RelativePoint(0.5, 0, RelativeUnit.Relative);
                break;
        }

        Place(_hingeShadow, rect.Translate(new Vector(Bezel, Bezel)));
        _hingeShadow.Background = new LinearGradientBrush
        {
            StartPoint = start,
            EndPoint = end,
            GradientStops = { new GradientStop(shadow, 0), new GradientStop(clear, 1) },
        };
    }

    private void PlaceGrips(bool vertical)
    {
        if (vertical)
        {
            Place(_firstGrip, new Rect(-GripReach, (Height - GripLength) / 2, GripReach + Bezel, GripLength));
            Place(_secondGrip, new Rect(Width - Bezel, (Height - GripLength) / 2, GripReach + Bezel, GripLength));
        }
        else
        {
            Place(_firstGrip, new Rect((Width - GripLength) / 2, -GripReach, GripLength, GripReach + Bezel));
            Place(_secondGrip, new Rect((Width - GripLength) / 2, Height - Bezel, GripLength, GripReach + Bezel));
        }

        PlaceGripMark(_firstGrip, vertical, outerFirst: true);
        PlaceGripMark(_secondGrip, vertical, outerFirst: false);
    }

    // The visible mark sits in the bezel; the rest of the grip reaches past the edge, easier to catch.
    private static void PlaceGripMark(Border grip, bool vertical, bool outerFirst)
    {
        grip.Cursor = new Cursor(vertical ? StandardCursorType.SizeWestEast : StandardCursorType.SizeNorthSouth);
        if (grip.Child is not Border mark)
            return;

        mark.Width = vertical ? 4 : GripLength - 16;
        mark.Height = vertical ? GripLength - 16 : 4;
        mark.HorizontalAlignment = vertical ? (outerFirst ? HorizontalAlignment.Right : HorizontalAlignment.Left) : HorizontalAlignment.Center;
        mark.VerticalAlignment = vertical ? VerticalAlignment.Center : (outerFirst ? VerticalAlignment.Bottom : VerticalAlignment.Top);
        mark.Margin = vertical ? new Thickness(outerFirst ? 0 : 2, 0, outerFirst ? 2 : 0, 0) : new Thickness(0, outerFirst ? 0 : 2, 0, outerFirst ? 2 : 0);
    }

    private static void Place(Control control, Rect rect)
    {
        SetLeft(control, rect.X);
        SetTop(control, rect.Y);
        control.Width = rect.Width;
        control.Height = rect.Height;
    }

    private static StreamGeometry RoundedRect(Rect rect, CornerRadius corners)
    {
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        g.BeginFigure(new Point(rect.Left + corners.TopLeft, rect.Top), true);
        g.LineTo(new Point(rect.Right - corners.TopRight, rect.Top));
        Corner(g, new Point(rect.Right, rect.Top + corners.TopRight), corners.TopRight);
        g.LineTo(new Point(rect.Right, rect.Bottom - corners.BottomRight));
        Corner(g, new Point(rect.Right - corners.BottomRight, rect.Bottom), corners.BottomRight);
        g.LineTo(new Point(rect.Left + corners.BottomLeft, rect.Bottom));
        Corner(g, new Point(rect.Left, rect.Bottom - corners.BottomLeft), corners.BottomLeft);
        g.LineTo(new Point(rect.Left, rect.Top + corners.TopLeft));
        Corner(g, new Point(rect.Left + corners.TopLeft, rect.Top), corners.TopLeft);
        g.EndFigure(true);
        return geometry;
    }

    private static void Corner(StreamGeometryContext g, Point to, double radius)
    {
        if (radius > 0)
            g.ArcTo(to, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
    }

    // Grabbing a half: a drag folds it, the hinge following the pointer; a click on the turning half
    // brings it to the front.
    private void Watch(Control control, Func<FoldSide> side)
    {
        control.PointerPressed += (_, e) =>
        {
            if (!_preset.Foldable || _drawnAngle is not null)
                return;

            // Closed, only the cover's half can swing open; open and flat, whichever half is grabbed folds.
            var grabbed = side();
            if (_preset.IsClosed)
                MovingSide = FoldSide.First;
            else if (Turn < FlatTurn)
                MovingSide = grabbed;
            else if (grabbed != _movingSide)
                return;

            _dragSide = _movingSide;
            _dragStart = e.GetPosition(this);
            _dragStartCos = Math.Cos(Turn * Math.PI / 180d);
            _dragMoved = false;
            e.Pointer.Capture(control);
            e.Handled = true;
        };

        control.PointerMoved += (_, e) =>
        {
            if (_dragSide is not { } dragging)
                return;

            var delta = e.GetPosition(this) - _dragStart;
            if (!_dragMoved && Math.Abs(delta.X) + Math.Abs(delta.Y) < ClickSlop)
                return;
            _dragMoved = true;

            // The free edge sits at half·cos(turn) from the hinge; keep it under the pointer.
            var vertical = _preset.IsClosed || IsVertical;
            var half = vertical ? Width / 2 : Height / 2;
            var along = vertical ? delta.X : delta.Y;
            var cos = _dragStartCos + ((dragging == FoldSide.First ? -along : along) / half);
            var turn = Math.Acos(Math.Clamp(cos, -1d, 1d)) * 180d / Math.PI;
            HingeDragged?.Invoke(180d - turn);
            e.Handled = true;
        };

        control.PointerReleased += (_, e) =>
        {
            if (_dragSide is null)
                return;

            _dragSide = null;
            e.Pointer.Capture(null);
            e.Handled = true;

            if (_dragMoved)
                HingeReleased?.Invoke(_angle);
            else if (control == _grabArea)
                BringToFront();
        };

        control.PointerCaptureLost += (_, _) => _dragSide = null;
    }

    // Swaps which half faces the viewer: the turning half opens flat, the other folds the same way. Only
    // the drawing moves; the hinge the app sees doesn't.
    private async void BringToFront()
    {
        var angle = _angle;
        var turn = 180d - angle;
        try
        {
            await Tween.Run(this, SwapLeg, t =>
            {
                _drawnAngle = angle + (turn * t);
                UpdateDevice();
            });
            _movingSide = _movingSide == FoldSide.First ? FoldSide.Second : FoldSide.First;
            await Tween.Run(this, SwapLeg, t =>
            {
                _drawnAngle = 180d - (turn * t);
                UpdateDevice();
            });
        }
        finally
        {
            _drawnAngle = null;
            UpdateDevice();
        }
    }
}
