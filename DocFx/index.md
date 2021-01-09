﻿<p align="center">
  <a href="https://github.com/manu-p-1/PowerPlug/" target="_blank">
    <img src="https://raw.githubusercontent.com/manu-p-1/PowerPlug/master/assets/PowerPlugLogoAlt.png" alt="PowerPlug Logo">
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

## PowerPlug Docs
PowerPlug is a PowerShell 7 cmdlet library. The main mission of PowerPlug is to make PowerShell development faster and eaiser. PowerPlug is run through C# `PSCmdlet` classes from the PowerShell Standard Library. 
The site is useful for version changelogs, latest releases, and cmdlet documentation.

## Execution
### Install from PS Gallery
The latest release can be found at [PowerPlug Releases](https://www.powershellgallery.com/packages/PowerPlug/). You can install the prerelease to PowerShell by running:

```powershell
Install-Module -Name PowerPlug
```
### Install from GitHub Releases
The latest zip release can be found under the [Github Releases Page](https://github.com/manu-p-1/PowerPlug/releases). You can use the `PowerPlug.dll` binary and place it within any other directory, but conventionally in `$env:PSModulePath`. 

### Importing into session
To import the dll for the session, you can run: `ipmo PowerPlug` **or** `Import-Module PowerPlug`. You can use the previous commands within the `$PROFILE` to load the library on PowerShell startup. Run `Get-Module PowerPlug` to confirm the import ran successfully.

## Contributing
We are actively looking for contributors to work on all aspects of the code base ― from documentation to c# cmdlet utilities.

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
PowerPlug is a very fluid project and you may encounter issues during execution, especially for preleases. For more information visit, [PowerPlug Repo](https://github.com/manu-p-1/PowerPlug/). Or, to report an issue visit,
[PowerPlug Issues](https://github.com/manu-p-1/PowerPlug/issues). If you are able to fix the isssue yourself by building the project, give our repo a Fork, would ya?

## Licensing
PowerPlug is licensed under the GNU General Public License v3.0. The GNU General Public License is a free, copyleft license for software and other kinds of works.
