# Changelog

All notable changes to PowerPlug will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-rc.1] - 2026-09-12

This release is close to a rewrite, and it is not compatible with 0.9.x scripts that relied on the old output shapes or the `BetaCmdlet` attribute. That is why the number jumps to 1.0.0 rather than 0.10.0: from here on, changes to stable cmdlets follow semantic versioning and experimental ones are marked as such. The module has grown from 16 to 44 cmdlets, drops its last external dependency, gains a module manifest and a full test suite, and every cmdlet has help.

The release candidate exists to catch platform specific problems before the number is permanent. Install it with `Install-Module PowerPlug -AllowPrerelease`.

### Added

New cmdlets, grouped by area:

- Data and encoding: `ConvertTo-UrlEncoding`, `ConvertFrom-UrlEncoding`, `ConvertFrom-UnixTime`, `ConvertTo-UnixTime`, `ConvertFrom-Jwt`, `Convert-Color`
- Security: `Get-StringHash`, `Test-Elevation`
- File system: `Rename-BatchItem`, `Get-DirectorySize`, `Find-DuplicateFile`, `Remove-EmptyDirectory`, `Get-FileEncoding`, `Convert-LineEnding`, `Set-FileTimestamp`, `Get-LargestFile`, `Compare-Directory`, `Wait-File`
- Networking: `Wait-Port`, `Test-Url`, `Get-TlsCertificate`, `Get-PublicIPAddress`, `Get-ListeningPort`
- Shell: `Add-EnvironmentPath`, `Remove-EnvironmentPath`, `Import-DotEnv`, `Get-SystemInfo`
- Diagnostics: `Watch-Command`

Other additions:

- A module manifest (`PowerPlug.psd1`) and loader (`PowerPlug.psm1`) so the module can be published to the PowerShell Gallery and loads the right build for the running .NET version.
- A format file so tabular output (PATH entries, port checks, certificates and so on) reads well without piping to `Format-Table`.
- `PowerPlug.Tests`, an xUnit project with pure unit tests for the helpers, mocked runtime tests for every cmdlet, and integration tests that host a real PowerShell runspace to check binding, pipelines, aliases, help and the module manifest. Around 490 tests run on both target frameworks.
- `tools/New-HelpFile.ps1`, which generates `PowerPlug.dll-Help.xml` for every cmdlet from the XML doc comments. Previously only five cmdlets had help.
- A GitHub Actions workflow that builds and tests on Windows, macOS and Linux.
- Typed output classes under `PowerPlug.Models` for every cmdlet. Output used to be `PSObject` with note properties, which was hard to discover and impossible to format.
- `ConvertTo-Base64` / `ConvertFrom-Base64`: `-UrlSafe` output and transparent Base64Url decoding, `Latin1` encoding, `-Force` guard before overwriting a file.
- `Compare-Hash`: SHA1 support, signatures may contain dashes, colons or spaces, accepts `Get-ChildItem` output on the pipeline.
- `New-RandomString`: `-ExcludeAmbiguous` and `-CharacterSet`.
- `Move-Trash`: wildcards, pipeline input from `Get-ChildItem`, `-PassThru`, and a real FreeDesktop trash on Linux instead of permanent deletion.
- `Test-Port`: accepts several ports at once.
- `Get-EnvironmentPath`: flags duplicate entries.
- `Invoke-Retry`: `-MaxDelayMilliseconds`, `-Jitter`, `-ArgumentList`, and Ctrl+C now interrupts the wait.
- `Measure-ScriptBlock`: reports failed iterations, `-ArgumentList`, progress for long runs.
- `ConvertTo-HashTable`: handles dictionaries and nested arrays properly when recursing.

### Changed

- **Targets .NET 8 and .NET 10.** PowerShell 7.4 or later is required.
- **Ampere has been removed.** The two helpers it provided (`StringBuilder.AppendIf` and `FileUtils.WriteLine`) are replaced with plain .NET code.
- `BetaCmdlet` is now `ExperimentalCmdlet` and is applied only where it means something: the Byname cmdlets, `Move-Trash`, `Get-Speed`, `Get-ListeningPort`, `Add-EnvironmentPath`, `Remove-EnvironmentPath` and `Watch-Command`. Every other cmdlet is treated as stable and no longer warns. The warning text names the cmdlet and explains why.
- The Byname internals (a strategy pattern spread across nine files) collapsed into `ProfileAliasWriter` plus two small base classes. Behaviour is the same, but the profile lines are now quoted correctly when a value contains spaces or quotes, `$PROFILE` is created if missing, and removal handles quoted values and nested function bodies.
- Byname cmdlets read `$PROFILE` from session state directly instead of spawning a nested PowerShell to evaluate `Test-Path $PROFILE`. Function definitions PowerPlug writes are tagged with a `# PowerPlug Byname` comment; `Remove-Byname` and `Set-Byname` only ever remove tagged functions, and only when no other persisted alias still points at them. Hand written functions in your profile are never touched.
- New aliases avoid names of common native tools. `Get-DirectorySize` is `dirsize` (not `du`), `Watch-Command` is `watchcmd` (not `watch`), `Rename-BatchItem` is `Rename-Batch`. The pre-existing `trash` and `speedtest` aliases are kept for compatibility.
- Path parameters resolve through the PowerShell provider (`GetUnresolvedProviderPathFromPSPath`), so PSDrive paths and `~` work everywhere. A wildcard that matches nothing is reported as an error rather than silently doing nothing, and a piped path that names an existing file is used as-is even when it contains bracket characters.
- `Move-Trash` refuses to move items across volumes (for example from an external drive) with a message pointing at `Remove-Item`, rather than copying gigabytes into the home trash.
- `TextEncodings.Parse` rejects unknown encoding names instead of silently returning UTF-8.
- Networking cmdlets share one `HttpClient`, honour Ctrl+C through `StopProcessing`, and never follow redirects silently.
- `Get-Speed` reports packet loss as a number, jitter over the raw sample order, and falls back to TCP latency when ICMP is unavailable without pretending it was a ping.
- `Get-NetworkInfo` returns real lists for IPv6 addresses, gateways and DNS servers instead of comma joined strings.
- `New-TemporaryDirectory` uses `Directory.CreateTempSubdirectory`.
- Source layout: cmdlets live under `Cmdlets/<Category>/`, helpers with no PowerShell dependency under `Internal/`, output types under `Models/`. File scoped namespaces and implicit usings throughout.
- Analyzers run at `latest-recommended` with warnings as errors on both projects.
- CodeQL workflow updated to current action versions and .NET 10.

### Fixed

- `ConvertFrom-Base64` accepted a `-OutputPath` in the `String` parameter set that was then ignored.
- `Compare-Hash` compared against a signature that had only had dashes removed; trailing whitespace or colons made every comparison fail.
- `Get-EnvironmentPath` used the process PATH even when `-Target User` was requested on Windows.
- `Test-Port` could report a port as open when the connect task faulted after the timeout check.
- `Move-Trash` on macOS could collide with an existing item in `~/.Trash` and overwrite it.
- Byname cmdlets wrote `-Description ""` into the profile when no description was given, and quoted descriptions with doubled double quotes, which PowerShell does not treat as an escape.

### Removed

- The Ampere package reference.
- The `BetaCmdlet` attribute (renamed, see above).
- The hand written `PowerPlug.dll-Help.xml`. It is now generated.
- The acknowledgements section from the README.
- The `PowerPlug/dist` folder and its `.gitignore`. Release builds now stage to `dist/` at the repository root.

## [0.9.0] - 2026-03-29

### Added
- **Invoke-Retry** (`retry`) - Retry a script block with configurable max attempts, delay, and exponential backoff.
- **Measure-ScriptBlock** (`msb`) - Multi-iteration benchmarking with min, max, average, median, and standard deviation.
- **ConvertTo-HashTable** (`toht`) - Recursively convert PSObject/PSCustomObject to an ordered hashtable.
- **Test-Port** (`tp`) - Cross-platform TCP port connectivity test with latency reporting.
- **ConvertTo-Base64** (`tobase64`) - Encode strings or file contents to Base64 with encoding options.
- **ConvertFrom-Base64** (`frombase64`) - Decode Base64 strings to plaintext or write decoded bytes to a file.
- **Get-EnvironmentPath** (`gpath`) - List PATH entries with existence validation.
- **New-RandomString** (`nrs`, `randstr`) - Generate cryptographically secure random strings.
- **New-TemporaryDirectory** (`ntd`) - Create uniquely named temporary directories.
- `PowerPlugCmdletBase` - Shared base class for all PowerPlug cmdlets with automatic beta warning emission.
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
- **Newline accumulation bug** - `BynameRemover.Remove()` now trims trailing whitespace before appending a single newline, preventing blank lines from growing on each `Set-Byname` / `Remove-Byname` invocation.
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
