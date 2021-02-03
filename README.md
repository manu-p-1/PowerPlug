<p align="center">
  <a href="https://github.com/manu-p-1/PowerPlug/" target="_blank">
    <img src="https://github.com/manu-p-1/PowerPlug/blob/volt/assets/VoltLogo.png" alt="Volt Logo">
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
PowerPlug is a PowerShell 7 cmdlet library. The main mission of PowerPlug is to make PowerShell development faster and eaiser. PowerPlug is run through C# `PSCmdlet` classes from the PowerShell Standard Library. The PowerPlug docs can be found at <https://powerplug.me>.

## Execution
### Install from PS Gallery
You can install the latest release to PowerShell 7 by running:

```powershell
Install-Module -Name PowerPlug
```
### Install from GitHub Releases
The latest zip release can be found under the [Github Releases Page](https://github.com/manu-p-1/PowerPlug/releases). You can use the `PowerPlug.dll` binary and place it within any other directory, but conventionally in `$env:PSModulePath`. 

### Importing into session
To import the dll for the session, you can run: `ipmo PowerPlug` **or** `Import-Module PowerPlug`. You can use the previous commands within the `$PROFILE` to load the library on PowerShell startup. Run `Get-Module PowerPlug` to confirm the import ran successfully.

## Contributing
We are actively looking for contributors to work on all aspects of the code base ― from documentation to C# Cmdlet utilities. For more information onn how to contribute, view our [CONTRIBUTING.md](https://github.com/manu-p-1/PowerPlug/blob/master/CONTRIBUTING.md)

### Building PowerPlug
Prerequisites:
- PowerShell 7.0 or Later
- Visual Studio 2017 or Later OR VSCode
- .NET 5

The default language setting for this project is C# 9.0. The project can be built using `dotnet build` and the output will display the `AssemblyPath`. PowerShell 7 can be set as the startup item on Visual Studio to dynamically debug PowerPlug.

### Documentation
There are three components to PowerPlug documentation:

1. Assembly Documentation
2. PowerShell Help File Documentation
3. Miscellaneous Documentation (README.md, Wiki's, Discussions, etc...)

All PowerPlug methods are documented using .NET XML documentation. This is compiled using [DocFX](https://dotnet.github.io/docfx/) with the `docfx.json` file under the [DocFX folder](https://github.com/manu-p-1/PowerPlug/tree/master/DocFx). DocFX creates static
HTML pages which are used by <https://powerplug.me>. THe `PowerPlug.dll-Help.xml` file is the MAML file that is used to generate the `Get-Help` documentation for PowerPlug cmdlets. Lastly, as a fluid repository, many files change and constantly need to be documented and updated.

## Roadmap
There are two planned releases - the latest being by **July 2021**:

- 0.2.6 - First full release of PowerPlug
- 0.3.0 - Full release with a statistics cmdlets in its own library, comprehensive documentation in the code-base and PowerShell, robust cmdlets

## State
PowerPlug is a very fluid project and you may encounter issues during execution, especially for preleases. To report an issue visit, [PowerPlug Issues](https://github.com/manu-p-1/PowerPlug/issues), or to contribute, view the contribution guidelines.

## Licensing
PowerPlug is licensed under the [**GNU General Public License v3.0**](https://www.gnu.org/licenses/gpl-3.0.en.html). The GNU General Public License is a free, copyleft license for software and other kinds of works.

## Acknowledgements
Thanks especially to my fellow friends and contributors
- [Sam Yuen](https://github.com/ssyuen)
- [Lok Kwong](https://github.com/Lok-Kwong)
