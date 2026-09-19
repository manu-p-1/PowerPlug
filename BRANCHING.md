# Branching strategy

PowerPlug ships as a NuGet package and a PowerShell Gallery module. This document describes how branches and version tags map to releases, so contributors know where to send a pull request and users know what to expect from each release channel.

## Branches

- **`master`** is the only branch releases are cut from. Every tag that gets published to NuGet and the PowerShell Gallery, whether it is an alpha, a beta, an rc or a final release, points at a commit on `master`. Nothing is ever published from any other branch.
- **`experimental`** is where the next release is put together. It is the default branch for pull requests and can contain unfinished or breaking work between releases. Treat it as unstable; do not build on top of it in production.
- **`feature/<name>`** and **`fix/<name>`** are short lived branches for a single change. Branch from `experimental`, and open the pull request back against `experimental`.
- **`hotfix/<version>`** branches from `master` for an urgent fix to something already released. It merges back into both `master` (to ship the fix) and `experimental` (so the fix is not lost on the next release).

```mermaid
gitGraph
    commit id: "1.0.0"
    branch experimental
    checkout experimental
    commit id: "feature A"
    commit id: "feature B"
    checkout master
    merge experimental id: "1.1.0-alpha1"
    checkout experimental
    commit id: "fix from beta feedback"
    checkout master
    merge experimental id: "1.1.0-beta1"
    checkout master
    commit id: "1.1.0-rc1"
    commit id: "1.1.0"
```

## Contributing

1. Fork the repository and branch from `experimental`.
2. Open the pull request against `experimental`, not `master`. Maintainers control when and how `experimental` moves to `master`.
3. Everything else in [CONTRIBUTING.md](CONTRIBUTING.md), tests, help file, changelog entry, still applies.

You do not need to think about version numbers or release channels when contributing. That is handled when `experimental` is merged into `master`.

## Release channels

A release does not live on its own branch. It is a sequence of tags on `master` as a version moves through alpha, beta, rc and final:

| Tag suffix | Example | Meaning for users |
| --- | --- | --- |
| `-alpha<N>` | `1.1.0-alpha1` | First preview of a new version. The cmdlet surface can still change. Install with `Install-Module PowerPlug -AllowPrerelease`. |
| `-beta<N>` | `1.1.0-beta1` | The feature set is frozen. Only fixes land from here on. Still requires `-AllowPrerelease`. |
| `-rc<N>` | `1.1.0-rc1` | Believed ready. Exists to catch platform specific problems before the version number is final. Still requires `-AllowPrerelease`. |
| none | `1.1.0` | The default install channel. Follows semantic versioning; see the note on experimental cmdlets below. |

Not every release needs every stage. A one line fix can go from `experimental` straight to a patch release on `master` without an alpha or beta in between. Alpha, beta and rc exist for releases large enough to need feedback before the number is permanent, the way [1.0.0-rc.1](CHANGELOG.md) was used for the rewrite.

Version numbers follow [Semantic Versioning](https://semver.org/), and cmdlets marked `[ExperimentalCmdlet]` are exempt from the usual breaking change rules; see the README for what "experimental" means for a cmdlet.

### Tag format

Use no separator between the stage name and its number (`alpha1`, not `alpha.1` or `alpha-1`). The PowerShell Gallery manifest's `Prerelease` field cannot contain a dot, so keeping the same format in git tags, the NuGet package version and the manifest avoids a mismatch between them.

### Cutting a release

1. Merge `experimental` into `master`.
2. Update `PowerPlugVersion` in `Directory.Build.props` and `CHANGELOG.md`.
3. Tag the commit on `master`, for example `git tag v1.1.0-alpha1`.
4. Push the tag, then publish the built package to NuGet and `dist/PowerPlug` to the PowerShell Gallery.
5. Repeat from step 2 with the next stage until a final tag with no suffix ships.

Only tags on `master` are valid release triggers. A tag created on any other branch should be deleted rather than published.
