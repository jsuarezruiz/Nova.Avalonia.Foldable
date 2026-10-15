using System;
using Avalonia;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// Interpolates a point along a circular arc. Port of Flutter's <c>MaterialPointArcTween</c>.
/// </summary>
/// <remarks>Moves that are nearly horizontal or vertical are interpolated in a straight line.</remarks>
public sealed class MaterialPointArcTween
{
    private const double OnAxisDelta = 2d;

    private readonly Point _begin;
    private readonly Point _end;
    private readonly Point _center;
    private readonly double _radius;
    private readonly double _beginAngle;
    private readonly double _endAngle;
    private readonly bool _isArc;

    /// <summary>
    /// Creates an arc from <paramref name="begin"/> to <paramref name="end"/>.
    /// </summary>
    public MaterialPointArcTween(Point begin, Point end)
    {
        _begin = begin;
        _end = end;

        var delta = end - begin;
        var deltaX = Math.Abs(delta.X);
        var deltaY = Math.Abs(delta.Y);
        var distance = Distance(delta);

        if (deltaX <= OnAxisDelta || deltaY <= OnAxisDelta)
        {
            return;
        }

        _isArc = true;

        // The corner of the bounding box that shares the begin point's row.
        var corner = new Point(end.X, begin.Y);

        if (deltaX < deltaY)
        {
            // A mostly vertical move: the arc's center sits level with the end point.
            _radius = distance * distance / Distance(corner - begin) / 2d;
            _center = new Point(end.X + (_radius * Math.Sign(begin.X - end.X)), end.Y);

            if (begin.X < end.X)
            {
                _beginAngle = SweepAngle() * Math.Sign(begin.Y - end.Y);
                _endAngle = 0d;
            }
            else
            {
                _beginAngle = Math.PI + (SweepAngle() * Math.Sign(end.Y - begin.Y));
                _endAngle = Math.PI;
            }
        }
        else
        {
            // A mostly horizontal move: the center sits directly above or below the begin point.
            _radius = distance * distance / Distance(corner - end) / 2d;
            _center = new Point(begin.X, begin.Y + (Math.Sign(end.Y - begin.Y) * _radius));

            if (begin.Y < end.Y)
            {
                _beginAngle = -Math.PI / 2d;
                _endAngle = _beginAngle + (SweepAngle() * Math.Sign(end.X - begin.X));
            }
            else
            {
                _beginAngle = Math.PI / 2d;
                _endAngle = _beginAngle + (SweepAngle() * Math.Sign(begin.X - end.X));
            }
        }

        double SweepAngle() => 2d * Math.Asin(distance / (2d * _radius));
    }

    /// <summary>
    /// Gets the point <paramref name="progress"/> of the way along the arc.
    /// </summary>
    public Point Lerp(double progress)
    {
        if (progress <= 0d)
        {
            return _begin;
        }

        if (progress >= 1d)
        {
            return _end;
        }

        if (!_isArc)
        {
            return _begin + ((_end - _begin) * progress);
        }

        var angle = _beginAngle + ((_endAngle - _beginAngle) * progress);

        return _center + new Point(Math.Cos(angle) * _radius, Math.Sin(angle) * _radius);
    }

    private static double Distance(Point point) => Math.Sqrt((point.X * point.X) + (point.Y * point.Y));
}
