using Avalonia;
using Avalonia.Controls;

namespace Nova.Avalonia.Foldable.Showcase.Animation;

/// <summary>
/// Calls back once per frame of the owner's <see cref="TopLevel"/> with the time since <see cref="Start"/>, like Flutter's <c>Ticker</c>.
/// </summary>
public sealed class FrameTicker
{
    private readonly Visual _owner;
    private readonly Action<TimeSpan> _onTick;

    private TimeSpan _startedAt;
    private bool _hasStartTime;
    private bool _framePending;
    private bool _waitingForAttach;

    /// <summary>
    /// Creates a ticker that calls <paramref name="onTick"/> once per frame of <paramref name="owner"/>.
    /// </summary>
    public FrameTicker(Visual owner, Action<TimeSpan> onTick)
    {
        _owner = owner;
        _onTick = onTick;
    }

    /// <summary>
    /// Gets whether the ticker is requesting frames.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts (or restarts) the ticker, resetting the elapsed time to zero.
    /// </summary>
    public void Start()
    {
        _hasStartTime = false;

        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        RequestFrame();
    }

    /// <summary>
    /// Stops the ticker. Safe to call when it is not running.
    /// </summary>
    public void Stop() => IsRunning = false;

    // At most one frame is pending at a time; a restart from inside the tick reuses it.
    private void RequestFrame()
    {
        if (_framePending)
        {
            return;
        }

        if (TopLevel.GetTopLevel(_owner) is { } topLevel)
        {
            _framePending = true;
            topLevel.RequestAnimationFrame(OnFrame);
        }
        else if (!_waitingForAttach)
        {
            // Out of the tree (a hidden pane): no frames to ask for, so carry on once it's back.
            _waitingForAttach = true;
            _owner.AttachedToVisualTree += OnReattached;
        }
    }

    private void OnReattached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _owner.AttachedToVisualTree -= OnReattached;
        _waitingForAttach = false;

        if (IsRunning)
        {
            // Measure from the resume; the time spent detached doesn't count.
            _hasStartTime = false;
            RequestFrame();
        }
    }

    private void OnFrame(TimeSpan now)
    {
        _framePending = false;

        if (!IsRunning)
        {
            return;
        }

        if (!_hasStartTime)
        {
            _startedAt = now;
            _hasStartTime = true;
        }

        _onTick(now - _startedAt);

        if (IsRunning)
        {
            RequestFrame();
        }
    }
}
