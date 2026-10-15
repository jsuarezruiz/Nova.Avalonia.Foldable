# FAQ

## General

### Which devices does it support?

Any foldable whose platform exposes the fold:

- The iPhone Duo, on iOS 27.1 or later.
- Android foldables with Jetpack WindowManager support, such as the Pixel Fold and the Galaxy Z Fold and Flip series.

Everywhere else (regular phones, tablets, desktop, browser) the library reports `FoldState.None` and `TwoPaneView` behaves like a size-based two-pane control.

### Do I need separate code per platform?

No. You write against `Fold`, `FoldState` and `TwoPaneView` in your shared project. The iOS and Android implementations are picked automatically by target framework.

### Does it work with the Avalonia previewer and on desktop?

Yes. There's no fold there, so you get the size-based layout. To design fold-aware layouts, set a `SimulatedFoldStateProvider` with `Fold.SetProvider(...)`. The Showcase's desktop head includes a simulator panel that does exactly that.

### Is it on NuGet?

Not yet. Reference the projects for now. Packages are on the roadmap.

## iOS

### Why does the repo contain iOS bindings?

The iPhone Duo APIs (`UIHingeInteraction`, `UIHinge`, `-[UIView reservedRegionsOfKind:]`) are new in the iOS 27.1 SDK, and .NET for iOS doesn't include them yet. [The temporary bindings](docs/platforms.md#the-temporary-bindings) explains where the copy in this repo comes from and what happens when the official ones ship.

### Why `ValidateXcodeVersion=false`?

The .NET for iOS workload checks for the Xcode version it was built against, which is older than the Xcode 27.1 the iPhone Duo simulator needs. The bindings are resolved at runtime and don't depend on the Xcode version, and the samples build with Xcode 27.0 and 27.1. The samples turn the check off in `samples/Shared/iOS/iOSApp.props`. Build with `-p:ValidateXcodeVersion=true` to keep it.

### I get `Undefined symbols: _OBJC_CLASS_$_UIHinge` when linking

You're building against an SDK older than 27.1 without the bindings' `buildTransitive` targets. A future NuGet package will apply them automatically. With project references, import `Nova.Avalonia.Foldable.Bindings.iOS.targets` in your iOS head, as [Prepare the iOS head](docs/getting-started.md#prepare-the-ios-head) shows.

### Does the app still run on iOS versions before 27.1?

Yes. The new classes are resolved at runtime, and on older iOS the provider reports no fold.

### The fold is 40pt wide. Is that right?

Yes, that's how the iPhone Duo reports its fold. [Platforms](docs/platforms.md#ios) has the numbers. `TwoPaneView` keeps both panes out of it.

### My content is smaller than the display on the Duo

That's Avalonia 12's automatic safe-area padding (`TopLevel.AutoSafeAreaPadding`, on by default), e.g. `0,0,84,34` around the Duo's sensor edge. `FoldBounds` is in top-level coordinates and `TwoPaneView` maps it into its own, so fold-aware layouts stay correct.

### I can't find an iPhone Duo simulator

It depends on the simulator runtime you have installed. [The iPhone Duo simulator](docs/platforms.md#the-iphone-duo-simulator) lists what to install and where the posture buttons are.

### The fold stopped being reported on the Duo simulator

Shut the simulator down and boot it again. A session that has been running for a long time can stop reporting reserved regions (the fold and the camera housing) while the hinge still works.

### `simctl io screenshot` gives a black image on the Duo

The Duo has several display ports and the default one is blank. Pass the display explicitly (`xcrun simctl io <device> enumerate` lists them). The inner and cover displays are separate, and only one recording can run at a time.

## Android

### Do I need to write Java or Kotlin?

No. Everything is C#. The library uses Microsoft's .NET bindings for Jetpack WindowManager (`Xamarin.AndroidX.Window` and `Xamarin.AndroidX.Window.WindowJava`). "WindowJava" is the name of the callback-style variant of the API, which maps better to C# than Kotlin flows. It isn't Java code you write.

### Why is `HingeAngle` null on my Android device?

WindowManager doesn't report a hinge angle. The library reads it from the `TYPE_HINGE_ANGLE` sensor, which needs Android 11 (API 30) or later, and which some foldables don't expose to apps. Posture and fold bounds still work.

### The build fails with `JAVA0000 … defined multiple times`

It's an AndroidX version mismatch. WindowManager pulls AndroidX Core 1.19 (which absorbed `core-ktx`) while another dependency brings an older `Xamarin.AndroidX.Core.Core.Ktx`. The library references `Core.Ktx` 1.19 to align them. If your app pins an older version, update it.

### My Debug APK closes immediately with "No assemblies found … Fast Deployment"

Debug builds leave the assemblies out of the APK and deploy them from the IDE. If you install with `adb`, set `<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>` in the Android head (the samples do).

### What's the best Android emulator for testing?

The **Pixel Fold** profile on the **Android 14 (API 34)** image. [The Pixel Fold emulator](docs/platforms.md#the-pixel-fold-emulator) has the setup and the commands that fold it.

### Why not the "7.6in Foldable" emulator profile?

It reports postures shifted by one (closed → half open, half open → flat, open → no fold). Its device-state ids start at 1, while the WindowManager extension's posture table expects them to start at 0, as on Pixel devices. The library reports what WindowManager sends, so the result is wrong on that profile only.

### The emulator crashes on boot

On macOS 27 with emulator 36.2.12, the **Android 36.1** image crashed on first boot for every foldable profile (look for `mprotect failed: Permission denied` in the log). The **Android 34** image works. Also make sure the AVD has `hw.gpu.enabled=yes`.

### The app doesn't move to the cover screen when I close the Pixel Fold emulator

That's an emulator limitation on this setup, where the dual-display switch doesn't happen. The library still reports `Closed` (from the hinge sensor). On a real device the app moves to the cover screen and `TwoPaneView` switches to one pane.

### `screencap` returns text instead of an image

The Pixel Fold has two displays, so pass one. `adb shell dumpsys SurfaceFlinger --display-id` lists them, and `adb exec-out screencap -d <id> -p > shot.png` captures the one you pick.

## TwoPaneView

### Why does my second pane disappear on one screen?

In single-pane mode the pane that isn't shown is hidden, as WinUI collapses it. It stays in the visual tree with its state and comes back as it was when the device unfolds. Use `PanePriority` to choose which one is shown.

### A `TransitioningContentControl` shows nothing after it comes back

That's an Avalonia issue in 12.1.2 to 12.1.4. After a `CrossFade`, a `TransitioningContentControl` that is removed from the visual tree and added back can show its content in the presenter it faded out, which is still transparent. `TwoPaneView` only hides a pane, so its panes aren't affected. If your app removes such a control itself, hide it with `IsVisible` instead, or use a transition that doesn't leave the faded-out page transparent.

### Can I use the hinge angle for layout?

Please don't, since not every Android foldable exposes it. [Hinge angle](docs/fold.md#hinge-angle) has what to use instead.
