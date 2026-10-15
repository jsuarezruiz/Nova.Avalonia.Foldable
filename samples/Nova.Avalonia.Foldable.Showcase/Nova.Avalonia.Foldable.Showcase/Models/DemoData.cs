using System.Collections.Generic;
using System.Linq;

namespace Nova.Avalonia.Foldable.Showcase.Models;

/// <summary>
/// The constellations the vignette ships with. Port of <c>DemoData</c>.
/// </summary>
/// <remarks>Five constellations repeated three times.</remarks>
public static class DemoData
{
    private const int Repeats = 3;

    public static IReadOnlyList<Constellation> Constellations { get; } = Build();

    private static Constellation[] Build()
    {
        Constellation[] set =
        [
            new("Aries", "The Ram", "Aries"),
            new("Cassiopeia", "The Queen", "Cassiopeia"),
            new("Camelopardalis", "The Giraffe", "Camelopardalis"),
            new("Cetus", "The Whale", "Cetus"),
            new("Pisces", "The Fishes", "Pisces"),
        ];

        return [.. Enumerable.Range(0, Repeats).SelectMany(_ => set)];
    }
}
