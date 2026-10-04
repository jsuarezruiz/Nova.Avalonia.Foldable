---
title: Introduction
description: The concepts behind Nova.Avalonia.Foldable and how its pieces fit together.
ms.date: 2026-09-27
---

# Introduction

A foldable has a hinge, and the hinge changes what a good layout looks like. Held like a book, the fold splits the screen into two pages. Resting on a table, the top half becomes a screen and the bottom half a place for controls. Closed, the app moves to a small cover display. Nova.Avalonia.Foldable reports these changes to your Avalonia app and gives you a layout control that follows them.

## Concepts

- **Posture**: how the device is folded. [DevicePosture](xref:Nova.Avalonia.Foldable.DevicePosture) is `Closed`, `HalfOpened` (book or tabletop) or `Flat`, and `Unknown` until the hinge reports.
- **Hinge angle**: degrees from 0 (closed) to 180 (flat), where the platform exposes it. Use it for effects, never for layout: not every device reports it.
- **Fold bounds**: the fold as a rectangle in the coordinates of the top level. On the iPhone Duo it is a 40pt band; on Android it can be a thin line.
- **Separating**: whether the fold currently divides the content, as it does while the device is half open.
- **Orientation**: whether the fold runs top to bottom (panes side by side) or left to right (panes stacked).

All of these live in one snapshot, [FoldState](xref:Nova.Avalonia.Foldable.FoldState). Devices without a fold report [FoldState.None](xref:Nova.Avalonia.Foldable.FoldState.None).

## How the pieces fit

1. A **provider** reads the fold for one top level: the iOS provider uses UIKit, the Android provider uses Jetpack WindowManager, and every other platform gets a provider that reports no fold. Each provider implements [IFoldStateProvider](xref:Nova.Avalonia.Foldable.IFoldStateProvider).
2. **[Fold](xref:Nova.Avalonia.Foldable.Fold)** publishes the provider's state on the top level as inherited attached properties, so every control below it can read, bind to or style on the fold.
3. **[TwoPaneView](xref:Nova.Avalonia.Foldable.TwoPaneView)** reads those properties and lays out its two panes around the fold.

You rarely touch providers directly. `TwoPaneView` starts tracking the fold as soon as it is on screen, and `Fold.IsEnabled` does the same for any other control.

## Based on dotnet/macios#26705

The iPhone Duo APIs are new in iOS 27.1, and .NET for iOS doesn't ship them yet. They are being added in [dotnet/macios#26705](https://github.com/dotnet/macios/pull/26705). Until that ships, the repository includes `Nova.Avalonia.Foldable.Bindings.iOS`, a copy of that PR's definitions. Once .NET for iOS includes them, the library switches to the official types and apps don't need to change. Android needs no custom bindings: it uses Microsoft's `Xamarin.AndroidX.Window` packages.
