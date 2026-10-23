---
title: Introduction
description: The concepts behind Nova.Avalonia.Foldable and how its pieces fit together.
---

# Introduction

A foldable has a hinge, and the hinge changes what a good layout looks like. Held like a book, the fold splits the screen into two pages. Resting on a table, the top half becomes a screen and the bottom half a place for controls. Closed, the app moves to a small cover display. Nova.Avalonia.Foldable reports these changes to your Avalonia app and gives you a layout control that follows them.

## Concepts

- The **posture** is how the device is folded. [DevicePosture](xref:Nova.Avalonia.Foldable.DevicePosture) is `Closed`, `HalfOpened` (book or tabletop) or `Flat`, and `Unknown` until the hinge reports or when the platform can't tell.
- The **hinge angle** goes from 0 degrees (closed) to 180 (flat), where the platform exposes it. [Reading the fold](fold.md#hinge-angle) explains what it's good for.
- The **fold bounds** are the fold as a rectangle in the coordinates of the top level. On the iPhone Duo it's a 40pt band, and on Android it can be a thin line.
- **Separating** means the fold currently divides the content, as it does while the device is half open.
- The **orientation** says whether the fold runs top to bottom (panes side by side) or left to right (panes stacked).

All of these live in one snapshot, [FoldState](xref:Nova.Avalonia.Foldable.FoldState). Devices without a fold report [FoldState.None](xref:Nova.Avalonia.Foldable.FoldState.None).

## How the pieces fit

1. A **provider** reads the fold for one top level. The iOS provider uses UIKit, the Android provider uses Jetpack WindowManager, and every other platform gets a provider that reports no fold. Each provider implements [IFoldStateProvider](xref:Nova.Avalonia.Foldable.IFoldStateProvider).
2. **[Fold](xref:Nova.Avalonia.Foldable.Fold)** publishes the provider's state on the top level as inherited attached properties, so every control below it can read, bind to or style on the fold.
3. **[TwoPaneView](xref:Nova.Avalonia.Foldable.TwoPaneView)** reads those properties and lays out its two panes around the fold.

You rarely touch providers directly, since `TwoPaneView` and [Fold.IsEnabled](fold.md#start-tracking) create them for you.

## Platform APIs

The iPhone Duo APIs are new in iOS 27.1 and ship in the .NET for iOS 27.1 workload, so iOS heads target `net10.0-ios27.1`. [Platforms](platforms.md#build-requirements) has the details. Android needs no custom bindings, since it uses Microsoft's `Xamarin.AndroidX.Window` packages.
