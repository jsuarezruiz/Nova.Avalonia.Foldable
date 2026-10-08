# Contributing to Nova.Avalonia.Foldable

Thanks for helping. Bug reports, device reports, docs and code are all welcome.

## Before you start

- **Found a bug?** Open an issue with the device or emulator, OS version, what you expected and what you got. The most useful thing you can attach is the output of the [Sample](samples/Nova.Avalonia.Foldable.Sample): it prints every value the library reports.
- **Tested on a real foldable?** Please share it, even if everything worked. Real-device reports (Galaxy Fold, Pixel Fold, iPhone Duo, …) are the most valuable contribution right now.
- **Planning a larger change** (a new provider, an API change, a new control)? Open an issue first so we can agree on the shape before you spend time on it.

## Setting up

Requirements:

- .NET 10 SDK with the `ios` and `android` workloads (the iOS workload must be 26.2 or later)
- Xcode for iOS work (Xcode 27.1 for the iPhone Duo simulator)
- The Android SDK and an emulator for Android work

```bash
dotnet test tests/Nova.Avalonia.Foldable.Tests
dotnet run --project samples/Nova.Avalonia.Foldable.Showcase/Nova.Avalonia.Foldable.Showcase.Desktop
```

Without the `ios` and `android` workloads (on Linux, for example), set `DesktopOnly=true` to build and test only the `net10.0` target:

```bash
DesktopOnly=true dotnet test tests/Nova.Avalonia.Foldable.Tests
```

To preview the documentation site, restore the pinned DocFX (`dotnet tool restore`) and run:

```bash
dotnet docfx docs/docfx.json --serve
```

[Platforms](docs/docs/platforms.md) explains how to run the samples on the iPhone Duo simulator and the Pixel Fold emulator.

## Making changes

1. Fork, then create a branch from `main`.
2. Keep changes focused: one fix or feature per pull request.
3. Match the surrounding code: naming, comment density, file layout. The repository uses `Nullable` everywhere; keep new code warning-free.
4. Add or update tests. Layout logic belongs in the headless tests under `tests/`, driven by `SimulatedFoldStateProvider`, so it can be checked without a device.
5. If you touch a platform provider, run the Sample on that platform and include what it reported in the pull request (a screenshot or the `FOLDDIAG` log lines).
6. Update the README, the documentation site or the FAQ when behavior changes.

### Where things live

| Area | Location |
| --- | --- |
| Core API (`Fold`, `FoldState`, `DevicePosture`, `FoldOrientation`) | `src/Nova.Avalonia.Foldable` |
| `TwoPaneView` and its enums | `src/Nova.Avalonia.Foldable/Controls/TwoPaneView` |
| Fold sources: the `IFoldStateProvider` interface, the no-fold fallback and the simulated fold for tests | `src/Nova.Avalonia.Foldable/Providers` |
| iOS implementation | `src/Nova.Avalonia.Foldable/Platforms/iOS` |
| Android implementation | `src/Nova.Avalonia.Foldable/Platforms/Android` |
| iOS 27.1 bindings (mirror dotnet/macios#26705) | `src/Nova.Avalonia.Foldable.Bindings.iOS` |
| Tests | `tests/Nova.Avalonia.Foldable.Tests` |
| Documentation site (DocFX) and FAQ | `docs` |

Folders are for navigation only: every public type stays in the `Nova.Avalonia.Foldable` namespace, so apps need one `using` and one XAML `xmlns`.

### A few rules of thumb

- **Coordinates:** providers report `FoldBounds` in top-level DIPs. Convert platform units (points, pixels) at the provider boundary, never in consumers.
- **Threads:** providers raise `Changed` on the UI thread.
- **No fold is not an error:** return `FoldState.None` (or a state with `IsFoldable = false`) instead of throwing, so apps run unchanged on every device.
- **Hinge angle is for effects:** don't make layout depend on it; not every device exposes it.
- **iOS bindings:** keep them identical to dotnet/macios#26705, so switching to the official ones stays a namespace change. Don't add convenience members there.
- **Android interop:** objects coming through generic callbacks or interface-typed lists need `JavaCast<T>()` / `Class.IsInstance`, not C# type tests.

## Pull requests

- Describe what changed and why, and how you tested it (which device, emulator or simulator).
- Keep commit messages short and descriptive.
- Make sure `dotnet test` passes and the projects you touched build.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
