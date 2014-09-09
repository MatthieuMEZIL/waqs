function GetToolsPath()
{
	$modules = (Get-Module WCFAsyncQueryableFunctionsClientPCL | select -property path)
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
	}
	return $null
}

function GetAvailableVersions()
{
	switch ($DTE.Version)
	{
		'10.0' {$version = @("NET40")}
		'11.0' {$version = @("NET40", "NET45")}
		'12.0' {$version = @("NET40", "NET45")}
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
	$value = $projectItem.ProjectItems | ?{($_.Name -ne $null) -and ($_.Name.EndsWith(".cs"))} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value
	if ($value -is [Array])
	{
		return $value[0]
	}
	else
	{
		return $value
	}	
}





function WCFAsyncQueryableServicesClientPCLInternal($edmxPath, $svcUrl, $kind, $sourceControl, $netVersion, $option)
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
	if (($kind -eq "All") -or ($kind -eq "WithoutFramework") -or ($kind -eq "GlobalOnly"))
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
	$pclToolsPath = Join-Path $toolsPath "Client.PCL"
	$defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value
	$exePath = Join-Path $pclToolsPath InitWCFAsyncQueryableServicesClientPCL.exe
	$references = (Get-Project).Object.References
	$null = $references.Add("System")
	$null = $references.Add("System.Core")
	$null = $references.Add("System.Runtime.Serialization")
	$null = $references.Add("System.ServiceModel")
	Install-Package Microsoft.Bcl.Async
	
	try
	{
	   $referencesUIHierarchyItems.Expanded = $referencesExpanded
	}
	catch
	{
	}
	
	$withGlobal = ($kind -eq "All") -or ($kind -eq "WithoutFramework") -or ($kind -eq "GlobalOnly")
	if ($kind -eq "GlobalOnly")
	{
		try
		{
			if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
			{
    			if (($isOnTfs -ne $false) -and (Test-Path $contextsFilePath))
    			{
    				$null = add-TfsPendingChange -edit $contextsFilePath
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
	}
	$exeArgs = @('"' + $edmxPath + '"', '"' + $pclToolsPath + '"', '"' + $defaultNamespace + '"', '"' + $svcUrl +'"', '"' + $waqsDirectory + '"', '"' + $waqsGeneralDirectory + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.PCL.ClientContext.tt")).ProjectItems | ?{$_.Name -eq ($edmxName + "ExpressionTransformer.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + (($DTE.Solution.FindProjectItem(($edmxName + ".Client.PCL.ServiceProxy.tt")).ProjectItems | ?{$_.Name -eq ("I" + $edmxName + "Service.cs")}).Properties | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + (GetFirstCsFile($DTE.Solution.FindProjectItem(($edmxName + ".Client.PCL.Entities.tt")))) + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.PCL.ClientContext.tt")).ProjectItems | ?{$_.Name -eq ($edmxName + "ClientContext.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + ($DTE.Solution.FindProjectItem(($edmxName + ".Client.PCL.ClientContext.Interfaces.tt")).ProjectItems | ?{$_.Name -eq ("I" + $edmxName + "ClientContext.cs")} | foreach{$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value) + '"', '"' + $entitiesSolutionPath + '"', '"' + $entitiesProjectPath + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $kind + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"')
	if ($kind -eq "All" -or $kind -eq "WithoutFramework" -or $kind -eq "WithoutGlobal" -or $kind -eq "WithoutGlobalWithoutFramework")
	{
	   $projectItems = GetProjects | foreach {(GetAllProjectItems $_)}
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
	   $clientPCLTemplatesFolder = Join-Path $slnFolder "PCLClientTemplates"
	   if (-not (Test-Path $clientPCLTemplatesFolder))
	   {
	       [System.IO.Directory]::CreateDirectory($clientPCLTemplatesFolder)
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
	   $pclClientTemplates = $null
       foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'PCLClientTemplates'})
       {
           $pclClientTemplates = $p.SubProject
	       $pclClientTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'PCLClientTemplates'}
	       $pclClientTemplatesExpanded = $pclClientTemplatesUIHierarchyItems.UIHierarchyItems.Expanded
       }
       while ($pclClientTemplates -eq $null)
       {
	       try
	       {
               $pclClientTemplates = $solutionItems.Object.AddSolutionFolder('PCLClientTemplates')
               $pclClientTemplatesUIHierarchyItems = $solutionItemsUIHierarchyItems.UIHierarchyItems | ?{$_.Name -eq 'PCLClientTemplates'}
	           $pclClientTemplatesExpanded = $false
	       }
	       catch # a strange bug can append: Method invocation failed because [System.__ComObject] does not contain a method named 'AddSolutionFolder'.
	       {
	           if ($option -eq 'Debug')
	           {
	               Write-Host "Catch PCLClientTemplates"
	           }
               foreach ($p in $solutionItems.ProjectItems | ?{$_.Name -eq 'PCLClientTemplates'})
               {
                   $pclClientTemplates = $p.SubProject
               }
	       }
       }
       
	   $ttincludesFolder = Join-Path $toolsPath 'ttincludes'
	   $pclClientTemplatesProjectItems = $pclClientTemplates.ProjectItems
	   $existingPCLClientTTIncludes = $pclClientTemplatesProjectItems | select -ExpandProperty Name
	   foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolder) | ?{[System.IO.Path]::GetFileName($_).StartsWith("WCFAsyncQueryableServices.")})
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
        	       $ttIncludeCopy = Join-Path $clientPCLTemplatesFolder $ttincludeName
        	       if (($existingPCLClientTTIncludes -eq $null) -or (-not ($existingPCLClientTTIncludes.Contains($ttincludeName))))
        	       {   
        	           $null = $pclClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
        	       }
    	       }
    	   }
	   }
	   switch ($DTE.Version)
	   {
    		'11.0' {$vsVersion = 'VS11'}
    		'12.0' {$vsVersion = 'VS12'}
	   }
       $ttincludesFolderVS = Join-Path $ttincludesFolder $vsVersion
	   foreach ($ttinclude in [System.IO.Directory]::GetFiles($ttincludesFolderVS))
	   {
           $ttincludeName = [System.IO.Path]::GetFileName($ttinclude)
           if ([System.IO.Path]::GetFileName($ttincludeName).StartsWith("WCFAsyncQueryableServices."))
           {
    	       $m = [System.Text.RegularExpressions.Regex]::Match($ttincludeName, '.(NET\d+).')
    	       if ((-not ($m.Success)) -or ($m.Groups[1].Value -eq $netVersion))
    	       {
        	       $ttIncludeCopy = Join-Path $clientPCLTemplatesFolder $ttincludeName
        	       if (($existingPCLClientTTIncludes -eq $null) -or (-not ($existingPCLClientTTIncludes.Contains($ttincludeName))))
        	       {
        	           $null = $pclClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
        	       }
        	       if ($ttinclude.Contains(('.' + $vsVersion + '.' + $netVersion + '.')))
        	       {
            	       $ttIncludeCopy = $ttIncludeCopy.Substring(0, $ttIncludeCopy.Length - 10) + '.merge.tt'
            	       $ttincludeName = [System.IO.Path]::GetFileName($ttIncludeCopy)
            	       if (($existingPCLClientTTIncludes -eq $null) -or (-not ($existingPCLClientTTIncludes.Contains($ttincludeName))))
            	       {
            	           $null = $pclClientTemplatesProjectItems.AddFromFile($ttIncludeCopy)
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
           $ttSpecialMergeFileCopy = Join-Path $clientPCLTemplatesFolder $ttSpecialMergeFileName
	       if (-not ([System.IO.File]::Exists($ttSpecialMergeFileName)))
	       {
	           copy $specialMergeFile $ttSpecialMergeFileCopy
    	       if (($existingPCLClientTTIncludes -eq $null) -or (-not ($existingPCLClientTTIncludes.Contains($ttSpecialMergeFileName))))
    	       {
    	           $null = $pclClientTemplatesProjectItems.AddFromFile($ttSpecialMergeFileCopy)
    	       }
	       }
	   }
	   try
	   {
    	   $pclClientTemplatesUIHierarchyItems.UIHierarchyItems.Expanded = $pclClientTemplatesExpanded
    	   $solutionItemsUIHierarchyItems.UIHierarchyItems.Expanded = $solutionItemsExpanded
	   }
	   catch
	   {
	   }
	   MergePCLClientTTIncludes
	}

    if ($kind -eq "FrameworkOnly")
	{
		$edmxName = "Framework"
	}
	$null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Client.PCL.waqs"))) 
	if ($kind -ne "GlobalOnly")
	{
	   $null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory ($edmxName + ".Client.PCL.tt"))) 
	}
	try
	{
        ($projectUIHierarchyItems | ? {$_.Name -eq ('WAQS.' + $edmxName)})[0].UIHierarchyItems.Expanded = $false
    }
    catch
    {
    }
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
	if ($isOnTfs)
	{
		$DTE.ExecuteCommand("File.TfsRefreshStatus")
	}
}

function WCFAsyncQueryableServicesClientPCL($edmxPath, $svcPath, $kind, $sourceControl, $netVersion, $option)
{
	$version = ((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value
	if (-not $version.StartsWith('.NETPortable,'))
	{
		throw "This project is not a Portable project ($version)"
	}

	if ($netVersion -eq $null)
	{
		$netVersion = "NET" + ([System.Text.RegularExpressions.Regex]::Match((((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value), "Version=v(\d+.\d+)").Groups[1].Value.Replace(".", ""))
	}
	if ($kind -eq $null)
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
	}
	WCFAsyncQueryableServicesClientPCLInternal $edmxPath $svcUrl $kind $sourceControl $netVersion $option
}

Register-TabExpansion 'WCFAsyncQueryableServicesClientPCL' @{ 
'edmxPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".edmx")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} }
'svcPath' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{($_.Name.EndsWith(".svc")) -and (-not (Test-Path (Join-Path ([System.IO.Path]::GetDirectoryName((Get-Project).FullName)) ("WAQS." + [System.IO.Path]::GetFileNameWithoutExtension($_.Name)))))} | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} } 
'kind' = { "All", "WithoutGlobal", "WithoutFramework", "WithoutGlobalWithoutFramework", "FrameworkOnly", "GlobalOnly" }
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions }
}

Export-ModuleMember WCFAsyncQueryableServicesClientPCL




function WCFAsyncQueryableServicesGlobalClientPCLInternal($contexts, $svcUrl, $sourceControl, $netVersion, $option)
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

    $projectPath = (Get-Project).FullName
	$projectDirectoryPath = [System.IO.Path]::GetDirectoryName($projectPath)
	$waqsDirectory = Join-Path $projectDirectoryPath "WAQSGlobal"

    switch ($DTE.Version)
	{
		'10.0' {$VSVersion = "VS10"}
		'11.0' {$VSVersion = "VS11"}
		'12.0' {$VSVersion = "VS12"}
	}
	
	if (Test-Path $waqsDirectory)
	{
		throw "$waqsDirectory already exists"
	}
	
	$defaultNamespace = ((Get-Project).Properties | ? {$_.Name -eq 'RootNamespace'}).Value

	$toolsPath = GetToolsPath
	$toolsPath = Join-Path $toolsPath "Client.PCL"

	try
	{
		if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
		{
			if (Test-Path $projectPath)
			{
				$isOnTfs = (add-TfsPendingChange -edit $projectPath) -ne $null
			}
		}
	}
	catch
	{
	}
	Install-Package Microsoft.Bcl.Async

	$exePath = Join-Path $toolsPath InitWCFAsyncQueryableServicesClientPCLGlobal.exe
	$exeArgs = @('"' + $toolsPath + '"', '"' + $projectDirectoryPath + '"', '"' + $netVersion + '"', '"' + $VSVersion + '"', '"' + $svcUrl +'"', '"' + $contexts + '"', '"' + $defaultNamespace + '"', '"' + $sourceControl + '"', '"' + (($DTE.Solution).FullName) + '"')
	if ($option -eq 'Debug')
	{
	   Write-Host $exePath
	   Write-Host $exeArgs
	}
	start-process -filepath $exePath -ArgumentList $exeArgs -Wait
		
	$null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory "Global.Client.PCL.waqs")) 
	$null = (Get-Project).ProjectItems.AddFromFile((Join-Path $waqsDirectory "Global.Client.PCL.tt")) 

    if ($isOnTfs)
	{
		$DTE.ExecuteCommand("File.TfsRefreshStatus")
	}
}

function WCFAsyncQueryableServicesGlobalClientPCL($contexts, $svcPath, $sourceControl, $netVersion, $option)
{
	$version = ((Get-Project).Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value
	if (-not $version.StartsWith('.NETPortable,'))
	{
		throw "This project is not a Portable project ($version)"
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
	WCFAsyncQueryableServicesGlobalClientPCLInternal $contexts $svcUrl $sourceControl $netVersion $option
}

Register-TabExpansion 'WCFAsyncQueryableServicesGlobalClientPCL' @{ 
'contexts' = { $DTE.Solution.FindProjectItem("Contexts.xml") | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} } 
'svcPath' = { $DTE.Solution.FindProjectItem("Global.svc") | foreach {$_.Properties | ?{$_.Name -eq 'LocalPath'} | select -ExpandProperty Value} | Sort-Object | foreach {'"' + $_ + '"'} } 
'sourceControl' = { "WithSourceControl", "WithoutSourceControl" }
'netVersion' = { GetAvailableVersions }
}

Export-ModuleMember WCFAsyncQueryableServicesGlobalClientPCL


function WCFAsyncQueryableServicesApplyViewModelPCL($edmxName)
{
	if ($edmxName -eq $null)
	{
		throw "edmxName cannot be null"
	}
	$project = ($DTE.Solution.FindProjectItem($DTE.ActiveDocument.FullName)).ContainingProject
	$version = ($project.Properties | ?{$_.Name -eq "TargetFrameworkMoniker"}).Value
	if (-not $version.StartsWith('.NETPortable,'))
	{
		throw "This project is not a Portable project ($version)"
	}

	$edmxName = [System.Text.RegularExpressions.Regex]::Match($edmxName, '^\"?(.*?)\"?$').Groups[1].Value
	if (-not [System.Text.RegularExpressions.Regex]::IsMatch($edmxName, "^\w[\w\d]*$"))
	{
	  throw "Invalid edmx name"
	}

	$toolsPath = GetToolsPath
	$defaultNamespace = ($project.Properties | ? {$_.Name -eq 'RootNamespace'}).Value
	$activeDocumentPath = $DTE.ActiveDocument.FullName
	
	try
	{
		if (((Get-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null) -or ((Add-PSSnapin Microsoft.TeamFoundation.PowerShell -ErrorAction SilentlyContinue) -ne $null))
		{
    		$null = add-TfsPendingChange -edit $activeDocumentPath
    	}
	}
	catch
	{
	}

	$clientVersion = "PCL"
	$toolsPath = Join-Path $toolsPath ("Client." + $clientVersion)
    $waqsFilePath = ($DTE.Solution.FindProjectItem((Join-Path ([System.IO.Path]::GetDirectoryName($project.FullName)) (Join-Path ("WAQS." + $edmxName) ($edmxName + ".Client." + $clientVersion + ".waqs")))).Properties | ? {$_.Name -eq "LocalPath"}).Value
    $exePath = Join-Path $toolsPath "InitViewModel.exe"
	$exeArgs = @('"' + $edmxName + '"', '"' + $defaultNamespace + '"', '"' + $activeDocumentPath +'"', '"' + $waqsFilePath + '"', '""')
	start-process -filepath $exePath -ArgumentList $exeArgs -Wait
	if ($isOnTfs)
	{
		$DTE.ExecuteCommand("File.TfsRefreshStatus")
	}
}

Register-TabExpansion 'WCFAsyncQueryableServicesApplyViewModelPCL' @{ 
'edmxName' = { GetProjects | foreach {(GetAllProjectItems $_)} | ?{$_.Name.EndsWith(".edmx")} | select {[System.IO.Path]::GetFileNameWithoutExtension($_.Name)} | select -unique -ExpandProperty * | Sort-Object | foreach {'"' + $_ + '"'} }
}

Export-ModuleMember WCFAsyncQueryableServicesApplyViewModelPCL




function UpdateWCFAsyncQueryableServicesClientPCLT4Templates()
{
    $projectsT4RootItems = GetProjects | foreach {(GetAllT4RootItems $_)}
    foreach ($file in $projectsT4RootItems | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value | ?{$_.EndsWith(".Client.PCL.tt")})
    {
        RecursiveGeneration $file
    }
    foreach ($file in $projectsT4RootItems | ?{$_.Name -eq 'Global.Client.PCL.tt'} | foreach {$_.Properties} | ?{$_.Name -eq "LocalPath"} | select -ExpandProperty Value)
    {
        RecursiveGeneration $file
    }
}

Export-ModuleMember UpdateWCFAsyncQueryableServicesClientPCLT4Templates


function MergePCLClientTTIncludes()
{
    $solutionItems = $null
    foreach ($p in $DTE.Solution.Projects | ?{$_.Name -eq 'Solution Items'}) 
    { 
        $solutionItems = $p 
    }
    if ($solutionItems -ne $null)
    {
        $pclClientTemplates = $null
        foreach ($pi in $solutionItems.ProjectItems | ?{$_.Name -eq 'PCLClientTemplates'})
        {
            $pclClientTemplates = $pi
        }
        if ($pclClientTemplates -ne $null)
        {
            $ttFolderPath = Join-Path ([System.IO.Path]::GetDirectoryName($DTE.Solution.FullName)) 'PCLClientTemplates'
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
            foreach ($tt in $pclClientTemplates.SubProject.ProjectItems | ?{$_.Name.EndsWith('.merge.tt')})
            {
                $transformTemplatesArgs = ('"' + (Join-Path $ttFolderPath $tt.Name) + '"', '-I "' + $ttIncludePath + '"')
                start-process -filepath $transformTemplatesExePath -ArgumentList $transformTemplatesArgs -Wait
            }
        }
    }
}

Export-ModuleMember MergePCLClientTTIncludes