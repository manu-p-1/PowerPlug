# GitHub Releases 

The following list is an up-to-date collection of all releases and descriptions to date.

## 0.2.7 Release of PowerPlug
This release contains a few fixes on PowerShell Help-URI and also fixing the PSD1 script in the module. 
**This is a very minor release and the [0.2.6 release](https://github.com/manu-p-1/PowerPlug/releases/tag/0.2.6) contained more important fixes to refer to**.

---

## 0.2.6 Release of PowerPlug
The first full release of PowerPlug comes with a variety of bug fixes and improvements. Versions 0.2.1-beta, 0.2.2, 0.2.3, 0.2.4, and 0.2.5 were tested internally.

Install PowerPlug from the **PowerShell Gallery** with:

```powershell
Install-Module -Name PowerPlug
```

### Features
- New `Get-Help` documentation
- Improved Performance
- [PowerPlugDocs](https://powerplug.me)

### Bug Fixes
- Fixed an issue where a Set-Byname would not occur
- Fixed an issue where Remove-Byname would not remove the alias
- Fixed an issue where Set-Byname would not replace old aliases with the new one
- Other internal fixes

---

## 0.2.1-alpha Release of PowerPlug
The second and patch release of PowerPlug is now available on the PowerShell gallery as a prerelease available here: [PowerPlug](https://www.powershellgallery.com/packages/PowerPlug/0.2.1-alpha)

Install PowerPlug to PowerShell with:
```powershell
Install-Module -Name PowerPlug -AllowPrerelease
```

### Features
- **New-Byname** - for creating a new alias to your Profile across various sessions
  - ```powershell
    New-Byname [-Name] <string> [-Value] <string> [-Description <string>] [-Option {None | ReadOnly | Constant | Private | AllScope
    | Unspecified}] [-PassThru] [-WhatIf] [-Confirm] [-Scope {Global | Local | Private | Numbered scopes | Script}] [-Force]
    [<CommonParameters>]
    ``` 
  - The underlying structure of the command is the same as New-Alias, however, `New-Byname` writes to the user's `$PROFILE`

- **Set-Byname** - for setting an alias to your Profile across various sessions
  - ```powershell
    Set-Byname [-Name] <string> [-Value] <string> [-Description <string>] [-Option {None | ReadOnly | Constant | Private | AllScope
    | Unspecified}] [-PassThru] [-WhatIf] [-Confirm] [-Scope {Global | Local | Private | Numbered scopes | Script}] [-Force]
    [<CommonParameters>]
    ``` 
  - The underlying structure of the command is the same as Set-Alias, however, `Set-Byname` writes to the user's `$PROFILE`

- **Remove-Byname** - for removing an alias to your Profile across various sessions
  - ```powershell
    Remove-Byname [-Name] <string> [-Scope {Global | Local | Private | Numbered scopes | Script}] [-Force] [<CommonParameters>]
    ``` 
  - The underlying structure of the command is the same as Remove-Alias, however, `Remove-Byname` removes and "bynames" from the  user's `$PROFILE`

---

## 0.1.0-alpha Release of PowerPlug
The initial release of PowerPlug is now available.

Import the PowerPlug dll to PowerShell with:
```powershell
ipmo "path/to/PowerPlug.dll"
```

### Features
- **Move-Trash** - Moves a file or directory to the Recycle Bin instead of erasing it off the system.
  - ```powershell
    Move-Trash [-Path] <string> [[-List]] [<CommonParameters>]
    ```

- **Compare-Hash** - Compares a file hash with a known signature and displays whether the signature was a match
  - ```powershell
    Compare-Hash [-Hash] {SHA256 | SHA512 | MD5} [-Path] <string> [-Signature] <string> [<CommonParameters>]
    ```