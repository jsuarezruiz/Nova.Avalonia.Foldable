---
title: TwoPaneView
description: A two-pane layout that splits at the fold on foldables and by size everywhere else.
ms.date: 2026-09-27
---

# TwoPaneView

[TwoPaneView](xref:Nova.Avalonia.Foldable.TwoPaneView) shows two panes side by side, stacked, or one at a time. On a foldable it puts them on either side of the fold; everywhere else it decides by its own size. It follows the WinUI and .NET MAUI `TwoPaneView` API, so code and knowledge carry over; the differences are listed below.

## Create a two-pane view

Set the content of each pane with `Pane1` and `Pane2`:

```xml
<fold:TwoPaneView xmlns:fold="using:Nova.Avalonia.Foldable">
  <fold:TwoPaneView.Pane1>
    <local:InboxView />
  </fold:TwoPaneView.Pane1>
  <fold:TwoPaneView.Pane2>
    <local:MessageView />
  </fold:TwoPaneView.Pane2>
</fold:TwoPaneView>
```

`TwoPaneView` starts tracking the fold as soon as it is on screen; you don't need to set `Fold.IsEnabled`.

## How the layout is chosen

The view goes through these rules in order and uses the first one that applies:

1. **Half open**, as a book or on a table: the panes sit on either side of the fold, side by side for a vertical fold and stacked for a horizontal one. The panes never overlap the fold itself.
2. **Flat** with a fold across the view: the panes split at the fold when the view is wider than `MinWideModeWidth` (or taller than `MinTallModeHeight` for a horizontal fold). Turn this off with `SnapToFold="False"` to split by size instead.
3. **Wide enough**: side by side when the view is wider than `MinWideModeWidth`, split by `Pane1Length` and `Pane2Length`.
4. **Tall enough**: stacked when the view is taller than `MinTallModeHeight`.
5. **Otherwise**: one pane, the one `PanePriority` names.

A closed device shows its cover display, so the fold is ignored and the size rules apply.

## Pane order

`WideModeConfiguration` controls side-by-side layouts: `LeftRight` (the default) puts Pane1 on the left, `RightLeft` puts it on the right, and `SinglePane` never shows two panes side by side. `TallModeConfiguration` does the same for stacked layouts with `TopBottom`, `BottomTop` and `SinglePane`.

A common choice for tabletop posture is `BottomTop`, which puts Pane1 below the fold, within reach, and Pane2 above it:

```xml
<fold:TwoPaneView TallModeConfiguration="BottomTop" />
```

To keep a phone from stacking the panes just because it is tall, raise `MinTallModeHeight` well above any screen height. The panes still stack at a horizontal fold when the device is half open.

## Sizes

When no fold decides the split, `Pane1Length` and `Pane2Length` do. Pixel lengths are honored, and star lengths share the rest; `Auto` counts as `1*`.

```xml
<!-- A fixed 320px list next to a detail pane that takes the rest. -->
<fold:TwoPaneView Pane1Length="320" Pane2Length="*" />
```

## One pane at a time

In single-pane mode `PanePriority` picks the pane on screen. The other pane leaves the visual tree but stays a logical child, so its state survives until it is shown again, for example when the device unfolds. Switch panes by changing the priority:

```csharp
Panes.PanePriority = TwoPaneViewPriority.Pane2; // Show the detail.
```

Anything that ticks off the top level's frames can't run while its pane is hidden. Start animations when the pane is shown, or in `AttachedToVisualTree`.

## Reacting to mode changes

`Mode` reports the current layout (`SinglePane`, `Wide` or `Tall`), and `ModeChanged` fires when it changes:

```csharp
Panes.ModeChanged += (_, _) =>
    BackButton.IsVisible = Panes.Mode == TwoPaneViewMode.SinglePane;
```

`Mode` and `ModeChanged` update at the end of the layout pass, so the panes are already arranged when the event fires.

## Differences from WinUI and .NET MAUI

- **Fold awareness**: the panes follow the fold reported by the platform, including the width of the fold itself.
- **SnapToFold**: an addition that splits at the fold on a flat device too.
- The thresholds behave like WinUI's: two panes only when the size is *above* `MinWideModeWidth` or `MinTallModeHeight`, so exactly 641 still shows one pane.

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Pane1` | `Control?` | `null` | The first pane: left, top, or the only one by default. |
| `Pane2` | `Control?` | `null` | The second pane: right or bottom. |
| `Pane1Length` | `GridLength` | `1*` | Pane1's share when no fold decides the split. |
| `Pane2Length` | `GridLength` | `1*` | Pane2's share when no fold decides the split. |
| `PanePriority` | `TwoPaneViewPriority` | `Pane1` | The pane shown in single-pane mode. |
| `MinWideModeWidth` | `double` | `641` | Width above which the panes go side by side. |
| `MinTallModeHeight` | `double` | `641` | Height above which the panes stack. |
| `WideModeConfiguration` | `TwoPaneViewWideModeConfiguration` | `LeftRight` | Pane order, or single pane, when the view is wide. |
| `TallModeConfiguration` | `TwoPaneViewTallModeConfiguration` | `TopBottom` | Pane order, or single pane, when the view is tall. |
| `SnapToFold` | `bool` | `true` | Split at the fold on a flat device too. |
| `Mode` | `TwoPaneViewMode` | `SinglePane` | (Read-only) How the panes are currently shown. |

## Events

| Event | Description |
| --- | --- |
| `ModeChanged` | Occurs when `Mode` changes. |
