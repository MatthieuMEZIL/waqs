param($installPath, $toolsPath, $package)

$path = "HKCU:\Software\Microsoft\VisualStudio\" + $DTE.Version + "_Config"
$ttIncludePath = (Get-Item (Join-Path $path "TextTemplating\IncludeFolders\.tt")).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE")

if ($ttIncludePath -eq $null)
{
    throw 'You must install WAQS vsix first.'
}

if ($ttIncludePath.EndsWith("\"))
{ 
    $ttIncludePath = $ttIncludePath.Substring(0, $ttIncludePath.Length - 1) 
}

$roslynAssemblies = Join-Path $ttIncludePath 'WAQS.Roslyn.Assemblies.ttinclude'
if (-not (Test-Path $roslynAssemblies))
{
    if (Test-Path "HKCU:\Software\Microsoft\VisualStudio\14.0_Config")
    {
        $assembliesPath = Join-Path (Get-Item HKCU:\Software\Microsoft\VisualStudio\14.0_Config).GetValue("InstallDir") 'PrivateAssemblies'
        [System.IO.File]::WriteAllText((Join-Path $ttIncludePath 'WAQS.Roslyn.Assemblies.ttinclude'), '<#@ assembly name="'+$assembliesPath+'\System.Reflection.Metadata.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\System.Collections.Immutable.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.CSharp.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.CSharp.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.Workspaces.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$assembliesPath+'\System.Composition.TypedParts.dll" #>')
    }
    else
    {
        $NuGetPath = Join-Path $toolsPath 'NuGet.exe'
        & $NuGetPath install Microsoft.CodeAnalysis.CSharp.Workspaces -Version 1.0.0-rc1 -Prerelease -OutputDirectory ($ttIncludePath) -Source 'https://www.nuget.org/api/v2/'
        [System.IO.File]::WriteAllText((Join-Path $ttIncludePath 'WAQS.Roslyn.Assemblies.ttinclude'), '<#@ assembly name="'+$ttIncludePath+'\System.Reflection.Metadata.1.0.18-beta\lib\portable-net45+win8\System.Reflection.Metadata.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\System.Collections.Immutable.1.1.33-beta\lib\portable-net45+win8+wp8+wpa81\System.Collections.Immutable.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Workspaces.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Workspaces.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Workspaces.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Workspaces.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.Composition.1.0.27\lib\portable-net45+win8+wp8+wpa81\System.Composition.TypedParts.dll" #>')
    }
}
