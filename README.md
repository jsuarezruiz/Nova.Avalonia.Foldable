# Nova.Avalonia.Foldable

Foldable device support for [Avalonia](https://avaloniaui.net). It tells your app how the device is folded (posture, hinge angle and where the fold is) on the iPhone Duo and Android foldables, and includes a `TwoPaneView` that places your content on either side of the fold.

<p align="center"><img src="images/banner.png" alt="Nova.Avalonia.Foldable" /></p>

On devices without a fold nothing changes: the library reports no fold and `TwoPaneView` falls back to size-based layout, so the same app runs on phones, tablets, desktop and the web.

<p align="center"><img src="images/iphone-duo-showcase.gif" width="672" alt="The Showcase spanning both screens of the iPhone Duo simulator" /></p>

*The Showcase app, "Guide To the Stars" from [Avalonia Vignettes](https://github.com/jsuarezruiz/avalonia_vignettes), on the iPhone Duo simulator.*

## Platform support

| Platform | Fold source | Tested on |
| --- | --- | --- |
| iOS 27.1+ | `UIHingeInteraction` and the view's division region | iPhone Duo simulator |
| Android 6.0+ (API 23) | Jetpack WindowManager and the hinge angle sensor | Pixel Fold emulator |
| Desktop, browser | None, reports no fold | |

The iOS 27.1 APIs aren't in .NET for iOS yet. They're being added in [dotnet/macios#26705](https://github.com/dotnet/macios/pull/26705), and until that ships this repo includes [`Nova.Avalonia.Foldable.Bindings.iOS`](src/Nova.Avalonia.Foldable.Bindings.iOS), a copy of that PR's definitions. On iOS versions before 27.1 the app still runs and reports no fold. Android uses Microsoft's `Xamarin.AndroidX.Window` packages, so it needs no custom bindings.

## Getting started

There's no NuGet package yet, so reference the project from your shared UI project:

```xml
<ProjectReference Include="path/to/src/Nova.Avalonia.Foldable/Nova.Avalonia.Foldable.csproj" />
```

In your iOS head, turn off the Xcode version check (.NET for iOS still expects Xcode 26.2) and import the bindings' link settings:

```xml
<PropertyGroup>
  <ValidateXcodeVersion>false</ValidateXcodeVersion>
</PropertyGroup>
<Import Project="path/to/src/Nova.Avalonia.Foldable.Bindings.iOS/buildTransitive/Nova.Avalonia.Foldable.Bindings.iOS.targets" />
```

In your Android head, let the activity handle configuration changes so it isn't recreated when the device folds:

```csharp
[Activity(
    MainLauncher = true,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : AvaloniaMainActivity { }
```

## Usage

### Two panes around the fold

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

`TwoPaneView` has the same API as the WinUI and .NET MAUI control. When the device is half open, the panes sit on either side of the fold, or above and below it in tabletop posture. When it's flat, they split at the fold if there's room (`SnapToFold`, on by default). When it's closed or has no fold, the usual size rules apply (`MinWideModeWidth` and `MinTallModeHeight`, 641 by default), and `PanePriority` picks the pane to show when only one fits. The hidden pane keeps its state.

### Reading the fold

`TwoPaneView` tracks the fold on its own. Anywhere else, set `Fold.IsEnabled` near the root. Every control below it then inherits the fold through attached properties: `Fold.State`, `Posture` (`Unknown`, `Closed`, `HalfOpened` or `Flat`), `HingeAngle`, `IsSeparating`, `FoldOrientation` and `IsFoldable`.

```xml
<UserControl xmlns:fold="using:Nova.Avalonia.Foldable"
             fold:Fold.IsEnabled="True">
  <UserControl.Styles>
    <Style Selector="Border.controls[(fold|Fold.Posture)=HalfOpened]">
      <Setter Property="Opacity" Value="0.85" />
    </Style>
  </UserControl.Styles>

  <TextBlock Text="{Binding $self.(fold:Fold.Posture)}" />
</UserControl>
```

Style selectors separate the namespace with `|`, bindings with `:`. In code, watch for property changes:

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == Fold.StateProperty && Fold.GetState(this) is { IsTabletop: true } state)
        ShowControlsBelow(state.FoldBounds); // top-level coordinates
}
```

Use the hinge angle for effects only. Not every device reports it, so base layout on the posture and the fold bounds.

### Without a foldable

A `SimulatedFoldStateProvider` lets you design and test fold layouts on desktop, in the previewer and in headless tests:

```csharp
var fold = new SimulatedFoldStateProvider();
Fold.SetProvider(window, fold);

fold.Current = new FoldState(
    IsFoldable: true,
    Posture: DevicePosture.HalfOpened,
    HingeAngle: 110,
    FoldBounds: new Rect(380, 0, 40, 860),
    IsSeparating: true);
```

The Showcase's desktop app wraps it in a simulator panel:

<p align="center"><img src="docs/images/desktop-simulator.png" width="720" alt="Showcase with the desktop foldable simulator" /></p>

## Samples

- [**Sample**](samples/Nova.Avalonia.Foldable.Sample) (iOS, Android) shows every value the library reports and draws the fold over a two-pane layout. Start here when testing a device.
- [**Showcase**](samples/Nova.Avalonia.Foldable.Showcase) (iOS, Android, desktop) is the Guide To the Stars vignette adapted to two screens.

```bash
dotnet run --project samples/Nova.Avalonia.Foldable.Showcase/Nova.Avalonia.Foldable.Showcase.Desktop
```

## Testing on simulators and emulators

**iPhone Duo:** install Xcode 27.1 with the iOS 27.1 simulator runtime (the iOS 27.2 beta 1 runtime doesn't support the Duo), then run an iOS head on the iPhone Duo simulator. Change postures with the buttons in Device Hub.

**Pixel Fold:** create a Pixel Fold emulator on the Android 14 (API 34) image with the GPU enabled, and give it a fold position, since the image doesn't define one:

```bash
adb shell settings put global display_features "fold-[1104,0,1104,1840]"
```

Then fold it with `adb emu posture 1|2|3` (closed, half open, open) and move the hinge with `adb emu sensor set hinge-angle0 <degrees>`.

The [FAQ](docs/FAQ.md) covers emulator quirks and other common problems.

## Limitations

- The iOS bindings are temporary. Once .NET for iOS ships them, the library switches to the official types and apps don't need to change.
- The hinge angle is only reported where the platform exposes it; on Android the device needs a hinge sensor.
- Avalonia 12 pads the content for safe areas, such as the Duo's sensor edge. `TwoPaneView` accounts for it, but keep it in mind when you position things against `FoldBounds` yourself.

Next on the list: NuGet packages, the official iOS bindings, camera housing regions, and real Auto lengths and animated mode changes in `TwoPaneView`.

## Contributing and license

Contributions are welcome, and reports from real foldables are especially useful. See [CONTRIBUTING.md](CONTRIBUTING.md) and the [FAQ](docs/FAQ.md).

MIT licensed, see [LICENSE](LICENSE). Third-party code and assets are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
