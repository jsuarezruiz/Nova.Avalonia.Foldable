# Releasing Nova.Avalonia.Foldable

Releases are created from version tags on `main`. The tag is the source of truth for the NuGet and assembly version, so no workflow or project file needs to be edited for each release. One release publishes both packages, `Nova.Avalonia.Foldable` and `Nova.Avalonia.Foldable.Simulator`, with the same version.

## One-time setup

1. On nuget.org, create an API key with the **Push new packages and package versions** scope and the glob pattern `Nova.Avalonia.Foldable*`. The key used by Nova.Avalonia.UI only covers `Nova.Avalonia.UI*`, so it can't push these packages.
2. Add it to this repository as an Actions secret named `NUGET_API_KEY`, either under **Settings > Secrets and variables > Actions**, or with:

   ```bash
   gh secret set NUGET_API_KEY --repo jsuarezruiz/Nova.Avalonia.Foldable
   ```

   `gh` asks for the value, so the key doesn't end up in your shell history.

## Create a release

Make sure the intended commit is on `main` and its build has passed. Then create an annotated tag and push it together with `main`:

```bash
git switch main
git pull --ff-only
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin main v0.1.0
```

Pushing both at once keeps the documentation site, which deploys on every push to `main`, from announcing a version before its packages are on their way.

Release tags use the `vMAJOR.MINOR.PATCH` format, or `vMAJOR.MINOR.PATCH-PRERELEASE` for previews, such as `v0.1.0-preview.1`. A tag with a prerelease suffix publishes prerelease packages and a GitHub prerelease that isn't marked as the latest release.

Pushing the tag starts the release workflow. Its first job checks formatting and runs the tests on Linux. The second job runs on macOS, because the library also targets `net10.0-ios27.1` and `net10.0-android` and packing needs the `ios` and `android` workloads. It verifies that the tag points to `main`, builds the desktop and Android samples, and packs both libraries with the version from the tag. If validation succeeds, it:

1. Prepares a draft GitHub Release with generated release notes and attaches the `.nupkg` and `.snupkg` files.
2. Publishes the NuGet and symbol packages to NuGet.org.
3. Publishes the GitHub Release after every NuGet package succeeds.

The publish command skips versions that already exist. If publishing only partially succeeds, the draft remains unpublished and a rerun reuses it, skips packages already on NuGet.org, and publishes the GitHub Release after the remaining packages succeed.

NuGet.org validates and indexes new packages before they can be installed, which usually takes a few minutes and can take longer for a package's first version.

## Build packages without releasing

Every build of `main` and every pull request packs both libraries with a `0.0.0-ci.<run>.<attempt>` version and uploads them as the `nuget-packages` artifact of the **Build and test** workflow.

To create packages locally, pass the version explicitly:

```bash
./build/pack-nuget.sh 0.1.0
```

Packing needs the `ios` and `android` workloads, and the script always packs every target, even when `DesktopOnly=true` is set. It requires a clean working tree so Source Link refers to the exact source used to build the packages. Set `ALLOW_DIRTY=1` only for local validation artifacts that will not be published. The packages go to `artifacts/packages/<version>` unless a second argument names another directory.
