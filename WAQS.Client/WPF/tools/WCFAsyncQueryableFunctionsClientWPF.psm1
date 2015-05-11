function GetToolsPath()
{
    $modules = (Get-Module WCFAsyncQueryableFunctionsClientWPF | select -property path)
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





function WAQSClientWPFInternal($edmxPath, $svcUrl, $kind, $sourceControl, $netVersion, $option)
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
    if (($svcUrl -eq $null) -and ($kind -ne "FrameworkOnly"))
    {
        throw "If kind is not FrameworkOnly, svcUrl cannot be null"
    }
    
    if ($netVersion -eq "NET46")
    {
        Write-Host "Note that .NET 4.6 new operators are not supported on specifications yet"
    }

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
    if ($kind -eq "GlobalOnly")
    {
        $waqsGeneralDirectory = Join-Path $projectDirectoryPath "WAQS"
        $contextsPath = Join-Path $waqsGeneralDirectory "Contexts.xml"
    }
    
    if (Test-Path $waqsDirectory)
    {
        throw "$waqsDirectory already exists"
    }
    
    $projectUIHierarchyItems = (GetProjectsUIHierarchyItems | ?{$_.Object.FullName -eq (Get-Project).FullName}).UIHierarchyItems
    $referencesUIHierarchyItems = ($projectUIHierarchyItems | ?{$_.Name -eq 'References'}).UIHierarchyItems
    $referencesExpanded = $referencesUIHierarchyItems.Expanded

    $toolsPath = GetToolsPath
    $wpfToolsPath = Join-Path $toolsPath "Client.WPF"
    $defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value
    $exePath = Join-Path $wpfToolsPath InitWAQSClientWPF.exe
    $references = (Get-Project).Object.References
    $null = $references.Add("System")
    $null = $references.Add("System.ComponentModel.DataAnnotations")
    $null = $references.Add("System.Core")
    $null = $references.Add("System.Drawing")
    $null = $references.Add("PresentationCore")
    $null = $references.Add("PresentationFramework")
    $null = $references.Add("System.Runtime.Serialization")
    $null = $references.Add("System.ServiceModel")
    $null = $references.Add("System.Xaml")
    $null = $references.Add("System.Xml")
    $ref = (Get-Project).Object.References | ?{$_.Name -eq 'System.Windows.Interactivity'}
    if ($ref -ne $null)
    {
        $ref.Remove()
    }
    $null = $references.Add((Join-Path $wpfToolsPath "System.Windows.Interactivity.dll"))
    $ref = (Get-Project).Object.References | ?{$_.Name -eq 'Microsoft.Expression.Interactions'}
    if ($ref -ne $null)
    {
        $ref.Remove()
    }
    $null = $references.Add((Join-Path $wpfToolsPath "Microsoft.Expression.Interactions.dll"))
    if ($netVersion -eq "NET40")
    {
        switch ($DTE.Version)
        {
            '10.0' {Install-Package AsyncCTP}
            '11.0' {Install-package Microsoft.CompilerServices.AsyncTargetingPack}
            '12.0' {Install-package Microsoft.CompilerServices.AsyncTargetingPack}
        }
    }
    if ($netVersion -eq 'NET40')
    {
        Install-Package Unity -Version 2.1.505.2       
    }
    else
    {
        Install-Package Unity -Version 3.0.1304.1
    }
    Install-Package Rx-WPF -Version 1.0.11226 
    
    try
    {
       $referencesUIHierarchyItems.Expanded = $referencesExpanded
    }
    catch
    {
    }

    $withGlobal = ($kind -eq "All") -or ($kind -eq "WithoutFramework") -or ($kind -eq "GlobalOnly")
    if ($withGlobal)
    {
        $appConfigPath = Join-Path $projectDirectoryPath "app.config"
        try
        {
            if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
            {
                $appXamlPath = Join-Path $projectDirectoryPath "App.xaml"
                if (Test-Path $appXamlPath)
                {
                    $isOnTfs = (add-TfsPendingChange -edit $appXamlPath) -ne $null
                }
                $appXamlCsPath = Join-Path $projectDirectoryPath "App.xaml.cs"
                if (($isOnTfs -ne $false) -and (Test-Path $appXamlCsPath))
                {
                    $null = add-TfsPendingChange -edit $appXamlCsPath
                }
                if (($isOnTfs -ne $false) -and (Test-Path $appConfigPath))
                {
                    $null = add-TfsPendingChange -edit $appConfigPath
                }
                if (($isOnTfs -ne $false) -and (Test-Path $contextsPath))
                {
                    $null = add-TfsPendingChange -edit $contextsPath
                }
            }
        }
        catch
        {
        }
    }
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
    $exeArgs = @('"' + $edmxPath + '"', '"' + $projectDirectoryPath + '"', '"' + $wpfToolsPath + '"', '"' + $defaultNamespace + '"', '"' + $svcUrl +'"', '"' + $waqsDirectory + '"', '"' + $waqsGeneralDirectory + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.WPF.ClientContext.tt")).ProjectItems | ?{$_.Name -eq ($edmxName + "ExpressionTransformer.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + (($DTE.Solution.FindProjectItem(($edmxName + ".Client.WPF.ServiceProxy.tt")).ProjectItems | ?{$_.Name -eq ("I" + $edmxName + "Service.cs")}).Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + (GetFirstCsFile($DTE.Solution.FindProjectItem(($edmxName + ".Client.WPF.Entities.tt")))) + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.WPF.ClientContext.tt")).ProjectItems | ?{$_.Name -eq ($edmxName + "ClientContext.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.WPF.ClientContext.Interfaces.tt")).ProjectItems | ?{$_.Name -eq ("I" + $edmxName + "ClientContext.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + $entitiesSolutionPath + '"', '"' + $entitiesProjectPath + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $kind + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"')
    if ($kind -eq "All" -or $kind -eq "WithoutFramework" -or $kind -eq "WithoutGlobal" -or $kind -eq "WithoutGlobalWithoutFramework")
    {
       $projectsItems = GetProjects | foreach {(GetAllProjectItems $_)}
       $specificationsProjectItem = $projectsItems | ?{($_.Properties | ?{$_.Name -eq "FullPath"} | ?{$_.Value.EndsWith("\Specifications\")}) -ne $null} | select-object -first 1
       $exeArgs = $exeArgs + ('"' + ($specificationsProjectItem.ContainingProject.FullName) + '"')
       $exeArgs = $exeArgs + ('"' + [System.IO.Path]::GetDirectoryName(($specificationsProjectItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)) + '"')
       $dtoProjectItem = $projectsItems | ?{($_.Properties | ?{$_.Name -eq "FullPath"} | ?{$_.Value.EndsWith("\DTO\")}) -ne $null} | select-object -first 1
       $exeArgs = $exeArgs + ('"' + ($dtoProjectItem.ContainingProject.FullName) + '"')
       $exeArgs = $exeArgs + ('"' + [System.IO.Path]::GetDirectoryName(($dtoProjectItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)) + '"')
       $entitiesItem = ($DTE.Solution.FindProjectItem(($edmxName + ".Server.Entities.tt")).ProjectItems | ?{$_.Name.EndsWith(".cs")})
       if ($entitiesItem.Length -ne $null)
       {
           $entitiesItem = $entitiesItem[0]
       }
       $exeArgs = $exeArgs + ('"' + ($entitiesItem.Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"')
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
       $clientWPFTemplatesFolder = Join-Path $slnFolder "WPFClientTemplates"
       if (-not (Test-Path $clientWPFTemplatesFolder))
       {
           [System.IO.Directory]::CreateDirectory($clientWPFTemplatesFolder)
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
       $wpfClientTemplates = $null
       foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'WPFClientTemplates'})
       {
           $wpfClientTemplates = $p.SubProject
           $wpfClientTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'WPFClientTemplates'}
           $wpfClientTemplatesExpanded = $wpfClientTemplatesUIHierarchyItems.UIHierarchyItems.Expanded
       }
       while ($wpfClientTemplates -eq $null)
       {
           try
           {
               $wpfClientTemplates = $solutionItems.Object.AddSolutionFolder('WPFClientTemplates')
               $wpfClientTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'WPFClientTemplates'}
               $wpfClientTemplatesExpanded = $false
           }
           catch # a strange bug can append: Method invocation failed because [System.__ComObject] does not contain a method named 'AddSolutionFolder'.
           {
               if ($option -eq 'Debug')
               {
                   Write-Host "Catch WPFClientTemplates"
               }
               foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'WPFClientTemplates'})
               {
                   $wpfClientTemplates = $p.SubProject
               }
           }
       }
       
       $ttincludesFolder = Join-Path $toolsPath 'ttincludes'
       $wpfClientTemplatesProjectItems = $wpfClientTemplates.ProjectItems
       $existingWPFClientTTIncludes = $wpfClientTemplatesProjectItems | select -ExpandProperty Name
       foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolder) | ?{[System.IO.Path]::GetFileName($_).StartsWith("WAQS.")})
       {
           $m = [System.Text.RegularExpressions.Regex]::Match($ttinclude, '.(NET\d+).')
           if ((-not ($m.Success)) -or ($m.Groups[1].Value -eq $netVersion))
           {
               $ttincludeName = [System.IO.Path]::GetFileName($ttinclude)
               if ($ttincludeName.EndsWith('.ttinclude.x64'))
               {
                   if (Test-Path "HKLM:\Software\Wow6432Node\Microsoft\Microsoft SDKs\Windows\")
                   {
                       $ttincludeName = $ttincludeName.Substring(0, $ttincludeName.Length - 4)
                   }
                   else
                   {
                       $ttincludeName = $null
                   }
               }
               else
               {
                   if ($ttincludeName.EndsWith('.ttinclude.x86'))
                   {
                       if (Test-Path "HKLM:\Software\Wow6432Node\Microsoft\Microsoft SDKs\Windows\")
                       {
                           $ttincludeName = $null
                       }
                       else
                       {
                           $ttincludeName = $ttincludeName.Substring(0, $ttincludeName.Length - 4)
                       }
                   }
               }
               if ($ttincludeName -ne $null)
               {
                   $ttIncludeCopy = Join-Path $clientWPFTemplatesFolder $ttincludeName
                   if (($existingWPFClientTTIncludes -eq $null) -or (-not ($existingWPFClientTTIncludes.Contains($ttincludeName))))
                   {
                       $null = $wpfClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
                   }
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
                   $ttIncludeCopy = Join-Path $clientWPFTemplatesFolder $ttincludeName
                   if (($existingWPFClientTTIncludes -eq $null) -or (-not ($existingWPFClientTTIncludes.Contains($ttincludeName))))
                   {
                       $null = $wpfClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
                   }
                   if ($ttinclude.Contains(('.' + $vsVersion + '.' + $netVersion + '.')))
                   {
                       $ttIncludeCopy = $ttIncludeCopy.Substring(0, $ttIncludeCopy.Length - 10) + '.merge.tt'
                       $ttincludeName = [System.IO.Path]::GetFileName($ttIncludeCopy)
                       if (($existingWPFClientTTIncludes -eq $null) -or (-not ($existingWPFClientTTIncludes.Contains($ttincludeName))))
                       {
                           $null = $wpfClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
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
           $ttSpecialMergeFileCopy = Join-Path $clientWPFTemplatesFolder $ttSpecialMergeFileName
           if (-not ([System.IO.File]::Exists($ttSpecialMergeFileName)))
           {
               copy $specialMergeFile $ttSpecialMergeFileCopy
               if (($existingWPFClientTTIncludes -eq $null) -or (-not ($existingWPFClientTTIncludes.Contains($ttSpecialMergeFileName))))
               {
                   $null = $wpfClientTemplatesProjectItems.AddFromFile($ttSpecialMergeFileCopy)
               }
           }
       }
       try
       {
           $wpfClientTemplatesUIHierarchyItems.UIHierarchyItems.Expanded = $wpfClientTemplatesExpanded
           $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded = $solutionItemsExpanded
       }
       catch
       {
       }
       MergeWPFClientTTIncludes
    }

    if ($kind -eq "FrameworkOnly")
    {
        $edmxName = "Framework"
    }
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Client.WPF.waqs"))) 
    if ($kind -ne "GlobalOnly")
    {
        $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Client.WPF.tt"))) 
    }
    try
    {
        ($projectUIHierarchyItems | ? {$_.Name -eq ('WAQS.' + $edmxName)})[0].UIHierarchyItems.Expanded = $false
    }
    catch
    {
    }
    if ($withGlobal)
    {
        $null = (Get-Project).ProjectItems.AddFromFile($appConfigPath) 
        if ($kind -eq "GlobalOnly")
        {
            $waqsGeneralUIHierarchyItems = $projectUIHierarchyItems | ? {$_.Name -eq 'WAQS'}
            if ($waqsGeneralUIHierarchyItems -ne $null)
            {
                $waqsGeneralUIHierarchyItems = $waqsGeneralUIHierarchyItems[0].UIHierarchyItems
                $waqsGeneralUIHierarchyItemsExpanded = $waqsGeneralUIHierarchyItems.Expanded;
            }
            $null = (Get-Project).ProjectItems.AddFromFile($contextsPath)
            if ($waqsGeneralUIHierarchyItems -eq $null)
            {
               try
               {
                   ($projectUIHierarchyItems | ? {$_.Name -eq 'WAQS'})[0].UIHierarchyItems.Expanded = $false
               }
               catch
               {
               }
            }
            else
            {
               try
               {
                  $waqsGeneralUIHierarchyItems.Expanded = $waqsGeneralUIHierarchyItemsExpanded
               }
               catch
               {
               }
            }
        }
    }
    if ($isOnTfs)
    {
        $DTE.ExecuteCommand("File.TfsRefreshStatus")
    }
}

function WAQSClientWPF($edmxPath, $svcPath, $kind, $sourceControl, $netVersion, $option)
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
    $svcUrl = [System.Text.RegularExpressions.Regex]::Match($svcPath, "^http(?:s)?://(?:.*?)$").Value
    if ($svcUrl.Length -eq 0)
    {
        $svcPath = [System.Text.RegularExpressions.Regex]::Match($svcPath, '^\"?(.*?)\"?$').Groups[1].Value
        $svcProjectProperties = $DTE.Solution.FindProjectItem($svcPath).ContainingProject.Properties
        if (($kind -eq "All") -or ($kind -eq "WithoutGlobal") -or ($kind -eq "WithoutFramework") -or ($kind -eq "WithoutGlobalWithoutFramework"))
        {
            $DTE.Solution.SolutionBuild.Build($true)
            $svcProjectPath = ($svcProjectProperties | ?{$_.Name -eq "LocalPath" } | select -ExpandProperty Value) + "\"
            if (-not ($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIIS"} | select -ExpandProperty Value))
            {
                $svcPort = $svcProjectProperties | ?{$_.Name -eq "WebApplication.DevelopmentServerPort" } | select -ExpandProperty Value
                $vsVersion = $DTE.Version
                $webServerPath = Join-Path "$env:ProgramFiles" ('Common Files\microsoft shared\DevServer\' + $vsVersion + '\WebDev.WebServer40.exe')
            }
            else 
            {
                if (($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIISExpress"} | select -ExpandProperty Value) -eq $true)
                {
                    $svcPort = [System.Text.RegularExpressions.Regex]::Match(($svcProjectProperties | ?{$_.Name -eq "WebApplication.IISUrl" } | select -ExpandProperty Value), ':(\d+)(?:/|$)').Groups[1].Value
                    $webServerPath = Join-Path "$env:ProgramFiles" 'IIS Express\iisexpress.exe'
                }
            }
            if (-not (Test-Path $webServerPath))
            {
                throw "Can't start the service. Please use the service url instead of the service path"
            }
            if ($webServerPath -ne $null)
            {
                 if ((([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpConnections() | foreach{$_.LocalEndPoint.Port}) -notcontains $svcPort) -and (([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() | foreach{$_.Port}) -notcontains $svcPort))
                {
                    start-process -filepath $webServerPath -ArgumentList ('/port:' + $svcPort + ' /path:"' + $svcProjectPath + '"') -NoNewWindow
                }
            }
            $svcUrl = ($svcProjectProperties | ?{$_.Name -eq "WebApplication.BrowseURL"} | select -ExpandProperty Value) + "/" + $svcPath.SubString(($svcProjectProperties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | select -ExpandProperty Length)).Replace("\", "/")
        }
        else
        {
            if ($kind -eq "GlobalOnly")
            {
                if (-not ($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIIS"} | select -ExpandProperty Value))
                {
                    $svcPort = $svcProjectProperties | ?{$_.Name -eq "WebApplication.DevelopmentServerPort" } | select -ExpandProperty Value
                }
                else 
                {
                    if (($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIISExpress"} | select -ExpandProperty Value) -eq $true)
                    {
                        $svcPort = [System.Text.RegularExpressions.Regex]::Match(($svcProjectProperties | ?{$_.Name -eq "WebApplication.IISUrl" } | select -ExpandProperty Value), ':(\d+)(?:/|$)').Groups[1].Value
                    }
                }              
                $svcUrl = ($svcProjectProperties | ?{$_.Name -eq "WebApplication.BrowseURL"} | select -ExpandProperty Value) + "/" + $svcPath.SubString(($svcProjectProperties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | select -ExpandProperty Length)).Replace("\", "/")
            }
        }
    }
    WAQSClientWPFInternal $edmxPath $svcUrl $kind $sourceControl $netVersion $option
}

Register-TabExpansion 'WAQSClientWPF' @{ 
'edmxPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".edmx")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
'svcPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".svc")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
'kind' = { "All", "WithoutGlobal", "WithoutFramework", "WithoutGlobalWithoutFramework", "FrameworkOnly", "GlobalOnly" }
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions }
}

Export-ModuleMember WAQSClientWPF




function WAQSGlobalClientWPFInternal($contexts, $svcUrl, $sourceControl, $netVersion, $option)
{
    if ($contexts -eq $null)
    {
           throw "contexts file cannot be null"
    }
    if (($netVersion -eq $null) -or (([array] $(GetAvailableVersions)) -notcontains $netVersion))
    {
        throw "This .NET version is not supported"
    }
    if ($svcUrl -eq $null)
    {
        throw "If kind is not FrameworkOnly, svcUrl cannot be null"
    }

    if ($netVersion -eq "NET46")
    {
        Write-Host "Note that .NET 4.6 new operators are not supported on specifications yet"
    }

    $projectPath = (Get-Project).FullName
    $projectDirectoryPath = [System.IO.Path]::GetDirectoryName($projectPath)
    $waqsDirectory = Join-Path $projectDirectoryPath "WAQSGlobal"
    
    if (Test-Path $waqsDirectory)
    {
        throw "$waqsDirectory already exists"
    }
    
    $defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value

    $toolsPath = GetToolsPath
    $toolsPath = Join-Path $toolsPath "Client.WPF"
    $references = (Get-Project).Object.References
    $null = $references.Add("System")
    $null = $references.Add("System.Core")
    $null = $references.Add("System.Runtime.Serialization")
    $null = $references.Add("System.ServiceModel")
    if ($netVersion -eq "NET40")
    {
        switch ($DTE.Version)
        {
            '10.0' {Install-Package AsyncCTP}
            '11.0' {Install-package Microsoft.CompilerServices.AsyncTargetingPack}
        }
    }
    switch ($DTE.Version)
    {
        '10.0' {$VSVersion = "VS10"}
        '11.0' {$VSVersion = "VS11"}
        '12.0' {$VSVersion = "VS12"}
        '14.0' {$VSVersion = "VS14"}
    }
    
    $appConfigFilePath = $DTE.Solution.FindProjectItem($contexts).ContainingProject.ProjectItems | ?{$_.Name -eq 'App.config'} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value
    $appXamlCsFilePath = GetAllProjectItems($DTE.Solution.FindProjectItem($contexts).ContainingProject) | ?{$_.Name -eq 'App.xaml.cs'} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value

    try
    {
        if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
        {
            if (Test-Path $appConfigFilePath)
            {
                $isOnTfs = (add-TfsPendingChange -edit $appConfigFilePath) -ne $null
            }
            if (Test-Path $projectPath)
            {
                $isOnTfs = (add-TfsPendingChange -edit $projectPath) -ne $null
            }
        }
    }
    catch
    {
    }

    $exePath = Join-Path $toolsPath InitWAQSClientWPFGlobal.exe
    $exeArgs = @('"' + $toolsPath + '"', '"' + $projectDirectoryPath + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $svcUrl +'"', '"' + $contexts + '"', '"' + $defaultNamespace + '"', '"' + $appConfigFilePath + '"', '"' + $appXamlCsFilePath + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"')
    if ($option -eq 'Debug')
    {
       Write-Host $exePath
       Write-Host $exeArgs
    }
    start-process -filepath $exePath -ArgumentList $exeArgs -Wait
        
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory "Global.Client.WPF.waqs")) 
    $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory "Global.Client.WPF.tt")) 

    if ($isOnTfs)
    {
        $DTE.ExecuteCommand("File.TfsRefreshStatus")
    }
}

function WAQSGlobalClientWPF($contexts, $svcPath, $sourceControl, $netVersion, $option)
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
    
    $contexts = [System.Text.RegularExpressions.Regex]::Match($contexts, '^\"?(.*?)\"?$').Groups[1].Value

    $svcUrl = [System.Text.RegularExpressions.Regex]::Match($svcPath, "^http(?:s)?://(?:.*?)$").Value
    if ($svcUrl.Length -eq 0)
    {
        $svcPath = [System.Text.RegularExpressions.Regex]::Match($svcPath, '^\"?(.*?)\"?$').Groups[1].Value
        $svcProjectProperties = $DTE.Solution.FindProjectItem($svcPath).ContainingProject.Properties
        if (-not ($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIIS"} | select -ExpandProperty Value))
        {
            $svcPort = $svcProjectProperties | ?{$_.Name -eq "WebApplication.DevelopmentServerPort" } | select -ExpandProperty Value
        }
        else 
        {
            if (($svcProjectProperties | ?{$_.Name -eq "WebApplication.UseIISExpress"} | select -ExpandProperty Value) -eq $true)
            {
                $svcPort = [System.Text.RegularExpressions.Regex]::Match(($svcProjectProperties | ?{$_.Name -eq "WebApplication.IISUrl" } | select -ExpandProperty Value), ':(\d+)(?:/|$)').Groups[1].Value
            }
        }              
        $svcUrl = ($svcProjectProperties | ?{$_.Name -eq "WebApplication.BrowseURL"} | select -ExpandProperty Value) + "/" + $svcPath.SubString(($svcProjectProperties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | select -ExpandProperty Length)).Replace("\", "/")
    }
    WAQSGlobalClientWPFInternal $contexts $svcUrl $sourceControl $netVersion $option
}

Register-TabExpansion 'WAQSGlobalClientWPF' @{ 
'contexts' = { $DTE.Solution.FindProjectItem("Contexts.xml") | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} } ;
'svcPath' = { $DTE.Solution.FindProjectItem("Global.svc") | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} } ;
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions };
}

Export-ModuleMember WAQSGlobalClientWPF




function WAQSApplyViewModelWPF($edmxName, $xamlFilePath)
{
    if ($edmxName -eq $null)
    {
        throw "edmxName cannot be null"
    }
    $project = ($DTE.Solution.FindProjectItem($DTE.ActiveDocument.FullName)).ContainingProject
    $version = ($project.Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value
    if (-not $version.StartsWith('.NETFramework,'))
    {
        throw "This project is not a .NET project ($version)"
    }

    $edmxName = [System.Text.RegularExpressions.Regex]::Match($edmxName, '^\"?(.*?)\"?$').Groups[1].Value
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($edmxName, "^\w[\w\d]*$"))
    {
      throw "Invalid edmx name"
    }
    $xamlFilePath = [System.Text.RegularExpressions.Regex]::Match($xamlFilePath, '^\"?(.*?)\"?$').Groups[1].Value

    $toolsPath = GetToolsPath
    $defaultNamespace = ($project.Properties | ? {$_.Name -eq 'RootNamespace'}).Value
    $activeDocumentPath = $DTE.ActiveDocument.FullName
    
    try
    {
        if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
        {
            $null = add-TfsPendingChange -edit $activeDocumentPath
            if (($xamlFilePath -ne $null) -and (Test-Path $xamlFilePath))
            {
                $isOnTfs = (add-TfsPendingChange -edit $xamlFilePath) -ne $null
                if ($isOnTfs)
                {
                    $null = add-TfsPendingChange -edit ($xamlFilePath + ".cs")
                }
            }
        }
    }
    catch
    {
    }

    $clientVersion = "WPF"
    $toolsPath = Join-Path $toolsPath ("Client." + $clientVersion)
    $exePath = Join-Path $toolsPath "InitViewModel.exe"
    $waqsFilePath = ($DTE.Solution.FindProjectItem((Join-Path ([System.IO.Path]::GetDirectoryName($project.FullName)) (Join-Path ("WAQS." + $edmxName) ($edmxName + ".Client." + $clientVersion + ".waqs")))).Properties | ? {$_.Name -eq "LocalPath"}).Value
    $exeArgs = @('"' + $edmxName + '"', '"' + $defaultNamespace + '"', '"' + $activeDocumentPath +'"', '"' + $waqsFilePath + '"', '"' + $xamlFilePath + '"')
    start-process -filepath $exePath -ArgumentList $exeArgs -Wait
    if ($isOnTfs)
    {
        $DTE.ExecuteCommand("File.TfsRefreshStatus")
    }
}

Register-TabExpansion 'WAQSApplyViewModelWPF' @{ 
'edmxName' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{$_.Name.EndsWith(".edmx")} | select {[System.IO.Path]::GetFileNameWithoutExtension($_.Name)} | select -unique -ExpandProperty * | Sort-Object | foreach {'"' + $_ + '"'} };
'xamlFilePath' = { (GetAllProjectItems (Get-Project)) | ?{$_.Name.EndsWith(".xaml")} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
}

Export-ModuleMember WAQSApplyViewModelWPF

function UpdateWAQSClientWPFT4Templates()
{
    $projectsT4RootItems = GetProjects | foreach {(GetAllT4RootItems $_)}
    foreach ($file in $projectsT4RootItems | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | ?{$_.EndsWith(".Client.WPF.tt")})
    {
        RecursiveGeneration $file
    }
    foreach ($file in $projectsT4RootItems | ?{$_.Name -eq 'Global.Client.WPF.tt'} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)
    {
        RecursiveGeneration $file
    }
}

Export-ModuleMember UpdateWAQSClientWPFT4Templates


function MergeWPFClientTTIncludes()
{
    $solutionItems = $null
    foreach ($p in $DTE.Solution.Projects | ?{$_.Name -eq 'Solution Items'}) 
    { 
        $solutionItems = $p 
    }
    if ($solutionItems -ne $null)
    {
        $wpfClientTemplates = $null
        foreach ($pi in $solutionItems.ProjectItems | ?{$_.Name -eq 'WPFClientTemplates'})
        {
            $wpfClientTemplates = $pi
        }
        if ($wpfClientTemplates -ne $null)
        {
            $ttFolderPath = Join-Path ([System.IO.Path]::GetDirectoryName($DTE.Solution.FullName)) 'WPFClientTemplates'
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
            foreach ($tt in $wpfClientTemplates.SubProject.ProjectItems | ?{$_.Name.EndsWith('.merge.tt')})
            {
                $transformTemplatesArgs = ('"' + (Join-Path $ttFolderPath $tt.Name) + '"', '-I "' + $ttIncludePath + '"')
                start-process -filepath $transformTemplatesExePath -ArgumentList $transformTemplatesArgs -WindowStyle Hidden -Wait
            }
        }
    }
}

Export-ModuleMember MergeWPFClientTTIncludes