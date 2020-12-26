<p align="center">
  <img src="https://github.com/manu-p-1/PowerPlug/blob/master/assets/PowerPlugLogo.png">
  <br>
</p>
<p align="center">
  
   <a href="https://github.com/manu-p-1/PowerPlug/graphs/contributors" alt="Contributors">
      <img src="https://img.shields.io/github/contributors/manu-p-1/PowerPlug?color=lightgrey" /></a>
    
   <a href="https://github.com/manu-p-1/PowerPlug/pulse" alt="Activity">
        <img src="https://img.shields.io/github/commit-activity/m/manu-p-1/PowerPlug?color=lightgrey" /></a>
        
   <a alt="Open Issues">
        <img src="https://img.shields.io/github/issues/manu-p-1/PowerPlug"/></a>
        
   <a alt="Repo Size">
        <img src="https://img.shields.io/github/repo-size/manu-p-1/PowerPlug?label=size&color=%20%230099ff"/></a>
        
   <a alt="License">
        <img src="https://img.shields.io/github/license/manu-p-1/PowerPlug?color=%20%230099ff"/></a>
   
</p>

## Introduction
The cmdlet library contains `PSCmdlet` classes from the PowerShell Standard Library. These cmdlets can be added to PowerShell in the `$PROFILE` to make them visible during
session. Cmdlet's also use XML documentation and the [XmlDoc2CmdletDoc](https://github.com/red-gate/XmlDoc2CmdletDoc) NuGet package to convert XML .NET comments to a 
`.dll-Help.xml` file that contains cmdlet help text in `MAML`. This allows for more extensibility and accessibility for cmdlet integration on PowerShell. For more information
on documenting PowerShell binary cmdlets, visit this article on [Documenting Your PowerShell Binary Cmdlets](https://www.red-gate.com/simple-talk/dotnet/software-tools/documenting-your-powershell-binary-cmdlets/).

## Execution
The latest release can be found under the [Releases Page](https://github.com/manu-p-1/PowerPlug/releases). You can use the `PowerPlug.dll` binary and place it within any other directory, but most sensibly in `$env:PSModulePath`. It is important to note that support for full `Get-Help` descriptions is contingent on the `PowerPlug.dll-Help` being in the same directory as `PowerPlug.dll`. To import the dll for the session, you can run:

```powershell
ipmo Path\To\PowerPlug.dll
```

You can use the aforementioned command within the `$PROFILE` to load the library on PowerShell startup. Run `Get-Module` to confirm the import rand successfully.

## Building Project
Prerequisites:
- PowerShell 7.0 or Later
- Visual Studio 2017 or Later
- .NET Framework 4.7.2 or Later

The default language setting for this project is C# 8.0. The project can be built using `dotnet build` and the output will display the `AssemblyPath`. 

## Roadmap
- Expanding the cmdlet library with more useful commands
- Transition to .NET 5 and C# 9.0 as a new development medium
- "Modularizing" the repo to work with a direct name such as `Import-Module -Name ...`
