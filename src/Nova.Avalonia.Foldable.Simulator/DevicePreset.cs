using Avalonia;

namespace Nova.Avalonia.Foldable.Simulator;

/// <summary>
/// The direction of the fold across the inner display.
/// </summary>
internal enum FoldAxis
{
    None,
    Vertical,
    Horizontal,
}

/// <summary>
/// A posture the simulator can show: the screen the app gets, and how the hinge sits.
/// </summary>
internal sealed record DevicePreset(
    string Title,
    string Hint,
    Size Screen,
    bool Foldable,
    FoldAxis Axis,
    DevicePosture Posture,
    double Angle)
{
    /// <summary>
    /// The iPhone Duo's inner display in points, as its simulator reports it.
    /// </summary>
    public static readonly Size InnerScreen = new(951, 669);

    /// <summary>
    /// The iPhone Duo's cover display in points.
    /// </summary>
    public static readonly Size CoverScreen = new(466, 678);

    /// <summary>
    /// The iPhone Duo reports the fold as a 40pt band, not a line.
    /// </summary>
    public const double FoldThickness = 40;

    public const int RegularIndex = 0;
    public const int ClosedIndex = 1;
    public const int OpenIndex = 2;
    public const int BookIndex = 3;
    public const int OpenRotatedIndex = 4;
    public const int TabletopIndex = 5;

    public static DevicePreset[] All { get; } =
    [
        new("Regular phone", "No hinge", new Size(390, 844), false, FoldAxis.None, DevicePosture.Unknown, 0),
        new("Closed", "Cover display", CoverScreen, true, FoldAxis.None, DevicePosture.Closed, 0),
        new("Open", "Flat, vertical fold", InnerScreen, true, FoldAxis.Vertical, DevicePosture.Flat, 180),
        new("Book", "Half open, vertical fold", InnerScreen, true, FoldAxis.Vertical, DevicePosture.HalfOpened, 110),
        new("Open, rotated", "Flat, horizontal fold", new Size(InnerScreen.Height, InnerScreen.Width), true, FoldAxis.Horizontal, DevicePosture.Flat, 180),
        new("Tabletop", "Half open, horizontal fold", new Size(InnerScreen.Height, InnerScreen.Width), true, FoldAxis.Horizontal, DevicePosture.HalfOpened, 100),
    ];

    public bool IsClosed => Posture == DevicePosture.Closed;

    public bool IsRotated => Axis == FoldAxis.Horizontal;
}
