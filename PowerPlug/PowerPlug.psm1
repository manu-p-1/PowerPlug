# PowerPlug module loader.
# Picks the build that matches the running .NET version so one module folder serves PowerShell 7.4 through 7.6+.

$ErrorActionPreference = 'Stop'

function Find-PowerPlugAssembly {
    # Development layout: the DLL sits next to this file (bin/Debug/<tfm>).
    $local = Join-Path $PSScriptRoot 'PowerPlug.dll'
    if (Test-Path -LiteralPath $local) {
        return $local
    }

    # Packaged layout: lib/<tfm>/PowerPlug.dll. Use the newest framework the runtime can load.
    $libRoot = Join-Path $PSScriptRoot 'lib'
    if (-not (Test-Path -LiteralPath $libRoot)) {
        throw "PowerPlug.dll was not found under '$PSScriptRoot'. Build the project or reinstall the module."
    }

    $runtimeMajor = [System.Environment]::Version.Major
    $candidates = Get-ChildItem -LiteralPath $libRoot -Directory |
        Where-Object { $_.Name -match '^net(\d+)\.0$' -and [int]$Matches[1] -le $runtimeMajor } |
        Sort-Object { [int]($_.Name -replace '^net(\d+)\.0$', '$1') } -Descending

    foreach ($candidate in $candidates) {
        $dll = Join-Path $candidate.FullName 'PowerPlug.dll'
        if (Test-Path -LiteralPath $dll) {
            return $dll
        }
    }

    throw "No PowerPlug build is compatible with .NET $runtimeMajor. PowerPlug needs PowerShell 7.4 or later."
}

$assemblyPath = Find-PowerPlugAssembly
Import-Module -Name $assemblyPath -ErrorAction Stop

Export-ModuleMember -Cmdlet * -Alias *
