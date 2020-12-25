# PowerShell Cmdlet Utility Library

## Introduction
The cmdlet library contains `PSCmdlet` classes from the PowerShell Standard Library. These cmdlets can be added to PowerShell in the `$PROFILE` to make them visisbe during
session. Cmdlet's also use XML documentation and the [XmlDoc2CmdletDoc](https://github.com/red-gate/XmlDoc2CmdletDoc) NuGet package to convert XML .NET comments to a 
`.dll-Help.xml` file that contains cmdlet help text in `MAML`. This allows for more extensibility and accessibility for cmdlet integration on PowerShell. For more information
on documenting PowerShell binary cmdlets, visit: [](https://github.com/red-gate/XmlDoc2CmdletDoc).

## Execution
You can use the `PowerShellCmdletLibrary.dll` binary and place it within any other directory but preferable `$env:PSModulePath`. To import the dll for the session, you can
run:

```powershell
ipmo Path\To\PowerShellCmdletLibrary.dll
```

You can use the aforementioned command within the `$PROFILE` to load the library on PowerShell startup.

## Building Project
Prerequisites:
- PowerShell 7.0 or Later
- Visual Studio 2017 or Later
- .NET Framework 4.7.2 or Later

The default language setting for this project is C# 8.0. The project can be built using `dotnet build` and the output will display the `AssemblyPath`. 

## Roadmap
- Expanding the cmdlet library with more useful commands
- Transition to .NET 5 and C# 9.0 as a new development medium
- "Module-arizing" the repo to work with a direct name such as `Import-Module -Name ...`
