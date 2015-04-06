param($installPath, $toolsPath, $package)

#Copy .ttinclude
$path = "HKCU:\Software\Microsoft\VisualStudio\" + $DTE.Version + "_Config"
$ttIncludePath = (Get-Item (Join-Path $path "TextTemplating\IncludeFolders\.tt")).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE")

if ($ttIncludePath -eq $null)
{
    throw 'You must install WAQS vsix first.'
}

if (-not (Test-Path $ttIncludePath))
{
	New-Item $ttincludePath -ItemType directory
}

switch($DTE.Version)
{
	"10.0" { $vsVersion = "VS10" }
	"11.0" { $vsVersion = "VS11" }
	"12.0" { $vsVersion = "VS12" }
	"14.0" { $vsVersion = "VS14" }
}

$ttIncludeSource = "$toolsPath\ttincludes" 
foreach ($file in [System.IO.Directory]::GetFiles($ttIncludeSource))
{
    $ttIncludeFile = Join-Path $ttincludePath ([System.IO.Path]::GetFileName($file))
	if ((-not (Test-Path $ttIncludeFile)) -or ([System.IO.File]::ReadAllText($ttIncludeFile) -ne [System.IO.File]::ReadAllText($file)))
	{
		copy $file $ttincludePath
	}
}
$ttIncludeSource = Join-Path $ttIncludeSource $vsVersion
foreach ($file in [System.IO.Directory]::GetFiles($ttIncludeSource))
{
    $ttIncludeFile = Join-Path $ttincludePath ([System.IO.Path]::GetFileName($file))
	if ((-not (Test-Path $ttIncludeFile)) -or ([System.IO.File]::ReadAllText($ttIncludeFile) -ne [System.IO.File]::ReadAllText($file)))
	{
		copy $file $ttincludePath
	}
}

foreach ($_ in Get-Module | ?{$_.Name -eq 'WCFAsyncQueryableFunctionsServerMock'})
{
    Remove-Module 'WCFAsyncQueryableFunctionsServerMock'
    break
}
    

#Import Module
Import-Module (Join-Path $toolsPath WCFAsyncQueryableFunctionsServerMock.psm1)