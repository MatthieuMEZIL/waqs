function GetToolsPath()
{
    $modules = (Get-Module WCFAsyncQueryableFunctionsServerMock | select -property path)
    if ($modules.Length -eq $null -or $modules.Length -eq 1)
    {
        return [System.IO.Path]::GetDirectoryName($modules.Path)
    }
    else
    {
        return [System.IO.Path]::GetDirectoryName($modules[-1].Path)
    }
}

function GetVersion()
{
    switch([System.Text.RegularExpressions.Regex]::Match(((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"} | select -ExpandProperty Value), "Version=v(\d.\d)").Groups[1].Value)
    {
        "4.0" {return "NET40"}
        "4.5" {return "NET45"}
        "4.6" {return "NET46"}
    }
    return $null
}

function GetAvailableVersions()
{
    switch ($DTE.Version)
    {
        '10.0' {$version = @("NET40")}
        '11.0' {$version = @("NET40", "NET45")}
        '12.0' {$version = @("NET40", "NET45", "NET46")}
        '14.0' {$version = @("NET40", "NET45", "NET46")}
    }
    return $version
}

function GetAllProjectItems($parent)
{
    $values = ($parent.ProjectItems | select *)
    $result = @()
    foreach ($value in $values)
    {
        if ($value -ne $parent -and $value.Name -ne $null -and (-not $value.Name.EndsWith(".tt")))
        {
            $result = $result + $value
            $result = $result + (GetAllProjectItems($value))
            $result = $result + (GetAllProjectItems($value.SubProject))
        }
    }
    return $result
}

function GetFirstCsFile($projectItem)
{
    $value = $projectItem.Collection | ?{($_.Name -ne $null) -and ($_.Name.EndsWith(".cs"))} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value
    if ($value -is [Array])
    {
        return $value[0]
    }
    else
    {
        return $value
    }    
}





function WAQSServerMockInternal($edmxPath, $kind, $sourceControl, $netVersion, $option)
{
    if ($kind -eq $null)
    {
        throw "Kind cannot be null"
    }
    if (($netVersion -eq $null) -or (([array] $(GetAvailableVersions)) -notcontains $netVersion))
    {
        throw "This .NET version is not supported"
    }
    if (($edmxPath -eq $null) -and ($kind -ne "FrameworkOnly"))
    {
        throw "If kind is not FrameworkOnly, edmxPath cannot be null"
    }
    
    if ($netVersion -eq "NET46")
    {
        Write-Host "Note that .NET 4.6 new operators are not supported on specifications yet"
    }
    
    $projectUIHierarchyItems = (GetProjectsUIHierarchyItems | ?{$_.Object.FullName -eq (Get-Project).FullName}).UIHierarchyItems
    $referencesUIHierarchyItems = ($projectUIHierarchyItems | ?{$_.Name -eq 'References'}).UIHierarchyItems
    $referencesExpanded = $referencesUIHierarchyItems.Expanded

    $projectDirectoryPath = [System.IO.Path]::GetDirectoryName((Get-Project).FullName)
    if ($kind -eq "FrameworkOnly")
    {
        $waqsDirectory = Join-Path $projectDirectoryPath "WAQS.Framework"
    }
    else
    {
        $edmxName = [System.IO.Path]::GetFileNameWithoutExtension($edmxPath)
        if (-not [System.Text.RegularExpressions.Regex]::IsMatch($edmxName, "^\w[\w\d]*$"))
        {
          throw "Invalid edmx name"
        }
        $waqsDirectory = Join-Path $projectDirectoryPath ("WAQS" + "." + $edmxName)
    }

    Install-Package EntityFramework -Version 6.1.3
    
    try
    {
       $referencesUIHierarchyItems.Expanded = $referencesExpanded
    }
    catch
    {
    }

    if (($kind -eq "All") -or ($kind -eq "WithoutFramework") -or ($kind -eq "GlobalOnly"))
    {
        $waqsGeneralDirectory = Join-Path $projectDirectoryPath "WAQS"
    }
    
    if (Test-Path $waqsDirectory)
    {
        throw "$waqsDirectory already exists"
    }

    $toolsPath = GetToolsPath
    $toolsPathServerMock = Join-Path $toolsPath "Server.Mock"
    $defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value
    $exePath = Join-Path $toolsPathServerMock InitWAQSServerMock.exe
    $references = (Get-Project).Object.References
    $null = $references.Add("System")
    $entitiesProjectPath = $DTE.Solution.FindProjectItem(($edmxName + '.Server.Entities.tt')).ContainingProject.FullName
    if ($entitiesProjectPath.Length -gt 0)
    {
        $entitiesSolutionPath = $DTE.Solution.FileName
    }
    
    switch ($DTE.Version)
    {
        '10.0' {$VSVersion = "VS10"}
        '11.0' {$VSVersion = "VS11"}
        '12.0' {$VSVersion = "VS12"}
        '14.0' {$VSVersion = "VS14"}
    }
    $exeArgs = @('"' + $edmxPath + '"', '"' + $projectDirectoryPath + '"', '"' + $toolsPathServerMock + '"', '"' + $defaultNamespace + '"', '"' + $waqsDirectory + '"', '"' + $waqsGeneralDirectory + '"', '"' + $entitiesSolutionPath + '"', '"' + $entitiesProjectPath + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $kind + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"')
    if ($kind -eq "All" -or $kind -eq "WithoutFramework")
    {
       $projectItems = GetProjects | foreach {$_.ProjectItems}
       $specificationsProjectItem = $projectItems | ?{($_.Properties | ?{$_.Name -eq "FullPath"} | ?{$_.Value.EndsWith("\Specifications\")}) -ne $null} | select-object -first 1
       $exeArgs = $exeArgs + ('"' + ($specificationsProjectItem.ContainingProject.FullName) + '"')
       $exeArgs = $exeArgs + ('"' + [System.IO.Path]::GetDirectoryName(($specificationsProjectItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)) + '"')
       $dtoProjectItem = $projectItems | ?{($_.Properties | ?{$_.Name -eq "FullPath"} | ?{$_.Value.EndsWith("\DTO\")}) -ne $null} | select-object -first 1
       $exeArgs = $exeArgs + ('"' + ($dtoProjectItem.ContainingProject.FullName) + '"')
       $exeArgs = $exeArgs + ('"' + [System.IO.Path]::GetDirectoryName(($dtoProjectItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)) + '"')
       $entitiesItem = ($DTE.Solution.FindProjectItem(($edmxName + ".Server.Entities.tt")).ProjectItems | ?{$_.Name.EndsWith(".cs")})
       if ($entitiesItem.Length -ne $null)
       {
           $entitiesItem = $entitiesItem[0]
       }
       $exeArgs = $exeArgs + ('"' + ($entitiesItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
       $dalInterfacesItem = ($DTE.Solution.FindProjectItem(($edmxName + ".Server.DAL.Interfaces.tt")).ProjectItems | ?{$_.Name.EndsWith(".cs")})
       if ($dalInterfacesItem.Length -ne $null)
       {
           $dalInterfacesItem = $dalInterfacesItem[0]
       }
       $exeArgs = $exeArgs + ('"' + ($dalInterfacesItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
       $dalItem = ($DTE.Solution.FindProjectItem(($edmxName + ".Server.DAL.tt")).ProjectItems | ?{$_.Name.EndsWith(".cs")})
       if ($dalItem.Length -ne $null)
       {
           $dalItem = $dalItem[0]
       }
       $exeArgs = $exeArgs + ('"' + ($dalItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
       $edmx = $DTE.Solution.FindProjectItem($edmxPath)
       $exeArgs = $exeArgs + ('"' + ($edmx.ContainingProject.FullName) + '"')
       $configFileItem = $DTE.Solution.FindProjectItem($edmxPath).ContainingProject.ProjectItems | ?{($_.Name -eq "App.Config") -or ($_.Name -eq "Web.config")}
       if ($configFileItem.Length -ne $null)
       {
           $configFileItem = $configFileItem[0]
       }
       $exeArgs = $exeArgs + ('"' + ($configFileItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
    }
    if ($option -eq 'Debug')
    {
       Write-Host $exePath
       Write-Host $exeArgs
    }
    start-process -filepath $exePath -ArgumentList $exeArgs -Wait
    
    if ($sourceControl -eq 'WithSourceControl')
    {
       $slnFolder = [System.IO.Path]::GetDirectoryName($DTE.Solution.FullName)
       $serverMockTemplatesFolder = Join-Path $slnFolder "ServerMockTemplates"
       if (-not (Test-Path $serverMockTemplatesFolder))
       {
           [System.IO.Directory]::CreateDirectory($serverMockTemplatesFolder)
       }
       $solutionItems = $null
       foreach($p in $DTE.Solution.Projects | ?{$_.ProjectName -eq 'Solution Items'})
       {
           $solutionItems = $p
           $solutionItemsUIHierarchyItems = (($DTE.Windows | ?{$_.Type -eq 'vsWindowTypeSolutionExplorer'}).Object.UIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'Solution Items'})
           $solutionItemsExpanded = $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded
       }
       while ($solutionItems -eq $null)
       {
           try
           {
               $solutionItems = $DTE.Solution.AddSolutionFolder('Solution Items')
               $solutionItemsUIHierarchyItems = (($DTE.Windows | ?{$_.Type -eq 'vsWindowTypeSolutionExplorer'}).Object.UIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'Solution Items'})
               $solutionItemsExpanded = $false
           }
           catch # a strange bug can append: Method invocation failed because [System.__ComObject] does not contain a method named 'AddSolutionFolder'.
           {
               if ($option -eq 'Debug')
               {
                   Write-Host "Catch Solution Items"
               }
               foreach($p in $DTE.Solution.Projects | ?{$_.ProjectName -eq 'Solution Items'})
               {
                   $solutionItems = $p
               }
           }
       }
       $serverMockTemplates = $null
       foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerMockTemplates'})
       {
           $serverMockTemplates = $p.SubProject
           $serverMockTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'ServerMockTemplates'}
           $serverMockTemplatesExpanded = $serverMockTemplatesUIHierarchyItems.UIHierarchyItems.Expanded
       }
       while ($serverMockTemplates -eq $null)
       {
           try
           {
               $serverMockTemplates = $solutionItems.Object.AddSolutionFolder('ServerMockTemplates')
               $serverMockTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'ServerMockTemplates'}
               $serverTemplatesExpanded = $false
           }
           catch # a strange bug can append: Method invocation failed because [System.__ComObject] does not contain a method named 'AddSolutionFolder'.
           {
               if ($option -eq 'Debug')
               {
                   Write-Host "Catch ServerMockTemplates"
               }
               foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerMockTemplates'})
               {
                   $serverMockTemplates = $p.SubProject
               }
           }
       }
       
       $ttincludesFolder = Join-Path $toolsPath 'ttincludes'
       $serverMockTemplatesProjectItems = $serverMockTemplates.ProjectItems
       $existingServerMockTTIncludes = $serverMockTemplatesProjectItems | select -ExpandProperty Name
       foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolder) | ?{[System.IO.Path]::GetFileName($_).StartsWith("WAQS.")})
       {
           $m = [System.Text.RegularExpressions.Regex]::Match($ttinclude, '.(NET\d+).')
           if ((-not ($m.Success)) -or ($m.Groups[1].Value -eq $netVersion))
           {
               $ttincludeName = [System.IO.Path]::GetFileName($ttinclude)
               $ttIncludeCopy = Join-Path $serverMockTemplatesFolder $ttincludeName
               if (($existingServerMockTTIncludes -eq $null) -or (-not ($existingServerMockTTIncludes.Contains($ttincludeName))))
               {
                   $null = $serverMockTemplatesProjectItems.AddFromFile($ttIncludeCopy)
               }
           }
       }
       switch ($DTE.Version)
       {
            '11.0' {$vsVersion = 'VS11'}
            '12.0' {$vsVersion = 'VS12'}
            '14.0' {$VSVersion = 'VS14'}
       }
       $ttincludesFolderVS = Join-Path $ttincludesFolder $vsVersion
       foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolderVS))
       {
           $ttincludeName = [System.IO.Path]::GetFileName($ttinclude)
           if ([System.IO.Path]::GetFileName($ttincludeName).StartsWith("WAQS."))
           {
               $m = [System.Text.RegularExpressions.Regex]::Match($ttinclude, '.(NET\d+).')
               if ((-not ($m.Success)) -or ($m.Groups[1].Value -eq $netVersion))
               {
                   $ttIncludeCopy = Join-Path $serverMockTemplatesFolder $ttincludeName
                   if (($existingServerMockTTIncludes -eq $null) -or (-not ($existingServerMockTTIncludes.Contains($ttincludeName))))
                   {
                       $null = $serverMockTemplatesProjectItems.AddFromFile($ttIncludeCopy)
                   }
                   if ($ttinclude.Contains(('.' + $vsVersion + '.' + $netVersion + '.')))
                   {
                       $ttIncludeCopy = $ttIncludeCopy.Substring(0, $ttIncludeCopy.Length - 10) + '.merge.tt'
                       $ttincludeName = [System.IO.Path]::GetFileName($ttIncludeCopy)
                       if (($existingServerMockTTIncludes -eq $null) -or (-not ($existingServerMockTTIncludes.Contains($ttincludeName))))
                       {
                           $null = $serverMockTemplatesProjectItems.AddFromFile($ttIncludeCopy)
                       }
                   }
               }
           }
       }
       $specialMergeFolder = Join-Path $ttincludesFolder 'SpecialMerge'
       foreach ($specialMerge in [System.IO.Directory]::GetFiles($specialMergeFolder))
       {
           $ttSpecialMergeFileName = [System.IO.Path]::GetFileName($specialMerge)
           $specialMergeFile = Join-Path $specialMergeFolder $ttSpecialMergeFileName
           $ttSpecialMergeFileCopy = Join-Path $serverMockTemplatesFolder $ttSpecialMergeFileName
           if (-not ([System.IO.File]::Exists($ttSpecialMergeFileName)))
           {
               copy $specialMergeFile $ttSpecialMergeFileCopy
               if (($existingServerMockTTIncludes -eq $null) -or (-not ($existingServerMockTTIncludes.Contains($ttSpecialMergeFileName))))
               {
                   $null = $serverMockTemplatesProjectItems.AddFromFile($ttSpecialMergeFileCopy)
               }
           }
       }
       try
       {
           $serverMockTemplatesUIHierarchyItems.UIHierarchyItems.Expanded = $serverMockTemplatesExpanded
           $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded = $solutionItemsExpanded
       }
       catch
       {
       }
       MergeServerMockTTIncludes
    }

    if ($kind -eq "FrameworkOnly")
    {
        $edmxName = "Framework"
    }
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Server.Mock.waqs"))) 
    if ($kind -ne "GlobalOnly")
    {
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Server.Mock.tt"))) 
    }
    if ($withGlobal)
    {
        $null = (Get-Project).ProjectItems.AddFromFile($appConfigPath) 
        $null = (Get-Project).ProjectItems.AddFromFile($expressionTransformerPath)
    }
    if ($isOnTfs)
    {
        $DTE.ExecuteCommand("File.TfsRefreshStatus")
    }
    try
    {
        ($projectUIHierarchyItems | ? {$_.Name -eq ('WAQS.' + $edmxName)})[0].UIHierarchyItems.Expanded = $false
    }
    catch
    {
    }
}

function WAQSServerMock($edmxPath, $kind, $sourceControl, $netVersion, $option)
{
    $version = ((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value
    if (-not $version.StartsWith('.NETFramework,'))
    {
        throw "This project is not a .NET project ($version)"
    }

    if ($netVersion -eq $null)
    {
        $netVersion = "NET" + ([System.Text.RegularExpressions.Regex]::Match((((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value), "Version=v(\d+.\d+)").Groups[1].Value.Replace(".", ""))
    }
    if ($kind -eq $null -and ((Get-Project).Properties.Item("OutputType").Value -eq [VSLangProj.prjOutputType]::prjOutputTypeWinExe))
    {
        $kind = "All"
    }
    if ($sourceControl -eq $null)
    {
       $sourceControl = "WithoutSourceControl"
    }
    
    $edmxPath = [System.Text.RegularExpressions.Regex]::Match($edmxPath, '^\"?(.*?)\"?$').Groups[1].Value
    WAQSServerMockInternal $edmxPath $kind $sourceControl $netVersion $option
}

Register-TabExpansion 'WAQSServerMock' @{ 
'edmxPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".edmx")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
'kind' = { "All", "WithoutFramework", "FrameworkOnly" }
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions }
}

Export-ModuleMember WAQSServerMock


function UpdateWAQSServerMockT4Templates()
{
    foreach ($file in GetProjects | foreach {(GetAllT4RootItems $_)} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | ?{$_.EndsWith(".Server.Mock.tt")})
    {
        RecursiveGeneration $file
    }
}


Export-ModuleMember UpdateWAQSServerMockT4Templates


function MergeServerMockTTIncludes()
{
    $solutionItems = $null
    foreach ($p in $DTE.Solution.Projects | ?{$_.Name -eq 'Solution Items'}) 
    { 
        $solutionItems = $p 
    }
    if ($solutionItems -ne $null)
    {
        $serverMockTemplates = $null
        foreach ($pi in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerMockTemplates'})
        {
            $serverMockTemplates = $pi
        }
        if ($serverMockTemplates -ne $null)
        {
            $ttFolderPath = Join-Path ([System.IO.Path]::GetDirectoryName($DTE.Solution.FullName)) 'ServerMockTemplates'
            $path = "HKCU:\Software\Microsoft\VisualStudio\" + $DTE.Version + "_Config"
            $ttIncludePath = (Get-Item (Join-Path $path "TextTemplating\IncludeFolders\.tt")).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE")
            if ($ttIncludePath.EndsWith('\'))
            {
                $ttIncludePath = $ttIncludePath.SubString(0, $ttIncludePath.Length - 1)
            }
            if ((Get-PSDrive | ?{$_.Name -eq 'HKCR'}) -eq $null)
            {
                $null = New-PSDrive -Name HKCR -PSProvider Registry -Root HKEY_CLASSES_ROOT
            }
            $defaultIconKey = 'HKCR:\VisualStudio.TextTemplating.' + $DTE.Version + '\DefaultIcon'
            $transformTemplatesExePath = Join-Path ([System.IO.Path]::GetDirectoryName((Get-Item $defaultIconKey).GetValue($null))) "TextTransform.exe"
            foreach ($tt in $serverMockTemplates.SubProject.ProjectItems | ?{$_.Name.EndsWith('.merge.tt')})
            {
                $transformTemplatesArgs = ('"' + (Join-Path $ttFolderPath $tt.Name) + '"', '-I "' + $ttIncludePath + '"')
                start-process -filepath $transformTemplatesExePath -ArgumentList $transformTemplatesArgs -WindowStyle Hidden -Wait
            }
        }
    }
}

Export-ModuleMember MergeServerTTIncludes