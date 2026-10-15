using System;
using System.Buffers;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Nova.Avalonia.Foldable.Showcase.Animation;

namespace Nova.Avalonia.Foldable.Showcase.Controls;

/// <summary>
/// A perspective star field flying past the viewer. Port of <c>star_field.dart</c> and <c>star_field_painter.dart</c>.
/// </summary>
/// <remarks>Scrolling and navigation drive <see cref="Speed"/>; negative values run the field backward.</remarks>
public sealed class StarField : Control
{
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<StarField, double>(nameof(Speed), 0.2d);

    public static readonly StyledProperty<int> StarCountProperty =
        AvaloniaProperty.Register<StarField, int>(nameof(StarCount), 400);

    private const double MaxZ = 500d;

    private const double MinZ = 1d;

    private const double Spread = 75d;

    private const double GlowingShare = 0.1d;

    // One brush for every glowing star; many respawn per frame at warp speed.
    private static readonly IImmutableBrush GlowBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xD4, 0xA1, 0xFF));

    // Shared by every field and never disposed, so a draw operation the renderer still holds can always use it.
    private static readonly Lazy<Bitmap> Glow = new(() =>
    {
        using var stream = AssetLoader.Open(new Uri("avares://Nova.Avalonia.Foldable.Showcase/Assets/Images/glow.png"));
        return new Bitmap(stream);
    });

    private readonly Random _random = new();
    private readonly List<Star> _stars = [];

    private FrameTicker? _ticker;

    /// <summary>
    /// Gets or sets how far the field advances each frame.
    /// </summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public int StarCount
    {
        get => GetValue(StarCountProperty);
        set => SetValue(StarCountProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Bounds.Size;

        if (size.Width <= 0d || size.Height <= 0d)
        {
            return;
        }

        // The sky behind the stars is the host's background. One operation draws the whole field
        // each frame, instead of hundreds of draw calls recorded and replayed.
        var (sprites, count) = Project(size);
        context.Custom(new FieldDrawOperation(new Rect(size), sprites, count, Glow.Value));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_stars.Count == 0)
        {
            for (var i = 0; i < StarCount; i++)
            {
                _stars.Add(Randomize(new Star(), randomDepth: true));
            }
        }

        _ticker ??= new FrameTicker(this, _ => Advance());
        _ticker.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _ticker?.Stop();
    }

    // Moves every star toward the viewer by Speed. Stars that arrive respawn at the far end;
    // stars pushed past the far end wrap to the front, which keeps the field full at negative speeds.
    private void Advance()
    {
        foreach (var star in _stars)
        {
            star.Z -= Speed;

            if (star.Z < MinZ)
            {
                Randomize(star, randomDepth: false);
            }
            else if (star.Z > MaxZ)
            {
                star.Z = MinZ;
            }
        }

        InvalidateVisual();
    }

    // Works out where each star lands this frame. The draw operation renders on another thread,
    // so it gets its own copy rather than the stars themselves, in a pooled array it returns when done.
    private (Sprite[] Sprites, int Count) Project(Size size)
    {
        var sprites = ArrayPool<Sprite>.Shared.Rent(_stars.Count);
        var count = 0;
        var time = Environment.TickCount64 / 200d;

        foreach (var star in _stars)
        {
            // Depth both shrinks the star and pulls it toward the center.
            var scale = 0.1d + (star.Size * (1d - (star.Z / size.Width)));

            if (scale <= 0d)
            {
                continue;
            }

            var x = (size.Width / 2d) + (star.X / star.Z * size.Width);
            var y = (size.Height / 2d) + (star.Y / star.Z * size.Height);

            // Stars close to the viewer spread past the edges; there's nothing to draw for them.
            var reach = star.IsGlowing ? (scale * 3d) + 1d : scale;

            if (x + reach < 0d || x - reach > size.Width || y + reach < 0d || y - reach > size.Height)
            {
                continue;
            }

            var halo = default(Rect);

            if (star.IsGlowing)
            {
                // The halo's width and height oscillate out of step, each star at its own phase.
                var phase = time + star.Phase;
                var width = (scale * 6d) + (2d * Math.Sin(phase * 0.5d));
                var height = (scale * 6d) + (2d * Math.Cos(phase * 0.75d));

                if (width > 0d && height > 0d)
                {
                    halo = new Rect(x - (width / 2d), y - (height / 2d), width, height);
                }
            }

            sprites[count++] = new Sprite(new Point(x, y), scale, star.Brush, halo);
        }

        return (sprites, count);
    }

    private Star Randomize(Star star, bool randomDepth)
    {
        star.X = (-1d + (_random.NextDouble() * 2d)) * Spread;
        star.Y = (-1d + (_random.NextDouble() * 2d)) * Spread;
        star.Z = randomDepth ? _random.NextDouble() * MaxZ : MaxZ;

        star.IsGlowing = _random.NextDouble() < GlowingShare;
        star.Phase = _random.NextDouble() * Math.Tau;

        if (star.IsGlowing)
        {
            star.Brush = GlowBrush;
            star.Size = 2d + (_random.NextDouble() * 2d);
        }
        else
        {
            star.Brush = Brushes.White;
            star.Size = 0.5d + (_random.NextDouble() * 2d);
        }

        return star;
    }

    private sealed class Star
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public double Size { get; set; } = 1d;

        public bool IsGlowing { get; set; }

        public double Phase { get; set; }

        public IImmutableBrush Brush { get; set; } = Brushes.White;
    }

    private readonly record struct Sprite(Point Center, double Radius, IImmutableBrush Brush, Rect Halo);

    // Draws one frame of the field straight to the renderer. It only reads what it was given.
    private sealed class FieldDrawOperation : ICustomDrawOperation
    {
        private readonly Bitmap _glow;
        private Sprite[] _sprites;
        private int _count;

        public FieldDrawOperation(Rect bounds, Sprite[] sprites, int count, Bitmap glow)
        {
            Bounds = bounds;
            _sprites = sprites;
            _count = count;
            _glow = glow;
        }

        public Rect Bounds { get; }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            foreach (var sprite in _sprites.AsSpan(0, _count))
            {
                context.DrawEllipse(sprite.Brush, null, sprite.Center, sprite.Radius, sprite.Radius);

                if (sprite.Halo.Width > 0d)
                {
                    context.DrawBitmap(_glow, sprite.Halo);
                }
            }
        }

        // Called once the renderer has replaced this frame.
        public void Dispose()
        {
            if (_count < 0)
            {
                return;
            }

            ArrayPool<Sprite>.Shared.Return(_sprites);
            _sprites = [];
            _count = -1;
        }
    }
}
