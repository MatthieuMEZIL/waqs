param (
    [Parameter(Mandatory=$false)]
    [AllowEmptyString()]
    [String] $BuildKind
)

function Build-NuPkg([string] $nuspecFile)
{
	Write-Host -ForegroundColor Cyan "Building '$([IO.Path]::GetFileName($nuspecFile))'"

    [xml]$nuspec = Get-Content $nuspecFile
	$version = $nuspec.package.metadata.version

    if (($BuildKind -eq 'All') -or ([IO.Path]::GetFileName($nuspecFile) -ne 'WAQS.RoslynDeploy.nuspec'))
    {
		$digits = $version -split '\.'
		$digits[$digits.Length - 1] = $($($digits[$digits.Length - 1] -as [Int32]) + 1) -as [String]
		$version = $digits -join '.'
    }
    .\New-NuGetPackage.ps1 -NuSpecFilePath "$nuspecFile" -VersionNumber $version -ReleaseNotes "Version $version" -NoPrompt
    return $version
}

function Generate-SpecificationsFile()
{
    $file = '.\ttincludes\WAQS.Specifications.Dependences.ttinclude'
    Write-Host -ForegroundColor Cyan  "Building '$file'"

    $list = New-Object "System.Collections.Generic.List``1[System.String]"
    foreach ($item in $(Get-Content '.\TestsDependences\TestsDependences\GetMembersVisitor.cs'))
    {
        $list.Add($item)
    }

    # remove using statements and namespace
    while ($list[0] -ne '{')
    {
        $list.RemoveAt(0);
    }
    $list.RemoveAt(0)
    $list.Reverse()
    while ($list[0] -ne '}')
    {
        $list.RemoveAt(0);
    }
    $list.RemoveAt(0)
    $list.Reverse()

    # strip leading spaces
    for ($i = 0; $i -lt $list.Count; $i++)
    {
        $line = $list[$i]
        if ($line.Length -gt 3 -and [String]::IsNullOrWhitespace($line.SubString(0, 4)))
        {
            $list[$i] = $line.SubString(4)
        }
    }

    # add new lines
    $list.Insert(0, '')
    $list.Insert(0, '// Generated by build script.')
    $list.Insert(0, '// Copyright (c) Matthieu MEZIL.  All rights reserved.')
    $list.Insert(0, '<#+')
    $list.Add('#>')

    # update the file
    $list | Set-Content $file
}

msbuild '.\NuGet Programs\WAQSNuGetPrograms.sln' /p:Platform=x86
if ($LASTEXITCODE)
{
    exit $LASTEXITCODE
}
Copy-Item -Path ".\NuGet Programs\InitWAQSServer\bin\Debug\InitWAQSServer.exe" -Destination .\WAQS.Server\tools\Server
Copy-Item -Path ".\NuGet Programs\InitWAQSServerMock\bin\Debug\InitWAQSServerMock.exe" -Destination .\WAQS.Server.Mock\tools\Server.Mock
Copy-Item -Path ".\NuGet Programs\InitWAQSClientWPF\bin\Debug\InitWAQSClientWPF.exe" -Destination .\WAQS.Client\WPF\tools\Client.WPF
Copy-Item -Path ".\NuGet Programs\InitWAQSClientWPFGlobal\bin\Debug\InitWAQSClientWPFGlobal.exe" -Destination .\WAQS.Client\WPF\tools\Client.WPF
Copy-Item -Path ".\NuGet Programs\InitWAQSClientPCL\bin\Debug\InitWAQSClientPCL.exe" -Destination .\WAQS.Client\PCL\tools\Client.PCL
Copy-Item -Path ".\NuGet Programs\InitWAQSClientPCLGlobal\bin\Debug\InitWAQSClientPCLGlobal.exe" -Destination .\WAQS.Client\PCL\tools\Client.PCL

msbuild '.\TestsDependences\TestsDependences.sln' /p:Platform="Any CPU"
if ($LASTEXITCODE)
{
    exit $LASTEXITCODE
}

vstest.console '.\TestsDependences\TestsDependences\bin\Debug\TestsDependences.dll'
if ($LASTEXITCODE)
{
    exit $LASTEXITCODE
}

Generate-SpecificationsFile

$waqsRoslynDeployVersion = dir WAQS.RoslynDeploy.nuspec | % { Build-NuPkg($_.FullName) }
$match = [System.Text.RegularExpressions.Regex]::Match($waqsRoslynDeployVersion, '.(?<version>(\d+.)?\d+.\d+.\d+$)')
$waqsRoslynDeployVersion = $match.Groups["version"].Value
$null = [System.Reflection.Assembly]::Load('System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')
$ns = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"
$waqsNuspec = dir WAQS.nuspec
$waqsXdoc = [System.Xml.Linq.XDocument]::Load($waqsNuspec.FullName)
$waqsDependencies = $waqsXdoc.Root.Element([System.Xml.Linq.XName]::Get("metadata", $ns)).Element([System.Xml.Linq.XName]::Get("dependencies", $ns)).Elements([System.Xml.Linq.XName]::Get("dependency", $ns))
foreach ($nuspec in (dir *.nuspec | ?{ $_.Name -ne 'WAQS.RoslynDeploy.nuspec' -and $_.Name -ne 'WAQS.nuspec' }))
{
    if (-not ($nuspec.Name -eq 'WAQS.RoslynDeploy.nuspec'))
    {
        $xdoc = [System.Xml.Linq.XDocument]::Load($nuspec.FullName)
        $waqsRoslynDeployDependency = $xdoc.Root.Element([System.Xml.Linq.XName]::Get("metadata", $ns)).Element([System.Xml.Linq.XName]::Get("dependencies", $ns)).Elements([System.Xml.Linq.XName]::Get("dependency", $ns)) | ?{$_.Attribute("id").Value -eq 'WAQS.RoslynDeploy'}
        $waqsRoslynDeployDependency.Attribute("version").Value = $waqsRoslynDeployVersion
        $null = $xdoc.Save($nuspec.FullName)
        $version = % { Build-NuPkg($nuspec.FullName) }
		$match = [System.Text.RegularExpressions.Regex]::Match($version, '.(?<version>(\d+.)?\d+.\d+.\d+$)')
		$version = $match.Groups["version"].Value
		$waqsDependencyElement = $waqsDependencies | ?{ $_.Attribute("id").Value -eq ([IO.Path]::GetFileNameWithoutExtension($nuspec.Name))}
		if ($waqsDependencyElement -ne $null)
		{
			$waqsDependencyElement.Attribute("version").Value = $version
		}
    }
}
$null = $waqsXdoc.Save($waqsNuspec.FullName)
$null = % { Build-NuPkg($waqsNuspec.FullName) }

