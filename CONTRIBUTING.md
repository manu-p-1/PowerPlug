# PowerPlug Contribution Guidelines

PowerPlug is only as good as its contributors and we are excited to help you make positive change. When contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change.

# Pull Request Process

1. Ensure any install or build dependencies are removed before the end of the layer when doing a 
   build.
2. Update the README.md with details of changes to the interface, this includes new environment 
   variables, exposed ports, useful file locations and container parameters.
3. Follow the Pull Request template carefully. Certain changes may *not* require you to answer all of the questions, however, an in-depth discussion about the changes goes a long way.
4. You may merge the Pull Request in once you have the sign-off of two other developers, or if you 
   do not have permission to do that, you may request the second reviewer to merge it for you.

## Do's and Don'ts

**Do** follow .NET coding style guidelines and conventions  
**Do** document all of your changes (screenshots are helpful)  
**Do** be engaged  
**Do** test your changes (integration and regression)  
**Do** respond to feedback  

**Don't** push large changes - no one likes to read a 5000 line diff  
**Don't** duplicate or repeat yourself  
**Don't** discourage others  

# Code of Conduct
Contributors must strictly adhere to the PowerPlug code of conduct mentioned here: [Code of Conduct](https://github.com/manu-p-1/PowerPlug/blob/master/CODE_OF_CONDUCT.md)

# Contribution
There are a variety of ways that you can contribute to PowerPlug for the betterment of developers. There are four main components to contribute to:

1. Cmdlets
2. PowerPlug DocFX
3. GitHub Repo
4. Other (Scripting, CI/CD, Unit Testing, etc...)

We'll discuss how to contribute to each one in detail below.

## Cmdlets
If you are completely new to PowerShell and would like to get started, start with the [PowerShell Documentation](https://docs.microsoft.com/en-us/powershell/). To learn more about how to write custom PSCmdlets, [start here](https://docs.microsoft.com/en-us/powershell/scripting/developer/cmdlet/cmdlet-overview?view=powershell-7.1). Custom cmdlets comprise of the base of this repository. If you are interested in creating a custom statistics or networking cmdlet library to accomplish tasks which are too verbose otherwise, PowerPlug is the place for you to contribute.

### Environment Information
It's important to note that PowerPlug only supports PowerShell version 7 and the [.NET 5 runtime](https://dotnet.microsoft.com/download/dotnet/5.0). After you setup .NET 5 with Visual Studio, you'll be able to fork and clone the repository. The project can be built using `dotnet build` or Visual Studio and the output will display the `AssemblyPath`. The AssemblyPath is a `.dll` file is created within the `bin` folder of the repository; this file can be imported into PowerShell 7. If a debug executable has not been setup already, follow these steps:

1. Go to the **Debug** menu at the top of Visual Studio
2. Under the **Debug** menu dropdown, select **PowerPlug Debug Properties**
3. Under **Executable:**, add the path to your PowerShell 7 exe
4. Under **Application Arguments**, add the following arguments:
   ```powershell
   -NoLogo -NoProfile -NoExit -Command "Import-Module '.\PowerPlug.dll'" 
   ```
   This will allow you to run PowerShell under Debug mode and and test cmdlets with the VS debugger.

### Cmdlet Documentation
Make sure to document your customized cmdlets using the XML format on top of each class, property and method. An example below is provided:

```csharp
/// <summary>
/// <para type="synopsis">Compares a file's user specified hash with another signature</para>
/// <para type="description">This function will compare a user defined hash of a file, such as an executable with the known signature of the file. 
/// This is especially useful since hashed values are long. The current supported hashes are SHA256, SHA512, MD5.
/// </para>
/// <para type="aliases">trash</para>
/// <example>
/// <para>A sample Compare-Sha256 command</para>
/// <code>Compare-Hash .\audacity-win-2.4.2.exe 1f20cd153b2c322bf1ff9941e4e5204098abdc7da37250ce3fb38612b3e927bc</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Compare, "Hash")]
[Alias("csh")]
public class CompareHashCmdlet : PSCmdlet { ... }
```

In order to view `Get-Help` information for your custom cmdlets within PowerShell, you'll need to add cmdlet help xml into the `PowerPlug.dll-Help.xml` file. For more information on creating cmdlet help, view [this article](https://docs.microsoft.com/en-us/powershell/scripting/developer/help/writing-help-for-windows-powershell-cmdlets?view=powershell-7.1). The XML-based cmdlet Help file promotes consistency within the PowerShell environment. The XML file is validated agains the MAML schema.

When a release is created, the assembly DLL (or multiple DLLs if multiple assemblies are created) are amalgamated with the the dll-Help.xml, license, and a `.psd1` file into a PowerShell module which is published in the PowerShell gallery. 

## DocFX
Since all PowerPlug methods are documented using .NET XML documentation. This is compiled using [DocFX](https://dotnet.github.io/docfx/) with the `docfx.json` file under the [DocFX folder](https://github.com/manu-p-1/PowerPlug/tree/master/DocFx). DocFX creates static HTML pages which are used by <https://powerplug.me>. If you would like to change documentation on this website after understanding the DocFX file structure, you can contribute it at the top-level DocFX folder.

### Building DocFX
Download the DocFX executable from their website. For convenience, it is recommended that the executable be added to the System environment variable PATH. [Here's a tutorial](https://www.c-sharpcorner.com/article/add-a-directory-to-path-environment-variable-in-windows-10/) on how to do that. To run the executable, go to the top level solution directory and run:

```powershell
docfx.exe .\DocFx\docfx.json
```

This allows you to build the documentation, then to view it locally on your localhost:

```powershell
docfx.exe .\DocFx\docfx.json --serve
```

For more command options when needed, view the DocFX website provided above.

## Repository Contributions
The repository is the first thing a developer see's about the project and we like to keep it tidy. If you see ways to contribute, such as improving this document, changing README's, or improving the logo artwork, don't hesitate to create and issue and get it resolved. Keep in mind that aformentioned guidelines to good pull requests still apply.

## Other Contributions
This is a broad contribution component which encompasses a massive role in the success of PowerPlug. Although no guidelines apply, contribution in this area signfies a level of content expertise and understanding. This section includes adding build scripts to improve productivity, creating CI/CD workflows, security compliance, adding unit testing assemblies to test PSCmdlets, optimizing the codebase with respect to performance, design, among other things. Keep in mind that aformentioned guidelines to good pull requests still apply.

# State
PowerPlug is a very fluid project and you may encounter issues during execution, especially for preleases. To report an issue visit, [PowerPlug Issues](https://github.com/manu-p-1/PowerPlug/issues). If you are able to fix the isssue yourself by building the project, show this repo some love and fork it, would ya?

# Licensing
PowerPlug is licensed under the [**GNU General Public License v3.0**](https://www.gnu.org/licenses/gpl-3.0.en.html). The GNU General Public License is a free, copyleft license for software and other kinds of works.

# Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage], version 1.4,
available at [http://contributor-covenant.org/version/1/4][version]

[homepage]: http://contributor-covenant.org
[version]: http://contributor-covenant.org/version/1/4/
