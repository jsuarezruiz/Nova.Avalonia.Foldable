using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;

namespace Nova.Avalonia.Foldable.Simulator;

/// <summary>
/// Runs a short eased animation on the owner's frames.
/// </summary>
internal static class Tween
{
    /// <summary>
    /// Calls <paramref name="step"/> with an eased progress from 0 to 1 once per frame, ending on exactly 1.
    /// </summary>
    public static Task Run(Visual owner, TimeSpan duration, Action<double> step, CancellationToken token = default)
    {
        var done = new TaskCompletionSource();
        FrameTicker? ticker = null;
        ticker = new FrameTicker(owner, elapsed =>
        {
            if (token.IsCancellationRequested)
            {
                ticker!.Stop();
                done.TrySetCanceled(token);
                return;
            }

            var t = duration <= TimeSpan.Zero ? 1d : Math.Clamp(elapsed / duration, 0d, 1d);
            step(EaseInOut(t));
            if (t >= 1d)
            {
                ticker!.Stop();
                done.TrySetResult();
            }
        });
        ticker.Start();
        return done.Task;
    }

    // Cubic ease in and out: a hinge starts and stops gently.
    private static double EaseInOut(double t) => t < 0.5d ? 4d * t * t * t : 1d - (Math.Pow((-2d * t) + 2d, 3d) / 2d);
}
