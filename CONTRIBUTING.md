# Contributing to PowerPlug

Thanks for taking an interest. PowerPlug is a small project and every fix, cmdlet and doc improvement helps. Before starting on anything sizeable, open an issue so we can talk it through; it saves everyone time if the shape of a change is agreed before the code is written.

## Getting set up

You need:

- PowerShell 7.4 or later
- The .NET 10 SDK (it also builds the .NET 8 target)
- An editor. VS Code with the C# Dev Kit and PowerShell extensions works well, as does Rider or Visual Studio.

Then:

```bash
git clone https://github.com/<you>/PowerPlug.git
cd PowerPlug
dotnet build
dotnet test
```

To try the module you just built:

```powershell
Import-Module ./PowerPlug/bin/Debug/net8.0/PowerPlug.psd1 -Force
```

`Properties/launchSettings.json` has a debug profile that launches `pwsh` with the module imported, so you can set breakpoints in a cmdlet and hit them from the prompt.

## How the code is organised

```
PowerPlug/
  Attributes/   ExperimentalCmdletAttribute
  Base/         PowerPlugCmdlet, the base class every cmdlet derives from
  Cmdlets/      One folder per category: Data, Diagnostics, FileSystem, Networking, Profile, Security, Shell
  Internal/     Helpers with no PowerShell dependency (hashing, parsers, trash, PATH handling)
  Models/       Output types returned by cmdlets
PowerPlug.Tests/
  Infrastructure/  CmdletHarness (mocked runtime), PowerShellFixture (hosted runspace), TempDirectory
  Internal/        Unit tests for the helpers
  Cmdlets/         Tests that run cmdlets against a mocked ICommandRuntime
  Integration/     Tests that run scripts in a real PowerShell runspace
tools/
  New-HelpFile.ps1  Generates PowerPlug.dll-Help.xml from the XML doc comments
```

The split between `Cmdlets/` and `Internal/` is deliberate. Anything that can be expressed without `System.Management.Automation` goes in `Internal/`, where it can be unit tested directly. The cmdlet is then a thin layer that binds parameters, calls the helper and writes output.

## Writing a cmdlet

A few conventions keep the module consistent:

- Derive from `PowerPlugCmdlet`. It gives you `ResolvePath`, `WriteError(exception, id, category, target)` and `WriteFileError`, and it handles the experimental warning.
- Make the class `sealed`, name it `<Verb><Noun>Cmdlet`, and put it in the matching `Cmdlets/<Category>/` folder.
- Use an [approved verb](https://learn.microsoft.com/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands). The integration tests check this.
- Return a typed class from `Models/` rather than a `PSObject` with note properties. Add an `[OutputType]` attribute. If the object has more than four properties and reads better as a table, add a view to `PowerPlug.Format.ps1xml`.
- Add `[Alias]` with one or two short names. Add them to `AliasesToExport` in `PowerPlug.psd1` too; the tests will tell you if you forget.
- Add the cmdlet name to `CmdletsToExport` in the manifest.
- Anything that deletes, renames, writes to disk or changes environment state gets `SupportsShouldProcess = true` and calls `ShouldProcess` before acting.
- Report problems with a file or host as non-terminating errors (`WriteError`) so a pipeline of many items keeps going. Use `ThrowTerminatingError` only when the cmdlet cannot do anything useful at all.
- Long running work should call `WriteProgress`, and anything that waits on the network should keep a `CancellationTokenSource`, cancel it in `StopProcessing`, and implement `IDisposable` to clean it up.
- Document the class with `<para type="synopsis">`, `<para type="description">` and at least one `<example>`. Document each parameter with `<para type="description">`. This is where `Get-Help` content comes from.

Mark a cmdlet `[ExperimentalCmdlet("reason")]` when it depends on external tools, platform quirks or the network in a way we cannot fully control, or when it modifies state the user cares about. Do not mark things experimental just because they are new. The reason should complete the sentence "`<Cmdlet>` is experimental. ..."

After adding or changing a cmdlet, regenerate the help file:

```powershell
dotnet build
./tools/New-HelpFile.ps1
```

and commit the updated `PowerPlug/PowerPlug.dll-Help.xml`.

## Tests

Every cmdlet should have tests at two levels:

1. **Mocked runtime tests** (`PowerPlug.Tests/Cmdlets/`). `CmdletHarness<T>` swaps in an NSubstitute `ICommandRuntime`, so you set parameters on the cmdlet, call `Run()`, and assert on `Output`, `Errors`, `Warnings` and `ShouldProcessTargets`. These are fast and cover logic and error handling.
2. **Integration tests** (`PowerPlug.Tests/Integration/`). `PowerShellFixture` hosts a runspace with the module loaded. Use it for anything that touches parameter binding, parameter sets, pipeline input, aliases or `SessionState`. Add the class to the `PowerShell` collection so it shares the runspace.

Helpers in `Internal/` get plain unit tests in `PowerPlug.Tests/Internal/`.

Tests must pass on both `net8.0` and `net10.0` and on all three operating systems. If a test genuinely cannot run somewhere (for example, creating symlinks on Windows without elevation), return early with a comment saying why rather than marking it skipped.

```bash
dotnet test                       # everything
dotnet test -f net10.0            # one framework
dotnet test --filter "FullyQualifiedName~TestPort"
```

## Style

- `TreatWarningsAsErrors` is on with the `latest-recommended` analyzer set. If an analyzer is wrong for a specific line, suppress it with a `#pragma` and a one line reason. Do not turn rules off project wide.
- File scoped namespaces, implicit usings, nullable enabled.
- Comments explain why, not what. If the code needs a comment to say what it does, rewrite the code.
- Keep the wording in docs and help natural. Write the way you would explain it to a colleague.

## Pull requests

See [BRANCHING.md](BRANCHING.md) for how branches and releases fit together. In short:

1. Branch from `experimental` and open the pull request against `experimental`, not `master`.
2. Keep the change focused. One cmdlet or one fix per pull request is ideal.
3. Add or update tests and the help file.
4. Add a line under `Unreleased` in `CHANGELOG.md`.
5. Make sure `dotnet build` and `dotnet test` are clean on your machine. CI runs the same on Windows, macOS and Linux.
6. Describe what changed and why in the pull request. Screenshots or a pasted session are welcome for anything user facing.

## Code of Conduct

Please read and follow the [Code of Conduct](https://github.com/manu-p-1/PowerPlug/blob/master/CODE_OF_CONDUCT.md).

## Licensing

By contributing you agree that your contributions are licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html), the same as the rest of the project.
