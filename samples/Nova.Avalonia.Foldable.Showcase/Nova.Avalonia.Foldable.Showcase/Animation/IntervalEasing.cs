using System;
using Avalonia.Animation.Easings;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// Port of Flutter's <c>Interval</c>: 0 until <see cref="Begin"/>, <see cref="Curve"/> until <see cref="End"/>, then 1.
/// </summary>
public sealed class IntervalEasing : Easing
{
    public IntervalEasing(double begin, double end, Easing? curve = null)
    {
        Begin = begin;
        End = end;
        Curve = curve;
    }

    /// <summary>
    /// Gets where the inner curve starts, as a fraction of the whole animation.
    /// </summary>
    public double Begin { get; }

    /// <summary>
    /// Gets where the inner curve finishes, as a fraction of the whole animation.
    /// </summary>
    public double End { get; }

    /// <summary>
    /// Gets the curve run over the window, or null for linear.
    /// </summary>
    public Easing? Curve { get; }

    public override double Ease(double progress)
    {
        var t = Math.Clamp((progress - Begin) / (End - Begin), 0d, 1d);

        // The ends are exact, even for a curve that misses 0 or 1.
        return t is 0d or 1d ? t : Curve?.Ease(t) ?? t;
    }
}
