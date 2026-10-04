---
title: Getting started
description: Add Nova.Avalonia.Foldable to an Avalonia app and show two panes around the fold.
ms.date: 2026-09-27
---

# Getting started

Follow these steps to reference the library, prepare the iOS and Android heads, and place a `TwoPaneView` in a view.

## Prerequisites

- .NET 10 SDK with the `ios` and `android` workloads for mobile heads
- Avalonia 12.1 or later
- Xcode for iOS work (Xcode 27.1 for the iPhone Duo simulator)

## Reference the library

There's no NuGet package yet, so reference the project from your shared UI project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/src/Nova.Avalonia.Foldable/Nova.Avalonia.Foldable.csproj" />
</ItemGroup>
```

The library targets `net10.0`, `net10.0-ios` and `net10.0-android`. The shared project references it once, and each head picks up its platform's provider.

## Prepare the iOS head

Turn off the Xcode version check, since .NET for iOS still expects Xcode 26.2, and import the bindings' link settings:

```xml
<PropertyGroup>
  <ValidateXcodeVersion>false</ValidateXcodeVersion>
</PropertyGroup>
<Import Project="path/to/src/Nova.Avalonia.Foldable.Bindings.iOS/buildTransitive/Nova.Avalonia.Foldable.Bindings.iOS.targets" />
```

The import drops the link-time references to the iOS 27.1 classes. Without it, linking against an older SDK fails with `Undefined symbols: _OBJC_CLASS_$_UIHinge`. The NuGet package will apply it automatically.

## Prepare the Android head

Let the activity handle configuration changes, so it isn't recreated when the device folds and the app keeps its state:

```csharp
[Activity(
    MainLauncher = true,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : AvaloniaMainActivity { }
```

## Show two panes

Every public type lives in the `Nova.Avalonia.Foldable` namespace, so one XAML namespace covers everything:

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

On a half-open device the inbox and the message sit on either side of the fold. On a regular phone only the inbox shows, and the message appears next to it once the window is wide enough. See [TwoPaneView](twopaneview.md) for the full set of rules.

## Try it without a device

Run the Showcase's desktop app, which hosts the app in a foldable simulator:

```bash
dotnet run --project samples/Nova.Avalonia.Foldable.Showcase/Nova.Avalonia.Foldable.Showcase.Desktop
```

[Testing without a device](testing.md) explains how to use the same simulated fold in your own app and tests.
