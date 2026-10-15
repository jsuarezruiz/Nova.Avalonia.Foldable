using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Nova.Avalonia.Foldable.Showcase.Controls;
using Nova.Avalonia.Foldable.Showcase.Models;

namespace Nova.Avalonia.Foldable.Showcase.Views;

/// <summary>
/// The scrolling guide. Port of <c>constellation_list_view.dart</c> and <c>constellation_list_renderer.dart</c>.
/// </summary>
/// <remarks>Indents are the original's seeded random sequence, kept as a table so the layout is stable.</remarks>
public partial class ListPage : UserControl
{
    private const double EdgeNudge = 25d;

    private const double VerticalPadding = 24d;

    public ListPage()
    {
        InitializeComponent();

        AddEntries();

        Scroller.ScrollChanged += OnScrollChanged;
    }

    /// <summary>
    /// Occurs when the list scrolls, with the distance moved since the last report.
    /// </summary>
    public event EventHandler<double>? Scrolled;

    /// <summary>
    /// Occurs when an entry is tapped, with the tapped card.
    /// </summary>
    public event EventHandler<ConstellationTitleCard>? EntryTapped;

    private void AddEntries()
    {
        // As in the original: 20 for the first entry, then Dart's Random(1).nextInt(4) * 20 sequence.
        // System.Random(1) produces a different one, so the values are listed.
        double[] indents = [20, 0, 60, 60, 60, 60, 20, 0, 20, 0, 20, 40, 60, 20, 0];

        for (var index = 0; index < DemoData.Constellations.Count; index++)
        {
            var isRedMode = index % 2 == 1;

            var indent = indents[index % indents.Length];

            var card = new ConstellationTitleCard
            {
                Constellation = DemoData.Constellations[index],
                IsRedMode = isRedMode,
            };

            var row = new Button
            {
                Content = card,
                Padding = new Thickness(
                    isRedMode ? 0d : indent,
                    VerticalPadding,
                    isRedMode ? indent : 0d,
                    VerticalPadding),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent,
                BorderThickness = default,
                RenderTransform = new TranslateTransform(isRedMode ? EdgeNudge : -EdgeNudge, 0d),
            };

            row.Classes.Add("constellationRow");
            card.HorizontalAlignment = isRedMode ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            row.Click += (_, _) => EntryTapped?.Invoke(this, card);

            Entries.Items.Add(row);
        }
    }

    // Only scrolling drives the stars. A change of viewport or extent, as when the device folds or the
    // list is rescaled, can move the offset too; those reports are left out.
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.OffsetDelta.Y != 0d && e.ExtentDelta == default && e.ViewportDelta == default)
            Scrolled?.Invoke(this, e.OffsetDelta.Y);
    }
}
