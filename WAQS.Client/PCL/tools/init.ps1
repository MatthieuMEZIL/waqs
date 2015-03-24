param($installPath, $toolsPath, $package)

#Copy .ttinclude
$path = "HKCU:\Software\Microsoft\VisualStudio\" + $DTE.Version + "_Config"
$ttIncludePath = (Get-Item (Join-Path $path "TextTemplating\IncludeFolders\.tt")).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE")
if ($ttIncludePath -eq $null)
{
	throw "You must install WCF Async Queryable Services vsix outside Visual Studio first. See WAQS documentation on http://msmvps.com/blogs/matthieu/archive/2013/12/13/how-to-use-waqs.aspx"
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

foreach ($file in [System.IO.Directory]::GetFiles($ttincludePath))
{
    if (Test-Path "HKLM:\Software\Wow6432Node\Microsoft\Microsoft SDKs\Windows\")
    {
        $badx = '.x86'
        $goodx = '.x64'
    }
    else
    {
        $badx = '.x64'
        $goodx = '.x86'
    }
    if ($file.EndsWith($goodx))
    {
        $newFile = $file.SubString(0, $file.Length-$goodx.Length)
        if ((-not (Test-Path $newFile)) -or ([System.IO.File]::ReadAllText($newFile) -ne [System.IO.File]::ReadAllText($file)))
        {
            Copy-Item $file $newFile
        }
    }
}

foreach ($_ in Get-Module | ?{$_.Name -eq 'WCFAsyncQueryableFunctionsClientPCL'})
{
    Remove-Module 'WCFAsyncQueryableFunctionsClientPCL'
    break
}    

Import-Module (Join-Path $toolsPath WCFAsyncQueryableFunctionsClientPCL.psm1)
