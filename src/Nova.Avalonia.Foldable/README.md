# Nova.Avalonia.Foldable

Foldable device support for [Avalonia](https://avaloniaui.net). It tells your app how the device is folded (posture, hinge angle and where the fold is) on the iPhone Duo and Android foldables, and includes a `TwoPaneView` that places your content on either side of the fold.

On devices without a fold nothing changes. The library reports no fold and `TwoPaneView` falls back to size-based layout, so the same app runs on phones, tablets, desktop and the web.

![The Showcase spanning both screens of the iPhone Duo simulator](https://raw.githubusercontent.com/jsuarezruiz/Nova.Avalonia.Foldable/main/images/iphone-duo-showcase.gif)

## Platform support

| Platform | Fold source | Tested on |
| --- | --- | --- |
| iOS 27.1+ | `UIHingeInteraction` and the view's division region | iPhone Duo simulator |
| Android 6.0+ (API 23) | Jetpack WindowManager and the hinge angle sensor | Pixel Fold emulator |
| Desktop, browser | None, reports no fold | |

On iOS the package needs the .NET for iOS 27.1 workload and Xcode 27.1, and the iOS head targets `net10.0-ios27.1`. [Platforms](https://jsuarezruiz.github.io/Nova.Avalonia.Foldable/docs/platforms.html#build-requirements) explains why. On iOS versions before 27.1 the app still runs and reports no fold.

## Getting started

The Android head should handle configuration changes, so the activity isn't recreated when the device folds. The iOS head targets `net10.0-ios27.1`. [Getting started](https://jsuarezruiz.github.io/Nova.Avalonia.Foldable/docs/getting-started.html) shows both.

## Two panes around the fold

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:fold="using:Nova.Avalonia.Foldable"
             xmlns:local="using:MyApp.Views">
  <fold:TwoPaneView>
    <fold:TwoPaneView.Pane1>
      <local:InboxView />
    </fold:TwoPaneView.Pane1>
    <fold:TwoPaneView.Pane2>
      <local:MessageView />
    </fold:TwoPaneView.Pane2>
  </fold:TwoPaneView>
</UserControl>
```

`TwoPaneView` follows the WinUI and .NET MAUI `TwoPaneView` API. When the fold divides the screen, as on a half-open device, the panes sit on either side of it, or above and below it in tabletop posture. When it's closed or has no fold, the usual size rules apply, and `PanePriority` picks the pane to show when only one fits.

## Reading the fold

Set `Fold.IsEnabled` near the root. Every control below it then inherits the fold through the attached properties `Fold.State`, `Posture`, `HingeAngle`, `IsSeparating`, `Orientation` and `IsFoldable`.

```xml
<UserControl xmlns:fold="using:Nova.Avalonia.Foldable"
             fold:Fold.IsEnabled="True">
  <TextBlock Text="{Binding $self.(fold:Fold.Posture)}" />
</UserControl>
```

A `SimulatedFoldStateProvider` lets you design and test fold layouts on desktop, in the previewer and in headless tests.

## Documentation

- [Documentation](https://jsuarezruiz.github.io/Nova.Avalonia.Foldable/), with getting started, `TwoPaneView`, reading the fold, testing without a device and platform details
- [API reference](https://jsuarezruiz.github.io/Nova.Avalonia.Foldable/api/Nova.Avalonia.Foldable.html)
- [FAQ](https://jsuarezruiz.github.io/Nova.Avalonia.Foldable/FAQ.html)
- [Source and samples](https://github.com/jsuarezruiz/Nova.Avalonia.Foldable)

## Feedback

Bug reports and results from real foldables are welcome in the [issues](https://github.com/jsuarezruiz/Nova.Avalonia.Foldable/issues). MIT licensed.
