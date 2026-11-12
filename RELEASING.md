# Releasing Nova.Avalonia.Foldable

Releases are created from version tags on `main`. The tag is the source of truth for the NuGet and assembly version, so no workflow or project file needs to be edited for each release. One release publishes both packages, `Nova.Avalonia.Foldable` and `Nova.Avalonia.Foldable.Simulator`, with the same version.

## One-time setup

Packages are published with nuget.org [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing), so there's no API key or repository secret. The release workflow exchanges GitHub's OIDC token for a nuget.org key that lasts one hour, right before it pushes.

On nuget.org, under your account's **Trusted Publishing**, a policy must exist with:

| Field | Value |
| --- | --- |
| Package owner | `jsuarezruiz` |
| Repository owner | `jsuarezruiz` |
| Repository | `Nova.Avalonia.Foldable` |
| Workflow file | `release.yml` |
| Environment | empty |
| Scopes | Push new packages and package versions |
| Glob patterns | `Nova.Avalonia.Foldable*` |

The policy is tied to the repository's GitHub ID, so renaming the workflow file, or deleting and recreating the repository, needs the policy updated. The `user` input of the `NuGet/login` step in `release.yml` is the nuget.org profile name, not an email address.

## Create a release

Make sure the intended commit is on `main` and its build has passed. Then create an annotated tag and push it together with `main`:

```bash
git switch main
git pull --ff-only
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin main v1.0.0
```

Pushing both at once keeps the documentation site, which deploys on every push to `main`, from announcing a version before its packages are on their way.

Release tags use the `vMAJOR.MINOR.PATCH` format, or `vMAJOR.MINOR.PATCH-PRERELEASE` for previews, such as `v1.1.0-preview.1`. A tag with a prerelease suffix publishes prerelease packages and a GitHub prerelease that isn't marked as the latest release.

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
./build/pack-nuget.sh 1.0.0
```

Packing needs the `ios` and `android` workloads, and the script always packs every target, even when `DesktopOnly=true` is set. The workflows pin workload set `10.0.402.1`, which has the iOS 27.1 workload the `net10.0-ios27.1` target needs; later sets replace 27.1 with 27.2 and fail to build that target. Install the same set locally with `dotnet workload install ios android --version 10.0.402.1`, and update the pin in both workflows when the library moves to a newer iOS target. It requires a clean working tree so Source Link refers to the exact source used to build the packages. Set `ALLOW_DIRTY=1` only for local validation artifacts that will not be published. The packages go to `artifacts/packages/<version>` unless a second argument names another directory.
