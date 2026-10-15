---
title: Samples
description: The two sample apps in the repository and how to run them.
---

# Samples

The repository has two sample apps. Both use the library from source.

## Sample

[Nova.Avalonia.Foldable.Sample](https://github.com/jsuarezruiz/Nova.Avalonia.Foldable/tree/main/samples/Nova.Avalonia.Foldable.Sample) (iOS and Android) shows every value the library reports, draws the fold as a red overlay over a two-pane layout, and keeps a log of hinge updates. Buttons switch `PanePriority` and `SnapToFold`. Every change is also written to the console with a `FOLDDIAG` prefix, which makes it the place to start when checking a new device.

## Showcase

[Nova.Avalonia.Foldable.Showcase](https://github.com/jsuarezruiz/Nova.Avalonia.Foldable/tree/main/samples/Nova.Avalonia.Foldable.Showcase) (iOS, Android and desktop) is the "Guide To the Stars" vignette from [Avalonia Vignettes](https://github.com/jsuarezruiz/avalonia_vignettes) adapted to two screens:

- the constellation list and the selected constellation on either side of the fold, under one sky;
- the title card flying across the fold when you pick a constellation;
- a stacked layout in tabletop posture;
- the state kept when the device folds and unfolds;
- the hinge warping the stars, the way scrolling does.

<p align="center"><img src="../../images/iphone-duo-showcase.gif" width="672" alt="The Showcase on the iPhone Duo simulator" /></p>

The desktop app adds the [foldable simulator](testing.md#the-desktop-simulator):

```bash
dotnet run --project samples/Nova.Avalonia.Foldable.Showcase/Nova.Avalonia.Foldable.Showcase.Desktop
```

## Running on a device

See [Platforms](platforms.md) for the iPhone Duo simulator and the Pixel Fold emulator setup.
