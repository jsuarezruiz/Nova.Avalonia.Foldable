using Avalonia.Animation.Easings;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// The curves Flutter's <c>Curves</c> class exposes under the familiar CSS names, as Avalonia
/// easings.
/// </summary>
/// <remarks>
/// Every one of these is a cubic bezier in Flutter, including the ones whose names suggest a
/// polynomial: <c>Curves.easeInQuad</c> is <c>Cubic(0.55, 0.085, 0.68, 0.53)</c>, not
/// <c>t * t</c>. Reaching for Avalonia's <c>QuadraticEaseIn</c> or <c>CubicEaseOut</c> would
/// therefore change the motion, so the vignettes name the bezier control points instead and let
/// <see cref="SplineEasing"/> solve them.
/// </remarks>
public static class FlutterEasings
{
    public static Easing EaseOut { get; } = new SplineEasing(0d, 0d, 0.58d, 1d);

    public static Easing EaseOutQuad { get; } = new SplineEasing(0.25d, 0.46d, 0.45d, 0.94d);
}
