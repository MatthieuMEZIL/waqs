function Build-NuPkg([string] $nuspecFile)
{
    echo "Updating '$nuspecFile'"

    [xml]$nuspec = Get-Content $nuspecFile
    $digits = $nuspec.package.metadata.version -split '\.'
    $digits[3] = $($($digits[3] -as [Int32]) + 1) -as [String]
    $version = $digits -join '.'

    $command =  ".\New-NuGetPackage.ps1 -NuSpecFilePath ""$nuspecFile"" -VersionNumber $version -ReleaseNotes ""Version $version"" -NoPrompt"
    echo $command

    .\New-NuGetPackage.ps1 -NuSpecFilePath "$nuspecFile" -VersionNumber $version -ReleaseNotes "Version $version" -NoPrompt
}

msbuild '.\NuGet Programs\WAQSNuGetPrograms.sln'
if ($LASTEXITCODE -eq 0)
{
    dir *.nuspec | % { Build-NuPkg($_.FullName) }
}
