function GetToolsPath()
{
    $modules = (Get-Module WCFAsyncQueryableFunctionsServer | select -property path)
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





function WAQSServerInternal($edmxPath, $kind, $appKind, $netVersion, $sourceControl, $option)
{
    if ($kind -eq $null)
    {
        throw "Kind cannot be null"
    }
    if ($appKind -eq $null)
    {
       throw "AppKind cannot be null"
    }
    if (($netVersion -eq $null) -or (([array] $(GetAvailableVersions)) -notcontains $netVersion))
    {
        throw ".NET $netVersion is not supported"
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
    
    if (Test-Path $waqsDirectory)
    {
        throw "$waqsDirectory already exists"
    }
    
    if ($kind -ne "FrameworkOnly")
    {
        $edmx = $DTE.Solution.FindProjectItem($edmxPath)
        $edmxProjectPath = $edmx.ContainingProject.FullName
    }
    $toolsPath = GetToolsPath
    $toolsPathServer = Join-Path $toolsPath "Server"
    $defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value
    if (($kind -eq "GlobalOnly") -or ($kind -eq "WithoutFramework"))
    {
        $fxProject = $DTE.Solution.FindProjectItem("WCFExceptionHandlerEndpointBehavior.cs").ContainingProject
    }
    else
    {
        $fxProject = Get-Project
    }
    $assemblyName = ($fxProject.Properties | ?{$_.Name -eq 'AssemblyName'}).Value
    $assemblyVersion = ($fxProject.Properties | ?{$_.Name -eq 'AssemblyVersion'}).Value
    $exePath = Join-Path $toolsPathServer InitWAQSServer.exe
    $references = (Get-Project).Object.References
    $null = $references.Add("System")
    $null = $references.Add("System.Configuration")
    $null = $references.Add("System.Core")
    $null = $references.Add("System.Data")
    $null = $references.Add("System.Runtime.Serialization")
    $null = $references.Add("System.ServiceModel")
    $null = $references.Add("System.ServiceModel.Activation")
    $null = $references.Add("System.ServiceModel.Channels")
    $null = $references.Add("System.Transactions")
    $null = $references.Add("System.Web")
    $null = $references.Add("System.Xml")
    if ($netVersion -eq 'NET40')
    {
        Install-Package Unity -Version 2.1.505.2
    }
    else
    {
        Install-Package Unity -Version 3.0.1304.1
        Install-Package CommonServiceLocator -Version 1.2.0
    }
    try
    {
        Install-Package EntityFramework -Version 6.1.3
    }
    catch
    {
    }
    
    try
    {
       $referencesUIHierarchyItems.Expanded = $referencesExpanded
    }
    catch
    {
    }
    
    $withGlobal = ($kind -eq "All") -or ($kind -eq "WithoutFramework") -or ($kind -eq "GlobalOnly")
    $globalDirectory = Join-Path $projectDirectoryPath "Global"
    if ($withGlobal)
    {
        $webConfigPath = Join-Path $projectDirectoryPath "Web.config"
        $globalAsaxPath = Join-Path $projectDirectoryPath "Global.asax"
        $globalAsaxCs = Join-Path $projectDirectoryPath "Global.asax.cs"
        $globalWCFService = Join-Path $globalDirectory "GlobalWCFService.cs"
        try
        {
            if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
            {
                $isOnTfs = (add-TfsPendingChange -edit $DTE.Solution.FullName) -ne $null
                if (Test-Path $webConfigPath)
                {
                    $isOnTfs = (add-TfsPendingChange -edit $webConfigPath) -ne $null
                }
                if (($isOnTfs -ne $false) -and (Test-Path $globalAsaxPath))
                {
                    $null = add-TfsPendingChange -edit $globalAsaxPath
                }
                if (($isOnTfs -ne $false) -and (Test-Path $globalAsaxCs))
                {
                    $null = add-TfsPendingChange -edit $globalAsaxCs
                }
                if (($kind -eq "GlobalOnly") -and ($isOnTfs -ne $false) -and (Test-Path $globalWCFService))
                {
                    $null = add-TfsPendingChange -edit $globalWCFService
                }
            }
        }
        catch
        {
        }
    }
    switch ($DTE.Version)
    {
        '10.0' {$VSVersion = "VS10"}
        '11.0' {$VSVersion = "VS11"}
        '12.0' {$VSVersion = "VS12"}
        '14.0' {$VSVersion = "VS14"}
    }
    $exeArgs = @('"' + $edmxPath + '"', '"' + $edmxProjectPath + '"', '"' + $projectDirectoryPath + '"', '"' + $toolsPathServer + '"', '"' + $defaultNamespace + '"', '"' + $assemblyName + '"', '"' + $assemblyVersion + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $kind + '"', '"' + $appKind + '"', '"' + $waqsDirectory + '"', '"' + ($DTE.Solution.FindProjectItem($edmxPath).ContainingProject.ProjectItems | ?{$_.Name -eq "App.Config"} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"', 'WCF')
    if ($kind -eq "GlobalOnly")
    {
        $exeArgs = $exeArgs + ('"' + (GetFirstCsFile($DTE.Solution.FindProjectItem($edmxName + ".Server.DAL.Interfaces.tt"))) + '"')
        $exeArgs = $exeArgs + ('"' + (GetFirstCsFile($DTE.Solution.FindProjectItem($edmxName + ".Server.DAL.tt"))) + '"')
        $exeArgs = $exeArgs + ('"' + ($DTE.Solution.FindProjectItem("I" + $edmxName + "Service.cs").Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
        $exeArgs = $exeArgs + ('"' + ($DTE.Solution.FindProjectItem($edmxName + "Service.cs").Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
        $exeArgs = $exeArgs + ('"' + ($DTE.Solution.FindProjectItem("I" + $edmxName + "WCFService.cs").Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
        $exeArgs = $exeArgs + ('"' + ($DTE.Solution.FindProjectItem($edmxName + "WCFService.cs").Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
         $edmxProjectPath = ($DTE.Solution.FindProjectItem($edmxPath).ContainingProject).FullName
        $exeArgs = $exeArgs + ('"' + $edmxProjectPath + '"')
        $edmxProjectFolderPath = [System.IO.Path]::GetDirectoryName($edmxProjectPath)
        $exeArgs = $exeArgs + ('"' + (Join-Path $edmxProjectFolderPath "Specifications") + '"')
        $exeArgs = $exeArgs + ('"' + (Join-Path $edmxProjectFolderPath "DTO") + '"')
    }
    else 
    {
        if ($kind -ne "FrameworkOnly")
        {
            $exeArgs = $exeArgs + ('"' + ((Get-Project).FullName) + '"')
            $specificationsFolder = Join-Path $projectDirectoryPath "Specifications"
            $exeArgs = $exeArgs + ('"' + $specificationsFolder + '"')
            $dtoFolder = Join-Path $projectDirectoryPath "DTO"
            $exeArgs = $exeArgs + ('"' + $dtoFolder + '"')
        }
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
       $serverTemplatesFolder = Join-Path $slnFolder "ServerTemplates"
       if (-not (Test-Path $serverTemplatesFolder))
       {
           [System.IO.Directory]::CreateDirectory($serverTemplatesFolder)
       }
       $solutionItems = $null
       foreach($p in $DTE.Solution.Projects | ?{$_.ProjectName -eq 'Solution Items'})
       {
           $solutionItems = $p
           $solutionItemsUIHierarchyItems = (($DTE.Windows | ?{$_.Type -eq 'vsWindowTypeSolutionExplorer'}).Object.UIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'Solution Items'})
           $solutionItemsExpanded = $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded
       }
       $indexTry = 0
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
               if ($indexTry -eq 3)
               {
                   throw 'Add Solution Items folder failed'
               }
               $indexTry = $indexTry + 1
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
       $serverTemplates = $null
       foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerTemplates'})
       {
           $serverTemplates = $p.SubProject
           $serverTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'ServerTemplates'}
           $serverTemplatesExpanded = $serverTemplatesUIHierarchyItems.UIHierarchyItems.Expanded
       }
       $indexTry = 0
       while ($serverTemplates -eq $null)
       {
           try
           {
               $serverTemplates = $solutionItems.Object.AddSolutionFolder('ServerTemplates')
               $serverTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'ServerTemplates'}
               $serverTemplatesExpanded = $false
           }
           catch # a strange bug can append: Method invocation failed because [System.__ComObject] does not contain a method named 'AddSolutionFolder'.
           {
               if ($indexTry -eq 3)
               {
                   throw 'Add ServerTemplates folder failed'
               }
               $indexTry = $indexTry + 1
               if ($option -eq 'Debug')
               {
                   Write-Host "Catch ServerTemplates"
               }
               foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerTemplates'})
               {
                   $serverTemplates = $p.SubProject
               }
           }
       }
       
       $ttincludesFolder = Join-Path $toolsPath 'ttincludes'
       $serverTemplatesProjectItems = $serverTemplates.ProjectItems
       $existingServerTTIncludes = $serverTemplatesProjectItems | select -ExpandProperty Name
       foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolder) | ?{[System.IO.Path]::GetFileName($_).StartsWith("WAQS.")})
       {
           $m = [System.Text.RegularExpressions.Regex]::Match($ttinclude, '.(NET\d+).')
           if ((-not ($m.Success)) -or ($m.Groups[1].Value -eq $netVersion))
           {
               $ttincludeName = [System.IO.Path]::GetFileName($ttinclude)
               $ttIncludeCopy = Join-Path $serverTemplatesFolder $ttincludeName
               if (($existingServerTTIncludes -eq $null) -or (-not ($existingServerTTIncludes.Contains($ttincludeName))))
               {
                   $null = $serverTemplatesProjectItems.AddFromFile($ttIncludeCopy)
               }
           }
       }

       switch ($DTE.Version)
       {
            '11.0' {$vsVersion = 'VS11'}
            '12.0' {$vsVersion = 'VS12'}
            '14.0' {$vsVersion = 'VS14'}
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
                   $ttIncludeCopy = Join-Path $serverTemplatesFolder $ttincludeName
                   if (($existingServerTTIncludes -eq $null) -or (-not ($existingServerTTIncludes.Contains($ttincludeName))))
                   {
                       $null = $serverTemplatesProjectItems.AddFromFile($ttIncludeCopy)
                   }
                   if ($ttinclude.Contains(('.' + $vsVersion + '.' + $netVersion + '.')))
                   {
                       $ttIncludeCopy = $ttIncludeCopy.Substring(0, $ttIncludeCopy.Length - 10) + '.merge.tt'
                       $ttincludeName = [System.IO.Path]::GetFileName($ttIncludeCopy)
                       if (($existingServerTTIncludes -eq $null) -or (-not ($existingServerTTIncludes.Contains($ttincludeName))))
                       {
                           $null = $serverTemplatesProjectItems.AddFromFile($ttIncludeCopy)
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
           $ttSpecialMergeFileCopy = Join-Path $serverTemplatesFolder $ttSpecialMergeFileName
           if (-not ([System.IO.File]::Exists($ttSpecialMergeFileName)))
           {
               copy $specialMergeFile $ttSpecialMergeFileCopy
               if (($existingServerTTIncludes -eq $null) -or (-not ($existingServerTTIncludes.Contains($ttSpecialMergeFileName))))
               {
                   $null = $serverTemplatesProjectItems.AddFromFile($ttSpecialMergeFileCopy)
               }
           }
       }

       try
       {
           $serverTemplatesUIHierarchyItems.UIHierarchyItems.Expanded = $serverTemplatesExpanded
           $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded = $solutionItemsExpanded
       }
       catch
       {
       }
       MergeServerTTIncludes
    }

    if ($kind -ne "FrameworkOnly" -and $kind -ne "GlobalOnly")
    {
        $edmxFileProperties = $edmx.Properties
        $edmxFileProperties.Item("CustomTool").Value = ""
        $edmxFileProperties.Item("BuildAction").Value = 0
        foreach ($ttPath in $edmx.ProjectItems | ?{$_.Name.EndsWith('.tt')} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)
        {
            $DTE.Solution.FindProjectItem($ttPath).Delete()
        }
    }
    
    if ($kind -eq "FrameworkOnly")
    {
        $edmxName = "Framework"
    }
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Server.waqs"))) 
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Server.tt"))) 
    $dalItem = $DTE.Solution.FindProjectItem((Join-Path $waqsDirectory ($edmxName + '.Server.DAL.tt')))
    if (($dalItem -ne $null) -and ($dalItem.ProjectItems.Count -lt 2)) # strange bug: sometimes code is not generated for this T4 only
    {
        $dalItem.Object.RunCustomTool()
    }
    try
    {
        ($projectUIHierarchyItems | ? {$_.Name -eq ('WAQS.' + $edmxName)})[0].UIHierarchyItems.Expanded = $false
    }
    catch
    {
    }
    if ($withGlobal -and $appKind -eq 'Web')
    {
        $null = (Get-Project).ProjectItems.AddFromFile($webConfigPath) 
        $null = (Get-Project).ProjectItems.AddFromFile($globalAsaxPath)
        $null = (Get-Project).ProjectItems.AddFromFile($globalAsaxCs)
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $projectDirectoryPath ($edmxName + ".svc"))) 
    }
    if ($kind -eq "GlobalOnly" -and $appKind -eq 'Web')
    {
        $globalUIHierarchyItems = $projectUIHierarchyItems | ? {$_.Name -eq ('Global')}
        if ($globalUIHierarchyItems -ne $null)
        {
           $globalUIHierarchyItems = $globalUIHierarchyItems[0].UIHierarchyItems
           $globalUIHierarchyItemsExpanded = $globalUIHierarchyItems.Expanded
        }
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $globalDirectory "GlobalWCFServiceContract.tt")) 
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $globalDirectory "GlobalWCFService.cs")) 
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $projectDirectoryPath "Global.svc")) 
        if ($globalUIHierarchyItems -eq $null)
        {
           try
           {
               ($projectUIHierarchyItems | ? {$_.Name -eq ('Global')})[0].UIHierarchyItems.Expanded = $false
           }
           catch
           {
           }
        }
        else
        {
           try
           {
               $globalUIHierarchyItems.Expanded = $globalUIHierarchyItemsExpanded
           }
           catch
           {
           }
        }
    }
    if ($specificationsFolder -ne $null)
    {
       $null = (Get-Project).ProjectItems.AddFromDirectory($specificationsFolder)
    }
    if ($dtoFolder -ne $null)
    {
       $null = (Get-Project).ProjectItems.AddFromDirectory($dtoFolder)
    }
    if ($isOnTfs)
    {
        $DTE.ExecuteCommand("File.TfsRefreshStatus")
    }
}

function WAQSServer($edmxPath, $kind, $appKind, $sourceControl, $netVersion, $option)
{
    if ($netVersion -eq $null)
    {
        $netVersion = GetVersion
    }
    if ($kind -eq $null)
    {
        $kind = "All"
    }
    if ($appKind -eq $null)
    {
       $appKind = "Web"
    }
    if ($sourceControl -eq $null)
    {
       $sourceControl = "WithoutSourceControl"
    }
    $edmxPath = [System.Text.RegularExpressions.Regex]::Match($edmxPath, '^\"?(.*?)\"?$').Groups[1].Value
    WAQSServerInternal $edmxPath $kind $appKind $netVersion $sourceControl $option
}

Register-TabExpansion 'WAQSServer' @{ 
'edmxPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".edmx")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
'kind' = { "All", "WithoutGlobal", "WithoutFramework", "WithoutGlobalWithoutFramework", "FrameworkOnly", "GlobalOnly" }
'appKind' = { "Web", "App" }
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions }
}

Export-ModuleMember WAQSServer

function UpdateWAQSServerT4Templates()
{
    $projectsT4RootItems = GetProjects | foreach {(GetAllT4RootItems $_)}
    foreach ($file in $projectsT4RootItems | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | ?{$_.EndsWith(".Server.tt")})
    {
        RecursiveGeneration $file
    }
    foreach ($file in $projectsT4RootItems | ?{$_.Name -eq 'GlobalWCFServiceContract.tt'} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)
    {
        RecursiveGeneration $file
    }
}

Export-ModuleMember UpdateWAQSServerT4Templates


function MergeServerTTIncludes()
{
    $solutionItems = $null
    foreach ($p in $DTE.Solution.Projects | ?{$_.Name -eq 'Solution Items'}) 
    { 
        $solutionItems = $p 
    }
    if ($solutionItems -ne $null)
    {
        $serverTemplates = $null
        foreach ($pi in $solutionItems.ProjectItems | ?{$_.Name -eq 'ServerTemplates'})
        {
            $serverTemplates = $pi
        }
        if ($serverTemplates -ne $null)
        {
            $ttFolderPath = Join-Path ([System.IO.Path]::GetDirectoryName($DTE.Solution.FullName)) 'ServerTemplates'
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
            foreach ($tt in $serverTemplates.SubProject.ProjectItems | ?{$_.Name.EndsWith('.merge.tt')})
            {
                $transformTemplatesArgs = ('"' + (Join-Path $ttFolderPath $tt.Name) + '"', '-I "' + $ttIncludePath + '"')
                start-process -filepath $transformTemplatesExePath -ArgumentList $transformTemplatesArgs -WindowStyle Hidden -Wait
            }
        }
    }
}

Export-ModuleMember MergeServerTTIncludes