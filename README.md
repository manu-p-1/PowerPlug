<p align="center">
  <a href="https://github.com/manu-p-1/PowerPlug/" target="_blank">
    <img src="https://github.com/manu-p-1/PowerPlug/blob/master/assets/PowerPlugLogo.png" alt="PowerPlug Logo">
  </a>
</p>

## Introduction

PowerPlug is a cross-platform PowerShell 7 module with the small utilities you end up wanting every day: checking a port, decoding a JWT, finding what is eating disk space, loading a `.env` file, persisting an alias, and so on. It is written in C# on top of the PowerShell Standard Library, has no third party runtime dependencies, and runs on Windows, macOS and Linux.

Version 1.0.0-rc.1 targets .NET 8 and .NET 10. The module works on PowerShell 7.4 and later and picks the right build for your runtime automatically.

## Cmdlets

Cmdlets marked with `*` are **experimental**. See [Experimental cmdlets](#experimental-cmdlets) below.

### Data and encoding
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `ConvertTo-Base64` | `tobase64` | Encode text or a file as Base64 or Base64Url |
| `ConvertFrom-Base64` | `frombase64` | Decode Base64 or Base64Url to text or a file |
| `ConvertTo-UrlEncoding` | `urlencode` | Percent-encode a string (RFC 3986 or form style) |
| `ConvertFrom-UrlEncoding` | `urldecode` | Decode a percent-encoded string |
| `ConvertFrom-UnixTime` | `fromepoch` | Unix seconds or milliseconds to `DateTime` |
| `ConvertTo-UnixTime` | `toepoch` | `DateTime` to Unix seconds or milliseconds |
| `ConvertFrom-Jwt` | `fromjwt` | Decode a JWT header and claims (no signature check) |
| `ConvertTo-HashTable` | `toht` | PSObject to ordered hashtable, optionally recursive |
| `Convert-Color` | `color` | Hex, RGB, HSL and CSS colour name conversion |

### Security
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Compare-Hash` | `csh` | Compare a file's hash with a published signature |
| `Get-StringHash` | `strhash` | Hash a string with SHA256, SHA384, SHA512, SHA1 or MD5 |
| `New-RandomString` | `nrs`, `randstr` | Cryptographically secure random strings and passwords |
| `Test-Elevation` | `isadmin` | True when running as administrator or root |

### File system
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Move-Trash` * | `trash` | Send files and folders to the Recycle Bin or Trash |
| `New-TemporaryDirectory` | `ntd` | Create a uniquely named temp directory |
| `Rename-BatchItem` | `Rename-Batch` | Regex bulk rename with `-WhatIf` preview (PowerRename style) |
| `Get-DirectorySize` | `dirsize` | Recursive folder size, or one row per child folder |
| `Find-DuplicateFile` | `dupes` | Find files with identical content |
| `Remove-EmptyDirectory` | `rmempty` | Prune empty directories bottom up |
| `Get-FileEncoding` | `gfe` | Encoding, byte order mark and line ending style of files |
| `Convert-LineEnding` | `eol` | Convert files between LF and CRLF without touching other bytes |
| `Set-FileTimestamp` | `touch` | Update timestamps, creating the file if needed |
| `Get-LargestFile` | `bigfiles` | Largest files under a directory |
| `Compare-Directory` | `dirdiff` | What differs between two directory trees |
| `Wait-File` | `waitfile` | Block until a file appears, changes or is deleted |

### Networking
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Test-Port` | `tp` | TCP connect test with latency |
| `Wait-Port` | `waitport` | Block until a TCP port opens |
| `Test-Url` | `turl` | HTTP status, latency, redirect target and headers |
| `Get-TlsCertificate` | `gtls` | Remote certificate subject, issuer, expiry and SANs |
| `Get-PublicIPAddress` | `pubip` | Your public IP via Cloudflare, ipify, icanhazip or a custom URL |
| `Get-NetworkInfo` | `gni`, `netinfo` | Interfaces with IPs, gateways, DNS and MAC |
| `Get-ListeningPort` * | `lsport` | Listening ports and the processes that own them |
| `Get-Speed` * | `speedtest`, `gspd` | Download, upload, latency, jitter and packet loss |

### Shell and environment
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Get-EnvironmentPath` | `gpath` | PATH entries with missing and duplicate flags |
| `Add-EnvironmentPath` * | `addpath` | Add directories to PATH |
| `Remove-EnvironmentPath` * | `rmpath` | Remove, dedupe or prune PATH entries |
| `Import-DotEnv` | `dotenv` | Load a `.env` file into the session |
| `Get-SystemInfo` | `sysinfo` | OS, hardware, runtime and session summary |

### Aliases (Byname)
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `New-Byname` * | `nbn` | Create an alias and persist it to `$PROFILE` |
| `Set-Byname` * | `sbn` | Change a persisted alias |
| `Remove-Byname` * | `rbn` | Remove a persisted alias |

### Diagnostics
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Invoke-Retry` | `retry` | Retry a script block with backoff and jitter |
| `Measure-ScriptBlock` | `msb` | Benchmark a script block with min, max, mean, median and standard deviation |
| `Watch-Command` * | `watchcmd` | Re-run a script block on an interval, like `watch` |

Every cmdlet has full help. Try `Get-Help Test-Url -Examples` or `Get-Command -Module PowerPlug`.

Aliases never shadow common native tools such as `du`, `watch` or `rename`.

## Experimental cmdlets

Some cmdlets are marked experimental. They work and they are tested, but they depend on things outside PowerPlug's control: platform specific behaviour (how the trash works on Linux), external tools (`lsof`, `ss`, `netstat`), the network, or they change state that matters to you (`$PROFILE`, PATH).

An experimental cmdlet prints a warning the first time it runs in a pipeline, for example:

```
WARNING: Move-Trash is experimental. It relies on platform specific trash behaviour. On Linux it follows the FreeDesktop spec, which some environments may not honor.
```

Silence it per call with `-WarningAction SilentlyContinue`. Experimental cmdlets may change their parameters or output between minor releases. Stable cmdlets will not without a note in the changelog. If something misbehaves, [open an issue](https://github.com/manu-p-1/PowerPlug/issues) with the platform and PowerShell version.

## A few examples

```powershell
# Which process is on port 3000, and stop it
Get-ListeningPort -Port 3000 | Stop-Process -Id { $_.ProcessId }

# Wait for a container to come up before running migrations
if (Wait-Port localhost 5432 -TimeoutSeconds 60 -Quiet) { ./migrate.ps1 }

# Certificates expiring in the next month
"api.example.com", "www.example.com" | Get-TlsCertificate | Where-Object DaysRemaining -lt 30

# Read the claims out of a bearer token
$response.Headers.Authorization | ConvertFrom-Jwt | Select-Object Subject, ExpiresAt, IsExpired

# Preview a bulk rename, then do it
Get-ChildItem *.jpg | Rename-BatchItem -Pattern '^IMG_(\d+)' -Replacement 'holiday-$1' -WhatIf
Get-ChildItem *.jpg | Rename-BatchItem -Pattern '^IMG_(\d+)' -Replacement 'holiday-$1'

# What is taking up space in the home folder
Get-DirectorySize ~ -Children | Sort-Object SizeBytes -Descending | Select-Object -First 10
Get-LargestFile ~ -Recurse -Top 20

# Normalise line endings before a commit
Get-ChildItem -Recurse -File -Include *.cs,*.ps1 | Convert-LineEnding -To LF

# Check a backup against the live folder byte for byte
Compare-Directory ./backup ./live -Recurse -CompareBy Hash

# Load secrets for a local run
Import-DotEnv ./.env.local -Force

# Retry a flaky API with exponential backoff
Invoke-Retry { Invoke-RestMethod https://api.example.com/health } -MaxAttempts 5 -DelayMilliseconds 500 -ExponentialBackoff -Jitter
```

## Installation

### From the PowerShell Gallery
```powershell
Install-Module -Name PowerPlug -AllowPrerelease
```

1.0.0-rc.1 is a release candidate. Drop `-AllowPrerelease` once 1.0.0 is published.

### From GitHub Releases
Download the zip from the [Releases page](https://github.com/manu-p-1/PowerPlug/releases) and extract the `PowerPlug` folder into a directory listed in `$env:PSModulePath`.

### Importing
```powershell
Import-Module PowerPlug
```

Add that line to your `$PROFILE` to load it in every session.

## Building from source

### Prerequisites
- PowerShell 7.4 or later
- .NET SDK 10.0 (it can build the .NET 8 target as well)

### Build and test
```bash
dotnet build
dotnet test
```

Import the development build straight from the output folder:
```powershell
Import-Module ./PowerPlug/bin/Debug/net8.0/PowerPlug.psd1
```

A release build stages a ready to ship module folder under `dist/PowerPlug` with both frameworks side by side:
```bash
dotnet build -c Release
pwsh ./tools/New-HelpFile.ps1 -Configuration Release
Import-Module ./dist/PowerPlug/PowerPlug.psd1
```

The help file is generated from the XML doc comments in the source, so after changing a cmdlet's documentation run `./tools/New-HelpFile.ps1` and commit the result.

## Project layout

```
PowerPlug/
  Attributes/      ExperimentalCmdlet attribute
  Base/            PowerPlugCmdlet base class
  Cmdlets/         One folder per category (Data, Diagnostics, FileSystem, Networking, Profile, Security, Shell)
  Internal/        Shared helpers with no PowerShell dependency (hashing, parsers, trash, PATH handling)
  Models/          Output types
  PowerPlug.psd1   Module manifest
  PowerPlug.psm1   Loader that picks the right framework build
PowerPlug.Tests/   xUnit tests: pure unit tests, mocked cmdlet tests, and hosted PowerShell integration tests
tools/             New-HelpFile.ps1
```

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](https://github.com/manu-p-1/PowerPlug/blob/master/CONTRIBUTING.md) for how the project is put together and what a good pull request looks like.

## Licensing

PowerPlug is licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html).
