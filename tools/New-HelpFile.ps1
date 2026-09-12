<#
.SYNOPSIS
    Builds PowerPlug.dll-Help.xml from the compiled assembly and its XML documentation file.

.DESCRIPTION
    Reads every cmdlet in PowerPlug.dll through reflection, pulls the synopsis, description, parameter
    descriptions and examples out of the <para type="..."> tags in the C# XML doc comments, and writes a
    MAML help file that Get-Help understands. Run this after adding or changing a cmdlet.

.PARAMETER Configuration
    Build configuration to read from. Defaults to Debug.

.PARAMETER Framework
    Target framework folder to read from. Defaults to net8.0 so the script works on PowerShell 7.4+.

.PARAMETER OutputPath
    Where to write the help file. Defaults to PowerPlug/PowerPlug.dll-Help.xml in the repo.

.EXAMPLE
    ./tools/New-HelpFile.ps1

.EXAMPLE
    dotnet build -c Release; ./tools/New-HelpFile.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [string] $Framework = 'net8.0',
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$binDir = Join-Path $repoRoot 'PowerPlug' 'bin' $Configuration $Framework
$assemblyPath = Join-Path $binDir 'PowerPlug.dll'
$docPath = Join-Path $binDir 'PowerPlug.xml'
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot 'PowerPlug' 'PowerPlug.dll-Help.xml'
}

foreach ($required in $assemblyPath, $docPath) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Missing '$required'. Run 'dotnet build -c $Configuration' first."
    }
}

# Load into a separate context so the script never holds a lock on the build output.
$context = [System.Runtime.Loader.AssemblyLoadContext]::new('PowerPlugHelp', $true)
try {
    $assembly = $context.LoadFromAssemblyPath((Resolve-Path -LiteralPath $assemblyPath).Path)
    $docs = [xml](Get-Content -LiteralPath $docPath -Raw)

    $memberDocs = @{}
    foreach ($member in $docs.doc.members.member) {
        $memberDocs[$member.name] = $member
    }

    function Get-DocNode {
        param([string] $Key)
        if ($memberDocs.ContainsKey($Key)) { return $memberDocs[$Key] }
        return $null
    }

    function Get-Para {
        param($Node, [string] $Type)
        if (-not $Node) { return $null }
        $para = @($Node.SelectNodes("summary/para[@type='$Type']"))
        if ($para.Count -eq 0) { return $null }
        return (($para | ForEach-Object { $_.InnerText }) -join ' ') -replace '\s+', ' ' | ForEach-Object Trim
    }

    function Get-PlainSummary {
        param($Node)
        if (-not $Node -or -not $Node.summary) { return $null }
        return ($Node.summary.InnerText -replace '\s+', ' ').Trim()
    }

    function Get-Examples {
        param($Node)
        if (-not $Node) { return @() }
        foreach ($example in @($Node.SelectNodes('summary/example'))) {
            $para = $example.SelectSingleNode('para')
            $code = $example.SelectSingleNode('code')
            [pscustomobject]@{
                Title = if ($para) { $para.InnerText.Trim() } else { '' }
                Code  = if ($code) { $code.InnerText.Trim() } else { '' }
            }
        }
    }

    function Get-TypeKey {
        param([type] $Type)
        return 'T:' + $Type.FullName
    }

    function Get-PropertyKey {
        param([System.Reflection.PropertyInfo] $Property)
        return 'P:' + $Property.DeclaringType.FullName + '.' + $Property.Name
    }

    function Get-ParameterTypeName {
        param([type] $Type)
        if ($Type -eq [System.Management.Automation.SwitchParameter]) { return 'SwitchParameter' }
        if ($Type.IsArray) { return (Get-ParameterTypeName $Type.GetElementType()) + '[]' }
        $underlying = [Nullable]::GetUnderlyingType($Type)
        if ($underlying) { return Get-ParameterTypeName $underlying }
        switch ($Type.FullName) {
            'System.String' { return 'String' }
            'System.Int32' { return 'Int32' }
            'System.Int64' { return 'Int64' }
            'System.Double' { return 'Double' }
            'System.Boolean' { return 'Boolean' }
            'System.Object' { return 'Object' }
            'System.DateTime' { return 'DateTime' }
            default { return $Type.Name }
        }
    }

    $cmdletTypes = $assembly.GetTypes() |
        Where-Object { -not $_.IsAbstract -and $_.IsSubclassOf([System.Management.Automation.PSCmdlet]) } |
        Sort-Object { $_.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false)[0].NounName },
                    { $_.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false)[0].VerbName }

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)

    $mamlNs = 'http://schemas.microsoft.com/maml/2004/10'
    $commandNs = 'http://schemas.microsoft.com/maml/dev/command/2004/10'
    $devNs = 'http://schemas.microsoft.com/maml/dev/2004/10'

    $writer = [System.Xml.XmlWriter]::Create($OutputPath, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('helpItems', 'http://msh')
        $writer.WriteAttributeString('schema', 'maml')

        foreach ($type in $cmdletTypes) {
            $cmdletAttr = $type.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false)[0]
            $name = "$($cmdletAttr.VerbName)-$($cmdletAttr.NounName)"
            $doc = Get-DocNode (Get-TypeKey $type)
            $synopsis = Get-Para $doc 'synopsis'
            $description = Get-Para $doc 'description'
            if (-not $synopsis) { Write-Warning "$name has no <para type='synopsis'>"; $synopsis = $name }
            if (-not $description) { $description = $synopsis }

            $aliases = @($type.GetCustomAttributes([System.Management.Automation.AliasAttribute], $false) | ForEach-Object AliasNames)
            $outputTypes = @($type.GetCustomAttributes([System.Management.Automation.OutputTypeAttribute], $false) | ForEach-Object { $_.Type } | ForEach-Object { $_.Name })

            $parameters = foreach ($property in $type.GetProperties()) {
                $paramAttrs = @($property.GetCustomAttributes([System.Management.Automation.ParameterAttribute], $true))
                if ($paramAttrs.Count -eq 0) { continue }
                $paramDoc = Get-DocNode (Get-PropertyKey $property)
                $paramDescription = Get-Para $paramDoc 'description'
                if (-not $paramDescription) { $paramDescription = Get-PlainSummary $paramDoc }
                if (-not $paramDescription) { $paramDescription = $property.Name }
                $validateSet = $property.GetCustomAttributes([System.Management.Automation.ValidateSetAttribute], $true) | Select-Object -First 1
                $paramAliases = @($property.GetCustomAttributes([System.Management.Automation.AliasAttribute], $true) | ForEach-Object AliasNames)

                [pscustomobject]@{
                    Name         = $property.Name
                    Type         = Get-ParameterTypeName $property.PropertyType
                    Description  = $paramDescription
                    Sets         = $paramAttrs
                    ValidValues  = @(if ($validateSet) { $validateSet.ValidValues })
                    Aliases      = @($paramAliases)
                    IsSwitch     = $property.PropertyType -eq [System.Management.Automation.SwitchParameter]
                }
            }

            $writer.WriteStartElement('command', 'command', $commandNs)
            $writer.WriteAttributeString('xmlns', 'maml', $null, $mamlNs)
            $writer.WriteAttributeString('xmlns', 'dev', $null, $devNs)

            $writer.WriteStartElement('command', 'details', $commandNs)
            $writer.WriteElementString('command', 'name', $commandNs, $name)
            $writer.WriteElementString('command', 'verb', $commandNs, $cmdletAttr.VerbName)
            $writer.WriteElementString('command', 'noun', $commandNs, $cmdletAttr.NounName)
            $writer.WriteStartElement('maml', 'description', $mamlNs)
            $writer.WriteElementString('maml', 'para', $mamlNs, $synopsis)
            $writer.WriteEndElement()
            $writer.WriteEndElement()

            $writer.WriteStartElement('maml', 'description', $mamlNs)
            $writer.WriteElementString('maml', 'para', $mamlNs, $description)
            if ($aliases.Count -gt 0) {
                $writer.WriteElementString('maml', 'para', $mamlNs, "Aliases: $($aliases -join ', ')")
            }
            $writer.WriteEndElement()

            # One syntax block per parameter set (or a single block when no sets are declared).
            $setNames = @($parameters | ForEach-Object { $_.Sets } | ForEach-Object { $_.ParameterSetName } | Sort-Object -Unique | Where-Object { $_ -ne '__AllParameterSets' })
            if ($setNames.Count -eq 0) { $setNames = @('__AllParameterSets') }

            $writer.WriteStartElement('command', 'syntax', $commandNs)
            foreach ($setName in $setNames) {
                $writer.WriteStartElement('command', 'syntaxItem', $commandNs)
                $writer.WriteElementString('maml', 'name', $mamlNs, $name)
                $inSet = $parameters | Where-Object { $_.Sets | Where-Object { $_.ParameterSetName -eq $setName -or $_.ParameterSetName -eq '__AllParameterSets' } }
                foreach ($parameter in ($inSet | Sort-Object { $attr = $_.Sets | Where-Object { $_.ParameterSetName -in $setName, '__AllParameterSets' } | Select-Object -First 1; if ($attr.Position -ge 0) { $attr.Position } else { 999 } }, Name)) {
                    $attr = $parameter.Sets | Where-Object { $_.ParameterSetName -in $setName, '__AllParameterSets' } | Select-Object -First 1
                    $writer.WriteStartElement('command', 'parameter', $commandNs)
                    $writer.WriteAttributeString('required', $attr.Mandatory.ToString().ToLowerInvariant())
                    $writer.WriteAttributeString('position', $(if ($attr.Position -ge 0) { $attr.Position } else { 'named' }))
                    $writer.WriteAttributeString('pipelineInput', $(if ($attr.ValueFromPipeline -and $attr.ValueFromPipelineByPropertyName) { 'true (ByValue, ByPropertyName)' } elseif ($attr.ValueFromPipeline) { 'true (ByValue)' } elseif ($attr.ValueFromPipelineByPropertyName) { 'true (ByPropertyName)' } else { 'false' }))
                    $writer.WriteElementString('maml', 'name', $mamlNs, $parameter.Name)
                    if (-not $parameter.IsSwitch) {
                        $writer.WriteStartElement('command', 'parameterValue', $commandNs)
                        $writer.WriteAttributeString('required', 'true')
                        $writer.WriteString($parameter.Type)
                        $writer.WriteEndElement()
                    }
                    $writer.WriteEndElement()
                }
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()

            $writer.WriteStartElement('command', 'parameters', $commandNs)
            foreach ($parameter in ($parameters | Sort-Object Name)) {
                $attr = $parameter.Sets | Select-Object -First 1
                $writer.WriteStartElement('command', 'parameter', $commandNs)
                $writer.WriteAttributeString('required', $attr.Mandatory.ToString().ToLowerInvariant())
                $writer.WriteAttributeString('position', $(if ($attr.Position -ge 0) { $attr.Position } else { 'named' }))
                $writer.WriteAttributeString('pipelineInput', $(if ($attr.ValueFromPipeline -and $attr.ValueFromPipelineByPropertyName) { 'true (ByValue, ByPropertyName)' } elseif ($attr.ValueFromPipeline) { 'true (ByValue)' } elseif ($attr.ValueFromPipelineByPropertyName) { 'true (ByPropertyName)' } else { 'false' }))
                if (@($parameter.Aliases).Count -gt 0) {
                    $writer.WriteAttributeString('aliases', ($parameter.Aliases -join ', '))
                }
                $writer.WriteElementString('maml', 'name', $mamlNs, $parameter.Name)
                $writer.WriteStartElement('maml', 'description', $mamlNs)
                $writer.WriteElementString('maml', 'para', $mamlNs, $parameter.Description)
                if (@($parameter.ValidValues).Count -gt 0) {
                    $writer.WriteElementString('maml', 'para', $mamlNs, "Accepted values: $($parameter.ValidValues -join ', ')")
                }
                $writer.WriteEndElement()
                $writer.WriteStartElement('command', 'parameterValue', $commandNs)
                $writer.WriteAttributeString('required', $(if ($parameter.IsSwitch) { 'false' } else { 'true' }))
                $writer.WriteString($parameter.Type)
                $writer.WriteEndElement()
                $writer.WriteStartElement('dev', 'type', $devNs)
                $writer.WriteElementString('maml', 'name', $mamlNs, $parameter.Type)
                $writer.WriteEndElement()
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()

            if ($outputTypes.Count -gt 0) {
                $writer.WriteStartElement('command', 'returnValues', $commandNs)
                foreach ($outputType in ($outputTypes | Sort-Object -Unique)) {
                    $writer.WriteStartElement('command', 'returnValue', $commandNs)
                    $writer.WriteStartElement('dev', 'type', $devNs)
                    $writer.WriteElementString('maml', 'name', $mamlNs, $outputType)
                    $writer.WriteEndElement()
                    $writer.WriteEndElement()
                }
                $writer.WriteEndElement()
            }

            $examples = @(Get-Examples $doc)
            if ($examples.Count -gt 0) {
                $writer.WriteStartElement('command', 'examples', $commandNs)
                $index = 0
                foreach ($example in $examples) {
                    $index++
                    $writer.WriteStartElement('command', 'example', $commandNs)
                    $writer.WriteElementString('maml', 'title', $mamlNs, "-------------------------- EXAMPLE $index --------------------------")
                    $writer.WriteElementString('dev', 'code', $devNs, $example.Code)
                    $writer.WriteStartElement('dev', 'remarks', $devNs)
                    $writer.WriteElementString('maml', 'para', $mamlNs, $example.Title)
                    $writer.WriteEndElement()
                    $writer.WriteEndElement()
                }
                $writer.WriteEndElement()
            }

            $writer.WriteStartElement('maml', 'relatedLinks', $mamlNs)
            $writer.WriteStartElement('maml', 'navigationLink', $mamlNs)
            $writer.WriteElementString('maml', 'linkText', $mamlNs, 'PowerPlug on GitHub')
            $writer.WriteElementString('maml', 'uri', $mamlNs, 'https://github.com/manu-p-1/PowerPlug')
            $writer.WriteEndElement()
            if ($cmdletAttr.HelpUri) {
                $writer.WriteStartElement('maml', 'navigationLink', $mamlNs)
                $writer.WriteElementString('maml', 'linkText', $mamlNs, 'Online Version')
                $writer.WriteElementString('maml', 'uri', $mamlNs, $cmdletAttr.HelpUri)
                $writer.WriteEndElement()
            }
            $writer.WriteEndElement()

            $writer.WriteEndElement() # command
        }

        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }

    Write-Host "Wrote help for $($cmdletTypes.Count) cmdlets to $OutputPath"
}
finally {
    $context.Unload()
}
