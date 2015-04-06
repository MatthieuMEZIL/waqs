param($installPath, $toolsPath, $package)

$path = "HKCU:\Software\Microsoft\VisualStudio\" + $DTE.Version + "_Config"
$ttIncludePath = (Get-Item (Join-Path $path "TextTemplating\IncludeFolders\.tt")).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE")

if ($ttIncludePath -eq $null)
{
    throw 'You must install WAQS vsix first.'
}

if ($ttIncludePath.EndsWith("\"))
{ 
    $ttIncludePath = $ttIncludePath.SubString(0, $ttIncludePath.Length - 1) 
}

if (-not (Test-Path (Join-Path $ttIncludePath 'Microsoft.CodeAnalysis.CSharp.Workspaces.1.0.0-rc1')))
{
    $NuGetPath = Join-Path $toolsPath 'NuGet.exe'
    & $NuGetPath install Microsoft.CodeAnalysis.CSharp.Workspaces -Version 1.0.0-rc1 -Prerelease -OutputDirectory ($ttIncludePath)
    [System.IO.File]::WriteAllText((Join-Path $ttIncludePath 'WAQS.Roslyn.Assemblies.ttinclude'), '<#@ assembly name="'+$ttIncludePath+'\System.Collections.Immutable.1.1.33-beta\lib\portable-net45+win8+wp8+wpa81\System.Collections.Immutable.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Workspaces.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.Workspaces.Common.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.Workspaces.Desktop.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Workspaces.dll" #>'+[System.Environment]::NewLine + '<#@ assembly name="'+$ttIncludePath+'\Microsoft.CodeAnalysis.CSharp.Workspaces.1.0.0-rc1\lib\net45\Microsoft.CodeAnalysis.CSharp.Workspaces.Desktop.dll" #>')
}
