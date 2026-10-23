---
title: Getting started
description: Add Nova.Avalonia.Foldable to an Avalonia app and show two panes around the fold.
---

# Getting started

Follow these steps to reference the library, prepare the iOS and Android heads, and place a `TwoPaneView` in a view.

## Prerequisites

- .NET 10 SDK 10.0.402 or later, with the `ios` and `android` workloads for mobile heads (the `ios` workload must be 27.1 or later)
- Avalonia 12.1.4 or later
- Xcode 27.1 for iOS work

## Reference the library

There's no NuGet package yet, so reference the project from your shared UI project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/src/Nova.Avalonia.Foldable/Nova.Avalonia.Foldable.csproj" />
</ItemGroup>
```

The library targets `net10.0`, `net10.0-ios27.1` and `net10.0-android`. The shared project references it once, and each head picks up its platform's provider.

## Prepare the iOS head

Target iOS 27.1, the first version of .NET for iOS with the iPhone Duo APIs. A head with a lower target version still builds, but picks up the library's `net10.0` build, which reports no fold:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-ios27.1</TargetFramework>
</PropertyGroup>
```

Keep `SupportedOSPlatformVersion` as it is. On iOS versions before 27.1 the app still runs and reports no fold.

## Prepare the Android head

Let the activity handle configuration changes, so it isn't recreated when the device folds and the app keeps its state:

```csharp
[Activity(
    MainLauncher = true,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity { }
```

## Show two panes

Every public type lives in the `Nova.Avalonia.Foldable` namespace, so one XAML namespace covers everything. Declare it and give a `TwoPaneView` a view for each pane, as in [Create a two-pane view](twopaneview.md#create-a-two-pane-view).

On a half-open device the two views sit on either side of the fold. On a regular phone the default size rules stack them, because most phones are taller than `MinTallModeHeight`. Raise that value to show one pane there instead. See [TwoPaneView](twopaneview.md) for the full set of rules.

## Try it without a device

The Showcase's [desktop app](samples.md#showcase) hosts the app in a foldable simulator, and [Testing without a device](testing.md) explains how to use the same simulated fold in your own app and tests.
