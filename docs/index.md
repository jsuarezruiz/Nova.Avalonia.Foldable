---
title: Welcome to Nova.Avalonia.Foldable
description: Foldable device support for Avalonia on the iPhone Duo and Android foldables.
ms.date: 2026-09-27
---

# Welcome to Nova.Avalonia.Foldable

**Nova.Avalonia.Foldable** adds foldable device support to [Avalonia](https://avaloniaui.net). It tells your app how the device is folded (posture, hinge angle and where the fold is) on the iPhone Duo and Android foldables, and includes a `TwoPaneView` that places your content on either side of the fold.

On devices without a fold nothing changes: the library reports no fold and `TwoPaneView` falls back to size-based layout, so the same app runs on phones, tablets, desktop and the web.

<p align="center"><img src="images/iphone-duo-showcase.gif" width="672" alt="The Showcase spanning both screens of the iPhone Duo simulator" /></p>

## What's included

- **[TwoPaneView](docs/twopaneview.md)**: a two-pane layout following the WinUI and .NET MAUI API that splits at the fold, stacks the panes in tabletop posture, and keeps the hidden pane's state when the device closes.
- **[Fold](docs/fold.md)**: inherited attached properties with the posture, hinge angle, fold bounds and orientation, ready for bindings, style selectors and code.
- **[Simulated fold](docs/testing.md)**: a provider you drive from code, for designing on desktop, the previewer and headless tests.

## Platforms

| Platform | Fold source | Tested on |
| --- | --- | --- |
| iOS 27.1+ | `UIHingeInteraction` and the view's division region | iPhone Duo simulator |
| Android 6.0+ (API 23) | Jetpack WindowManager and the hinge angle sensor | Pixel Fold emulator |
| Desktop, browser | None, reports no fold | |

See [Platforms](docs/platforms.md) for how each one works and how to set up the simulator and emulator.

## Requirements

- .NET 10
- Avalonia 12.1 or later
- For the iPhone Duo simulator, Xcode 27.1 with the iOS 27.1 simulator runtime

## Next steps

- Read the [Introduction](docs/introduction.md) for the concepts.
- Follow [Getting started](docs/getting-started.md) to add the library to your app.
- Browse the [API reference](api/Nova.Avalonia.Foldable.yml) for every type and member.
