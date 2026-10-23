---
title: Platforms
description: How the iOS and Android providers read the fold, and how to test on the iPhone Duo simulator and the Pixel Fold emulator.
---

# Platforms

Each platform has its own provider, picked automatically by target framework. Your code only sees [FoldState](xref:Nova.Avalonia.Foldable.FoldState). So far the providers have been tested on the iPhone Duo simulator and the Pixel Fold emulator.

## iOS

The iOS provider attaches a `UIHingeInteraction` to the Avalonia root view for the hinge status and angle, and reads the view's **division** reserved region for the fold. UIKit points map 1:1 to Avalonia's device-independent pixels (DIPs).

- The iPhone Duo reports the fold as a 40pt band, for example `x=455.5 w=40` on the 951×669 inner display. `TwoPaneView` keeps both panes out of it.
- A closed iPhone Duo reports no division region at all. Only the hinge knows the device folds.
- When the app moves between the cover and the inner display, UIKit updates the division region a moment after resizing the app, and can briefly report the inner display's fold turned sideways on the cover display. The provider reads the region again shortly after each resize and hinge update, and ignores regions that don't fit on the screen.
- **Occlusion** regions are ignored, since they also report the camera housing on every iPhone.
- On iOS versions before 27.1 the app runs normally and reports no fold.

### Build requirements

The provider uses the `UIKit` bindings for the iOS 27.1 APIs, which first ship in the .NET for iOS 27.1 workload (.NET SDK 10.0.402 with workload set 10.0.402.1). That workload needs Xcode 27.1.

The library's iOS target is `net10.0-ios27.1`, and the iOS head needs 27.1 or later too. With the 27.1 workload a head that targets `net10.0-ios` gets 27.1. A head pinned lower, such as `net10.0-ios26.0`, still builds, but NuGet picks the library's `net10.0` build, which reports no fold. The samples set `net10.0-ios27.1` to make the requirement explicit. `SupportedOSPlatformVersion` can stay lower. On iOS versions before 27.1 the new classes are missing, and the provider reports no fold.

### The iPhone Duo simulator

1. Install **Xcode 27.1** with the **iOS 27.1** simulator runtime. The iOS 27.2 beta 1 runtime lists the iPhone Duo (`iPhone19,4`) as unsupported.
2. Build an iOS head against the Xcode 27.1 SDK, setting `MD_APPLE_SDK_ROOT` if another Xcode is selected.
3. Run it on the **iPhone Duo** simulator, and change postures with the buttons at the bottom of **Device Hub**, which replaces the Simulator app in Xcode 27.

## Android

The Android provider runs on Android 6.0 (API 23) and later. It listens to Jetpack WindowManager through Microsoft's `WindowInfoTrackerCallbackAdapter` and maps the window's `FoldingFeature`:

- `HALF_OPENED` becomes `HalfOpened`, and `FLAT` becomes `Flat`.
- The bounds are converted from window pixels to top-level DIPs.
- `IsSeparating` comes straight from WindowManager.

WindowManager has no hinge angle, so the provider reads the `TYPE_HINGE_ANGLE` sensor on Android 11 (API 30) and later, where the device has one, and pauses it while the app is in the background. With no folding feature in the window, as on the cover screen, the posture comes from the sensor, `Closed` below 30° and `Unknown` above. Without a sensor there is no way to tell a closed device from a window that simply doesn't span the fold, so the posture is `Unknown`, and it is also `Unknown` until WindowManager or the sensor has reported.

### The Pixel Fold emulator

1. Create a **Pixel Fold** emulator on the **Android 14 (API 34)** image with the GPU enabled.
2. Give it a fold position, since the image doesn't define one:

   ```bash
   adb shell settings put global display_features "fold-[1104,0,1104,1840]"
   ```

3. Fold it with `adb emu posture 1|2|3` (closed, half open, open), move the hinge with `adb emu sensor set hinge-angle0 <degrees>`, and read the Sample's output with `adb logcat -s DOTNET`.

Debug builds installed with `adb` need `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` in the Android head, as the samples do. The [FAQ](../FAQ.md) covers emulator images that crash on boot and the "7.6in Foldable" profile, which reports postures shifted by one.

## Desktop and browser

There is no fold, so the provider reports `FoldState.None` and `TwoPaneView` uses its size rules. To design fold layouts there, use a [simulated fold](testing.md).
