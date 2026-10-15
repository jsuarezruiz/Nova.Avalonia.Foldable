using System;
using System.Linq;
using Avalonia;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// Interpolates a rectangle by moving two opposite corners along arcs. Port of Flutter's <c>MaterialRectArcTween</c>.
/// </summary>
public sealed class MaterialRectArcTween
{
    private static readonly (Corner Begin, Corner End)[] Diagonals =
    [
        (Corner.TopLeft, Corner.BottomRight),
        (Corner.BottomRight, Corner.TopLeft),
        (Corner.TopRight, Corner.BottomLeft),
        (Corner.BottomLeft, Corner.TopRight),
    ];

    private readonly MaterialPointArcTween _beginArc;
    private readonly MaterialPointArcTween _endArc;

    /// <summary>
    /// Creates a tween from <paramref name="begin"/> to <paramref name="end"/>.
    /// </summary>
    public MaterialRectArcTween(Rect begin, Rect end)
    {
        Begin = begin;
        End = end;

        var centersVector = end.Center - begin.Center;
        var diagonal = Diagonals.MaxBy(candidate => Support(begin, centersVector, candidate));

        _beginArc = new MaterialPointArcTween(CornerOf(begin, diagonal.Begin), CornerOf(end, diagonal.Begin));
        _endArc = new MaterialPointArcTween(CornerOf(begin, diagonal.End), CornerOf(end, diagonal.End));
    }

    /// <summary>
    /// Gets the rectangle the flight starts from.
    /// </summary>
    public Rect Begin { get; }

    /// <summary>
    /// Gets the rectangle the flight lands on.
    /// </summary>
    public Rect End { get; }

    /// <summary>
    /// Gets the rectangle <paramref name="progress"/> of the way through the flight.
    /// </summary>
    public Rect Lerp(double progress)
    {
        if (progress <= 0d)
        {
            return Begin;
        }

        if (progress >= 1d)
        {
            return End;
        }

        var first = _beginArc.Lerp(progress);
        var second = _endArc.Lerp(progress);

        return new Rect(
            Math.Min(first.X, second.X),
            Math.Min(first.Y, second.Y),
            Math.Abs(second.X - first.X),
            Math.Abs(second.Y - first.Y));
    }

    private static double Support(Rect begin, Vector centers, (Corner Begin, Corner End) diagonal)
    {
        var delta = CornerOf(begin, diagonal.End) - CornerOf(begin, diagonal.Begin);
        var length = Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));

        return ((centers.X * delta.X) + (centers.Y * delta.Y)) / length;
    }

    private static Point CornerOf(Rect rect, Corner corner) => corner switch
    {
        Corner.TopLeft => rect.TopLeft,
        Corner.TopRight => rect.TopRight,
        Corner.BottomLeft => rect.BottomLeft,
        Corner.BottomRight => rect.BottomRight,
        _ => throw new ArgumentOutOfRangeException(nameof(corner)),
    };

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }
}
