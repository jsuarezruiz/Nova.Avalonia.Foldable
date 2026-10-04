using Avalonia.Animation.Easings;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// Flutter's <c>Curves</c> as Avalonia easings.
/// </summary>
/// <remarks>Flutter's curves are cubic beziers, so these use <see cref="SplineEasing"/> rather than Avalonia's polynomial easings.</remarks>
public static class FlutterEasings
{
    public static Easing EaseOut { get; } = new SplineEasing(0d, 0d, 0.58d, 1d);

    public static Easing EaseOutQuad { get; } = new SplineEasing(0.25d, 0.46d, 0.45d, 0.94d);
}
