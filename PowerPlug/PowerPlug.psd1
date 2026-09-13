@{
    RootModule           = 'PowerPlug.psm1'
    ModuleVersion        = '1.0.0'
    GUID                 = 'b0608836-251b-4d25-9671-d09526a35d28'
    Author               = 'Manu Puduvalli'
    CompanyName          = 'The PowerPlug Authors'
    Copyright            = 'Copyright (c) Manu Puduvalli 2021-2026'
    Description          = 'A cross-platform PowerShell 7+ cmdlet utility library for day to day development, DevOps and general tooling.'
    PowerShellVersion    = '7.4'
    CompatiblePSEditions = @('Core')
    FormatsToProcess     = @('PowerPlug.Format.ps1xml')

    CmdletsToExport      = @(
        # Aliases (Byname)
        'New-Byname', 'Set-Byname', 'Remove-Byname'
        # Data and encoding
        'ConvertTo-Base64', 'ConvertFrom-Base64'
        'ConvertTo-UrlEncoding', 'ConvertFrom-UrlEncoding'
        'ConvertFrom-UnixTime', 'ConvertTo-UnixTime'
        'ConvertFrom-Jwt', 'ConvertTo-HashTable', 'Convert-Color'
        # Security
        'Compare-Hash', 'Get-StringHash', 'New-RandomString', 'New-SecureKey', 'Test-Elevation'
        # File system
        'Move-Trash', 'New-TemporaryDirectory', 'Rename-BatchItem'
        'Get-DirectorySize', 'Find-DuplicateFile', 'Remove-EmptyDirectory'
        'Get-FileEncoding', 'Convert-LineEnding', 'Set-FileTimestamp'
        'Get-LargestFile', 'Compare-Directory', 'Wait-File'
        # Networking
        'Get-Speed', 'Get-NetworkInfo', 'Test-Port', 'Wait-Port', 'Test-Url'
        'Get-TlsCertificate', 'Get-PublicIPAddress', 'Get-ListeningPort'
        # Shell and environment
        'Get-EnvironmentPath', 'Add-EnvironmentPath', 'Remove-EnvironmentPath'
        'Import-DotEnv', 'Get-SystemInfo'
        # Diagnostics
        'Invoke-Retry', 'Measure-ScriptBlock', 'Watch-Command'
    )

    AliasesToExport      = @(
        'nbn', 'sbn', 'rbn'
        'tobase64', 'frombase64', 'urlencode', 'urldecode', 'fromepoch', 'toepoch', 'fromjwt', 'toht', 'color'
        'csh', 'strhash', 'nrs', 'randstr', 'nsk', 'isadmin', 'Test-Administrator'
        'trash', 'ntd', 'Rename-Batch', 'dirsize', 'dupes', 'rmempty'
        'gfe', 'eol', 'touch', 'bigfiles', 'dirdiff', 'waitfile'
        'speedtest', 'gspd', 'gni', 'netinfo', 'tp', 'waitport', 'turl', 'gtls', 'Get-SslCertificate'
        'pubip', 'Get-PublicIP', 'lsport', 'Get-OpenPort'
        'gpath', 'addpath', 'rmpath', 'dotenv', 'sysinfo'
        'retry', 'msb', 'watchcmd'
    )

    FunctionsToExport    = @()
    VariablesToExport    = @()

    PrivateData          = @{
        PSData = @{
            Tags         = @('utilities', 'devops', 'networking', 'encoding', 'filesystem', 'cross-platform', 'PSEdition_Core', 'Windows', 'Linux', 'MacOS')
            LicenseUri   = 'https://www.gnu.org/licenses/gpl-3.0.en.html'
            ProjectUri   = 'https://github.com/manu-p-1/PowerPlug'
            IconUri      = 'https://raw.githubusercontent.com/manu-p-1/PowerPlug/master/assets/PowerPlugIcon.png'
            ReleaseNotes = 'https://github.com/manu-p-1/PowerPlug/blob/master/CHANGELOG.md'
            # The Gallery only allows letters and digits here, so this is rc1 rather than rc.1.
            Prerelease   = 'rc1'
        }
    }
}
