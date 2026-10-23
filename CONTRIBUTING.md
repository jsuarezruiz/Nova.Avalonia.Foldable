# Contributing to Nova.Avalonia.Foldable

Thanks for helping. Bug reports, device reports, docs and code are all welcome.

## Before you start

- **Found a bug?** Open an issue with the device or emulator, OS version, what you expected and what you got. The most useful thing you can attach is the output of the [Sample](samples/Nova.Avalonia.Foldable.Sample), which prints every value the library reports.
- **Tested on a real foldable?** Please share it, even if everything worked. Reports from real devices, such as a Galaxy Fold, Pixel Fold or iPhone Duo, help the most right now.
- **Planning a larger change** (a new provider, an API change, a new control)? Open an issue first so we can agree on the shape before you spend time on it.

## Setting up

Requirements:

- .NET 10 SDK with the `ios` and `android` workloads (the iOS workload must be 27.1 or later: .NET SDK 10.0.402 with workload set 10.0.402.1)
- Xcode 27.1 for iOS work
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
2. Keep changes focused, with one fix or feature per pull request.
3. Match the naming, comment density and file layout of the surrounding code. The repository uses `Nullable` everywhere, so keep new code warning-free.
4. Add or update tests. Layout logic belongs in the headless tests under `tests/`, driven by `SimulatedFoldStateProvider`, so it can be checked without a device.
5. If you touch a platform provider, run the Sample on that platform and include what it reported in the pull request (a screenshot or the `FOLDDIAG` log lines).
6. Update the README, the documentation site or the FAQ when behavior changes. The NuGet package has its own README, `src/Nova.Avalonia.Foldable/README.md`, with absolute links, since nuget.org can't follow relative ones.

### Where things live

| Area | Location |
| --- | --- |
| Core API (`Fold`, `FoldState`, `DevicePosture`, `FoldOrientation`) | `src/Nova.Avalonia.Foldable` |
| `TwoPaneView` and its enums | `src/Nova.Avalonia.Foldable/Controls/TwoPaneView` |
| The `IFoldStateProvider` interface, the no-fold fallback and the simulated fold for tests | `src/Nova.Avalonia.Foldable/Providers` |
| iOS implementation | `src/Nova.Avalonia.Foldable/Platforms/iOS` |
| Android implementation | `src/Nova.Avalonia.Foldable/Platforms/Android` |
| Tests | `tests/Nova.Avalonia.Foldable.Tests` |
| Settings, icons and launch screens the sample apps share | `samples/Shared` |
| Documentation site (DocFX) and FAQ | `docs` |

Folders are for navigation only. Every public type stays in the `Nova.Avalonia.Foldable` namespace, so apps need one `using` and one XAML `xmlns`.

### A few rules of thumb

- Providers report `FoldBounds` in top-level DIPs. Convert platform units (points, pixels) at the provider boundary, never in consumers.
- Providers raise `Changed` on the UI thread.
- No fold is not an error. Return `FoldState.None` (or a state with `IsFoldable = false`) instead of throwing, so apps run unchanged on every device.
- The hinge angle is for effects. Don't make layout depend on it, since not every device exposes it.
- On Android, objects coming through generic callbacks or interface-typed lists need `JavaCast<T>()` or `Class.IsInstance`, not C# type tests.

## Pull requests

- Describe what changed and why, and how you tested it (which device, emulator or simulator).
- Keep commit messages short and descriptive.
- Make sure `dotnet test` passes and the projects you touched build.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
