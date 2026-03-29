# PowerPlug Contribution Guidelines

PowerPlug is only as good as its contributors and we are excited to help you make positive change. When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

## Pull Request Process

1. Ensure any install or build dependencies are removed before the end of the layer when doing a build.
2. Update the `README.md` with details of changes to the interface.
3. Follow the Pull Request template carefully. Certain changes may *not* require you to answer all of the questions, however, an in-depth discussion about the changes goes a long way.
4. You may merge the Pull Request once you have the sign-off of two other developers, or if you do not have permission to do that, you may request the second reviewer to merge it for you.

## Do's and Don'ts

**Do** follow .NET coding style guidelines and conventions  
**Do** document all of your changes (screenshots are helpful)  
**Do** be engaged  
**Do** test your changes (integration and regression)  
**Do** respond to feedback  

**Don't** push large changes — no one likes to read a 5000 line diff  
**Don't** duplicate or repeat yourself  
**Don't** discourage others  

## Code of Conduct

Contributors must strictly adhere to the PowerPlug code of conduct: [Code of Conduct](https://github.com/manu-p-1/PowerPlug/blob/master/CODE_OF_CONDUCT.md)

## Contribution Areas

There are several ways to contribute to PowerPlug:

1. **Cmdlets** — New or improved PowerShell cmdlets
2. **Repository** — Documentation, README improvements, artwork
3. **Infrastructure** — CI/CD, build scripts, unit testing, security compliance

### Cmdlets

If you are new to PowerShell, start with the [PowerShell Documentation](https://learn.microsoft.com/en-us/powershell/). To learn about writing custom PSCmdlets, see the [Cmdlet Overview](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/cmdlet-overview). PowerPlug welcomes contributions for networking, encoding, file system, diagnostic, and utility cmdlets.

#### Environment Setup

PowerPlug targets **PowerShell 7+** and **.NET 8 / .NET 10** (multi-target). To get started:

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)).
2. Fork and clone the repository.
3. Build the project:
   ```bash
   dotnet build
   ```
4. Import the module into PowerShell 7 for testing:
   ```powershell
   Import-Module ./PowerPlug/bin/Debug/net8.0/PowerPlug.dll
   ```

To debug with an IDE (Visual Studio, Rider, VS Code), configure the launch profile to run `pwsh` with these arguments:

```
-NoLogo -NoProfile -NoExit -Command "Import-Module './PowerPlug.dll'"
```

See `Properties/launchSettings.json` for the existing debug configuration.

#### Cmdlet Guidelines

- **Mark all new cmdlets as BETA** by applying `[BetaCmdlet(BetaCmdlet.WarningMessage)]`.
- **Seal cmdlet classes** — all cmdlet classes should be `sealed`.
- Organize cmdlets into category folders under `Cmdlets/` (e.g., `Networking/`, `Encoding/`).
- Document classes, properties, and methods using XML doc comments.
- Add the `[Cmdlet]` and `[Alias]` attributes with appropriate verb-noun names.
- For file-based cmdlets, use `CmdletUtilities.ResolvePath()` for path resolution.
- For destructive operations, enable `SupportsShouldProcess`.

#### Cmdlet Help

To provide `Get-Help` support, add cmdlet help XML to the `PowerPlug.dll-Help.xml` file. For more information on creating cmdlet help, see [Writing Help for PowerShell Cmdlets](https://learn.microsoft.com/en-us/powershell/scripting/developer/help/writing-help-for-windows-powershell-cmdlets).

### Repository Contributions

The repository is the first thing a developer sees about the project. If you see ways to improve documentation, README files, or artwork, don't hesitate to create an issue.

### Other Contributions

This includes build scripts, CI/CD workflows, security compliance, unit testing, and performance optimization. Contribution in this area signifies a level of expertise and understanding.

## Current State

> **All cmdlets are currently marked BETA.** You may encounter issues during execution. To report a problem, visit [PowerPlug Issues](https://github.com/manu-p-1/PowerPlug/issues).

## Licensing

PowerPlug is licensed under the [**GNU General Public License v3.0**](https://www.gnu.org/licenses/gpl-3.0.en.html).
