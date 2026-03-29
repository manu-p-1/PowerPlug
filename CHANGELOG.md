# Changelog

All notable changes to PowerPlug will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.0] - 2026-03-29

### Added
- **Invoke-Retry** (`retry`) — Retry a script block with configurable max attempts, delay, and exponential backoff.
- **Measure-ScriptBlock** (`msb`) — Multi-iteration benchmarking with min, max, average, median, and standard deviation.
- **ConvertTo-HashTable** (`toht`) — Recursively convert PSObject/PSCustomObject to an ordered hashtable.
- **Test-Port** (`tp`) — Cross-platform TCP port connectivity test with latency reporting.
- **ConvertTo-Base64** (`tobase64`) — Encode strings or file contents to Base64 with encoding options.
- **ConvertFrom-Base64** (`frombase64`) — Decode Base64 strings to plaintext or write decoded bytes to a file.
- **Get-EnvironmentPath** (`gpath`) — List PATH entries with existence validation.
- **New-RandomString** (`nrs`, `randstr`) — Generate cryptographically secure random strings.
- **New-TemporaryDirectory** (`ntd`) — Create uniquely named temporary directories.
- `PowerPlugCmdletBase` — Shared base class for all PowerPlug cmdlets with automatic beta warning emission.
- Shared `CmdletUtilities.ResolvePath()` for consistent path resolution across all file-based cmdlets.
- `CopyLocalLockFileAssemblies` in `.csproj` to ensure dependency DLLs are emitted alongside `PowerPlug.dll`.
- `SupportsShouldProcess` on all destructive cmdlets (`New-Byname`, `Set-Byname`, `Remove-Byname`, `Move-Trash`).
- Multi-target framework support: `net8.0` and `net10.0`.

### Changed
- Upgraded from .NET 5 to .NET 8 / .NET 10 multi-targeting.
- Upgraded Ampere dependency from 0.1.0 to 0.9.2.
- `Compare-Hash` now supports SHA384 and uses `Convert.ToHexString()` for hex conversion.
- `Move-Trash` now works cross-platform (Windows Recycle Bin, macOS Trash, Linux permanent delete with warning).
- `BetaCmdlet` attribute no longer writes directly to `Console`; warnings go through the PowerShell pipeline.
- Byname cmdlets no longer duplicate function definitions when the function already exists in `$PROFILE`.
- `FunctionExistsInProfile` now uses regex with word boundaries instead of naive string matching.
- Byname removal regex operations now have a 5-second timeout to prevent ReDoS.
- `CmdletUtilities.InvokePowershellCommandOrThrowIfUnsuccessful` now checks `Error.Count > 0` before indexing.
- Trash collision counter bounded to 10,000 to prevent infinite loops.
- `launchSettings.json` uses `pwsh` instead of hardcoded Windows path.

### Fixed
- **Newline accumulation bug** — `BynameRemover.Remove()` now trims trailing whitespace before appending a single newline, preventing blank lines from growing on each `Set-Byname` / `Remove-Byname` invocation.
- Null dereference in `WritableBynameCreatorBaseOperation.GetAliasValueType()` when `ResolvedCommand` is null.
- `Profile.ProfileExists()` now handles null/empty results from `Test-Path $PROFILE`.
- `Profile.GetProfile()` validates the profile path before constructing a `Profile` instance.

### Removed
- Dependency on `Microsoft.VisualBasic` (Windows Recycle Bin uses it only when running on Windows).
- Hardcoded `C:\Users\Manu\...` documentation output path from `.csproj`.
- Manual `-WhatIf` / `-Confirm` parameters from `WritableByname` (now handled by `SupportsShouldProcess`).

## [0.1.0] - 2021-01-01

### Added
- Initial release with `Compare-Hash`, `Move-Trash`, `New-Byname`, `Set-Byname`, `Remove-Byname`.
