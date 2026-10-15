---
title: Testing without a device
description: Design and test fold-aware layouts on desktop, in the previewer and in headless tests.
---

# Testing without a device

You don't need a foldable to build for one. A [SimulatedFoldStateProvider](xref:Nova.Avalonia.Foldable.SimulatedFoldStateProvider) feeds a fold you set from code into the same attached properties the platform providers use, so your layouts can't tell the difference.

## Simulate a fold

Give a window or other top level a simulated provider, then set its state:

```csharp
var fold = new SimulatedFoldStateProvider();
Fold.SetProvider(window, fold);

fold.Current = new FoldState(
    IsFoldable: true,
    Posture: DevicePosture.HalfOpened,
    HingeAngle: 110,
    FoldBounds: new Rect(380, 0, 40, 860), // top-level coordinates
    IsSeparating: true);
```

Setting `Current` raises `Changed`, and the whole tree updates. Set it back to `FoldState.None` to simulate a regular device.

## Replace the provider everywhere

`Fold.ProviderFactory` decides which provider each new top level gets. Return `null` to keep the platform's default:

```csharp
// Every window starts on a simulated book posture while designing.
Fold.ProviderFactory = _ => new SimulatedFoldStateProvider(
    new FoldState(true, DevicePosture.HalfOpened, 110, new Rect(380, 0, 40, 860), true));
```

## Headless tests

With Avalonia's headless platform (`Avalonia.Headless.XUnit`), layout rules can be checked in a unit test. The repository's own tests work this way:

```csharp
[AvaloniaFact]
public void Half_Open_Splits_At_The_Fold()
{
    var view = new TwoPaneView { Pane1 = new Border(), Pane2 = new Border() };
    var window = new Window { Width = 900, Height = 600, Content = view };
    Fold.SetProvider(window, new SimulatedFoldStateProvider(
        new FoldState(true, DevicePosture.HalfOpened, 110, new Rect(430, 0, 40, 600), true)));
    window.Show();
    view.UpdateLayout();

    Assert.Equal(TwoPaneViewMode.Wide, view.Mode);
    Assert.Equal(new Rect(0, 0, 430, 600), view.Pane1!.Bounds);
    Assert.Equal(new Rect(470, 0, 430, 600), view.Pane2!.Bounds);
}
```

## The desktop simulator

The Showcase's desktop app wraps a simulated provider in a foldable simulator:

<p align="center"><img src="../../images/desktop-simulator.png" width="720" alt="Showcase with the desktop foldable simulator" /></p>

- **Postures**: regular phone, closed, open, book, open rotated and tabletop, with the iPhone Duo's screen sizes and 40pt fold.
- **Hinge**: the slider works like the real hinge: 0° closes the device, 180° lays it flat, and anything in between half-opens it.
- **What the app sees**: a live readout of the state the app receives.
- **Keys**: 1 to 6 pick a posture, and the left and right arrows move the hinge (hold Shift for 1°).
- **Open the device**: plays the device unfolding from the cover display to flat.

[Samples](samples.md#showcase) has the command that runs it. Sizes and the fold match the iPhone Duo, but safe-area insets aren't simulated. Check the final result on the [iPhone Duo simulator or the Pixel Fold emulator](platforms.md).
