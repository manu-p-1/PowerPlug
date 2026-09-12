# Security Policy

## Supported versions

| Version | Supported |
| ------- | --------- |
| 1.0.x   | Yes       |
| < 1.0   | No        |

## Reporting a vulnerability

If you find a security problem in PowerPlug, please report it privately rather than opening a public issue.

Use GitHub's private vulnerability reporting on the repository's Security tab, or email the maintainer at the address on their GitHub profile. Include:

1. What the problem is
2. How to reproduce it
3. What you think the impact is
4. A suggested fix, if you have one

You will get an acknowledgement within 48 hours and we will agree a disclosure timeline with you once the issue is understood.

## What PowerPlug does that you should know about

PowerPlug is a utility module and several cmdlets touch things outside the current session.

**Writes to your PowerShell profile.** `New-Byname`, `Set-Byname` and `Remove-Byname` append to and edit `$PROFILE`. Lines are quoted so an alias value cannot inject extra commands, and removal only matches lines PowerPlug wrote. Still, only run these where you trust the input, and review the file if anything looks odd. All three support `-WhatIf`.

**Changes environment variables.** `Add-EnvironmentPath` and `Remove-EnvironmentPath` rewrite PATH. `Import-DotEnv` sets whatever variables the `.env` file contains. On Windows the `User` and `Machine` targets write to the registry. All support `-WhatIf`, and `Import-DotEnv` leaves existing variables alone unless you pass `-Force`.

**Runs script blocks you give it.** `Invoke-Retry`, `Measure-ScriptBlock` and `Watch-Command` execute the script block as-is in your session. They add no sandboxing. Do not pass script blocks built from untrusted input.

**Deletes, renames and rewrites files.** `Move-Trash` moves items to the platform trash (Recycle Bin, `~/.Trash`, or the FreeDesktop trash on Linux). `Remove-EmptyDirectory` deletes directories that contain nothing. `Rename-BatchItem` renames whatever matches. `Convert-LineEnding` rewrites files in place (through a temporary file and rename, so an interrupted run leaves the original intact). `Set-FileTimestamp` creates empty files and changes timestamps. All support `-WhatIf` and none of them follow symbolic links.

**Talks to the internet.**
- `Get-Speed` downloads from and uploads random bytes to Cloudflare's speed test endpoints (`speed.cloudflare.com`) by default and pings `1.1.1.1`. You can point it at your own endpoints with `-DownloadUrl`, `-UploadUrl` and `-LatencyHost`.
- `Get-PublicIPAddress` asks Cloudflare, ipify or icanhazip which address you appear to come from, or a URL you supply.
- `Test-Url`, `Test-Port`, `Wait-Port` and `Get-TlsCertificate` connect to the host you name.

All requests identify themselves with the user agent `PowerPlug/<version>`. Nothing is sent beyond what the protocol requires.

**Accepts any TLS certificate on purpose.** `Get-TlsCertificate` exists to inspect certificates, including expired or untrusted ones, so it completes the handshake regardless and reports the trust verdict in `ChainTrusted`. No application data is exchanged over that connection.

**Does not verify JWT signatures.** `ConvertFrom-Jwt` decodes the header and payload so you can read them. It tells you nothing about whether the token is genuine. Do not use it to make trust decisions.

**Offers weak hash algorithms.** `Compare-Hash`, `Get-StringHash` and `Find-DuplicateFile` accept MD5 and SHA1 because vendors still publish those checksums. They are fine for detecting accidental corruption and duplicates, and not fine for anything an attacker might influence. The default everywhere is SHA256.

**Shells out for process names.** `Get-ListeningPort` runs `netstat`, `lsof` or `ss` with fixed arguments to find which process owns a socket. No user input reaches the command line.

**Regular expressions from user input.** `Rename-BatchItem` compiles the pattern you give it. Profile editing and `.env` parsing use fixed patterns. Every regex in the module runs with a match timeout so a hostile pattern or file cannot hang the session.

## Dependencies

PowerPlug's runtime dependency is the PowerShell Standard Library, which is compile time only. The shipped module contains only `PowerPlug.dll` and the .NET base class library it runs on.

The test project references `Microsoft.PowerShell.SDK`, xUnit and NSubstitute. None of that ships with the module.

Dependabot and CodeQL run on the repository.
