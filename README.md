<p align="center">
  <a href="https://github.com/manu-p-1/PowerPlug/" target="_blank">
    <img src="https://github.com/manu-p-1/PowerPlug/blob/master/assets/PowerPlugLogo.png" alt="Volt Logo">
  </a>
  <br>
</p>
<p align="center">
  
   <a href="https://github.com/manu-p-1/PowerPlug/graphs/contributors" alt="Contributors">
      <img src="https://img.shields.io/github/contributors/manu-p-1/PowerPlug?color=%20%230099ff"/></a>
    
   <a href="https://github.com/manu-p-1/PowerPlug/pulse" alt="Activity">
      <img src="https://img.shields.io/github/commit-activity/m/manu-p-1/PowerPlug?color=%20%230099ff"/></a>
        
   <a href="https://github.com/manu-p-1/PowerPlug/issues" alt="Open Issues">
      <img src="https://img.shields.io/github/issues/manu-p-1/PowerPlug"/></a>
      
   <a href="https://github.com/manu-p-1/PowerPlug/releases" alt="Latest Release">
      <img src="https://img.shields.io/github/v/release/manu-p-1/PowerPlug?include_prereleases"/></a>
        
   <a href="#" alt="Repo Size">
      <img src="https://img.shields.io/github/repo-size/manu-p-1/PowerPlug?label=size&color=informational"/></a>
        
   <a href="https://github.com/manu-p-1/PowerPlug/blob/master/LICENSE" alt="License">
      <img src="https://img.shields.io/github/license/manu-p-1/PowerPlug?color=informational"/></a>
</p>

## Introduction
PowerPlug is a cross-platform PowerShell 7+ cmdlet utility library targeting .NET 8 and .NET 10. The main mission of PowerPlug is to make PowerShell development faster and easier with practical, everyday utilities. PowerPlug is built using C# `PSCmdlet` classes from the PowerShell Standard Library and is driven by the [Ampere Library](https://github.com/manu-p-1/Ampere).

> **Note:** All cmdlets are currently in **BETA** and may change in future releases.

## Cmdlets

### Networking
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Get-Speed` | `speedtest`, `gspd` | Network speed test with download/upload speed, latency, jitter, and packet loss |
| `Get-NetworkInfo` | `gni`, `netinfo` | Detailed network interface information (IP, subnet, gateway, DNS, MAC, speed) |
| `Test-Port` | `tp` | TCP port connectivity test with latency reporting |

### Encoding
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `ConvertTo-Base64` | `tobase64` | Encode strings or file contents to Base64 |
| `ConvertFrom-Base64` | `frombase64` | Decode Base64 strings to plaintext or file |

### Security & Hashing
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Compare-Hash` | `csh` | Compare file hashes (SHA256, SHA512, SHA384, MD5) against known signatures |
| `New-RandomString` | `nrs`, `randstr` | Generate cryptographically secure random strings |

### File System
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Move-Trash` | `trash` | Move files to Recycle Bin (Windows) or Trash (macOS) |
| `New-TemporaryDirectory` | `ntd` | Create uniquely named temporary directories |

### Aliases (Byname)
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `New-Byname` | `nbn` | Create a persistent alias in your `$PROFILE` |
| `Set-Byname` | `sbn` | Modify an existing persistent alias |
| `Remove-Byname` | `rbn` | Remove a persistent alias from `$PROFILE` |

### Diagnostics & Utilities
| Cmdlet | Alias | Description |
|--------|-------|-------------|
| `Invoke-Retry` | `retry` | Retry a script block with configurable backoff |
| `Measure-ScriptBlock` | `msb` | Benchmark a script block with statistical analysis |
| `ConvertTo-HashTable` | `toht` | Convert PSObject to an ordered hashtable |
| `Get-EnvironmentPath` | `gpath` | List PATH entries with existence validation |

## Installation

### From PowerShell Gallery
```powershell
Install-Module -Name PowerPlug
```

### From GitHub Releases
Download the latest zip from the [Releases Page](https://github.com/manu-p-1/PowerPlug/releases). Place the module folder in a directory listed in `$env:PSModulePath`.

### Importing into a Session
```powershell
Import-Module PowerPlug
```

To load automatically on startup, add the above line to your `$PROFILE`.

## Building

### Prerequisites
- PowerShell 7.0 or later
- .NET 8 SDK or .NET 10 SDK
- Any editor (VS Code recommended)

### Build
```bash
dotnet build
```

The output DLL will be in `bin/Debug/net8.0/` (or `net10.0/`). Import it into PowerShell with:
```powershell
Import-Module ./PowerPlug/bin/Debug/net8.0/PowerPlug.dll
```

## Contributing
We welcome contributions! See [CONTRIBUTING.md](https://github.com/manu-p-1/PowerPlug/blob/master/CONTRIBUTING.md) for guidelines.

## Licensing
PowerPlug is licensed under the [**GNU General Public License v3.0**](https://www.gnu.org/licenses/gpl-3.0.en.html). The GNU General Public License is a free, copyleft license for software and other kinds of works.

## Acknowledgements
Thanks especially to my fellow friends and contributors
- [Sam Yuen](https://github.com/ssyuen)
- [Lok Kwong](https://github.com/Lok-Kwong)
