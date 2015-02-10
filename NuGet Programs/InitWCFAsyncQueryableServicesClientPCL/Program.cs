using Microsoft.Win32;
using Roslyn.Compilers.CSharp;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InitWCFAsyncQueryableServicesClientPCL
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                int argIndex = 0;
                string edmxPath = args[argIndex++];
                string edmxName;
                if (string.IsNullOrEmpty(edmxPath))
                    edmxName = null;
                else
                    edmxName = Path.GetFileNameWithoutExtension(edmxPath);
                string toolsClientPCLPath = args[argIndex++];
                string rootNamespace = args[argIndex++];
                string svcUrl = args[argIndex++];
                string waqsDirectory = args[argIndex++];
                string waqsGeneralDirectory = args[argIndex++];
                string expressionTransformerPath = args[argIndex++];
                string serviceContractPath = args[argIndex++];
                string entitiesPath = args[argIndex++];
                string clientContextPath = args[argIndex++];
                string clientContextInterfacesPath = args[argIndex++];
                string serverEntitiesSolutionPath = args[argIndex++];
                string serverEntitiesProjectPath = args[argIndex++];
                string netVersion = args[argIndex++];
                string vsVersion = args[argIndex++];
                string kind = args[argIndex++];
                bool sourceControl = args[argIndex++] == "WithSourceControl";
                string slnFilePath = args[argIndex++];
                string slnTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "PCLClientTemplates");
                string serverEntitiesFolderPath = null;
                string specificationsSlnFilePath = slnFilePath;
                string specificationsCsprojPath = null;
                string specificationsFolderPath = null;
                string dtoSlnFilePath = null;
                string dtoCsprojPath = null;
                string dtoFolderPath = null;
                string serverEntitiesNamespace = null;

                if (kind == "All" || kind == "WithoutFramework" || kind == "WithoutGlobal" || kind == "WithoutGlobalWithoutFramework")
                {
                    dtoSlnFilePath = specificationsSlnFilePath;
                    specificationsCsprojPath = args[argIndex++];
                    specificationsFolderPath = args[argIndex++];
                    dtoCsprojPath = args[argIndex++];
                    dtoFolderPath = args[argIndex++];
                    var edmxDirectoryPath = Path.GetDirectoryName(edmxPath);
                    if (edmxDirectoryPath == Path.GetDirectoryName(serverEntitiesProjectPath))
                    {
                        if (Directory.Exists(specificationsFolderPath = Path.Combine(edmxDirectoryPath, "Specifications")))
                            specificationsCsprojPath = serverEntitiesProjectPath;
                        if (Directory.Exists(dtoFolderPath = Path.Combine(edmxDirectoryPath, "DTO")))
                            dtoCsprojPath = serverEntitiesProjectPath;
                    }
                    serverEntitiesFolderPath = Path.GetDirectoryName(args[argIndex]);
                    string entityContent;
                    using (var sr = new StreamReader(args[argIndex]))
                    {
                        entityContent = sr.ReadToEnd();
                    }
                    serverEntitiesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(entityContent));
                }

                if (!Directory.Exists(waqsDirectory))
                {
                    string entitiesNamespace = null;
                    string clientContextNamespace = null;
                    string clientContextInterfacesNamespace = null;
                    if (kind == "GlobalOnly" && !(string.IsNullOrEmpty(expressionTransformerPath) || string.IsNullOrEmpty(serviceContractPath) || string.IsNullOrEmpty(entitiesPath) || string.IsNullOrEmpty(clientContextPath) || string.IsNullOrEmpty(clientContextInterfacesPath)))
                    {
                        string expressionTransformerContent;
                        using (var sr = new StreamReader(expressionTransformerPath))
                        {
                            expressionTransformerContent = sr.ReadToEnd();
                        }
                        string serviceContractContent;
                        using (var sr = new StreamReader(serviceContractPath))
                        {
                            serviceContractContent = sr.ReadToEnd();
                        }
                        new GetNamespace().Visit(Syntax.ParseCompilationUnit(serviceContractContent));
                        string entitiesContent;
                        using (var sr = new StreamReader(entitiesPath))
                        {
                            entitiesContent = sr.ReadToEnd();
                        }
                        entitiesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(entitiesContent));
                        string clientContextContent;
                        using (var sr = new StreamReader(clientContextPath))
                        {
                            clientContextContent = sr.ReadToEnd();
                        }
                        clientContextNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(clientContextContent));
                        string clientContextInterfacesContent;
                        using (var sr = new StreamReader(clientContextInterfacesPath))
                        {
                            clientContextInterfacesContent = sr.ReadToEnd();
                        }
                        clientContextInterfacesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(clientContextInterfacesContent));
                    }
                    if (kind == "GlobalOnly")
                    {
                        string contextsFilePath = Path.Combine(waqsGeneralDirectory, "Contexts.xml");
                        if (Directory.Exists(waqsGeneralDirectory))
                        {
                            if (File.Exists(contextsFilePath))
                            {
                                var contexts = XElement.Load(contextsFilePath);
                                contexts.Add(XElement.Parse(string.Format("<Context Name=\"{0}\" WAQS=\"..\\WAQS.{0}\\{0}.Client.PCL.waqs\" />", edmxName)));
                                contexts.Save(contextsFilePath);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(waqsGeneralDirectory);
                            if (kind == "GlobalOnly")
                                using (var sw = new StreamWriter(contextsFilePath))
                                {
                                    sw.WriteLine(string.Concat(
@"<Contexts>
    <Context Name=", "\"{0}\" WAQS=\"..\\WAQS.{0}\\{0}.Client.PCL.waqs\" />", @"
</Contexts>"), edmxName);
                                }
                        }
                    }

                    Directory.CreateDirectory(waqsDirectory);

                    string toolsWaqsPath = null;
                    switch (kind)
                    {
                        case "All":
                        case "WithoutGlobal":
                            toolsWaqsPath = Path.Combine(toolsClientPCLPath, "Client.PCL.waqs");
                            break;
                        case "WithoutFramework":
                        case "WithoutGlobalWithoutFramework":
                            toolsWaqsPath = Path.Combine(toolsClientPCLPath, "ClientWithoutFramework.PCL.waqs");
                            break;
                        case "FrameworkOnly":
                            toolsWaqsPath = Path.Combine(toolsClientPCLPath, "ClientFrameworkOnly.PCL.waqs");
                            edmxName = "Framework";
                            break;
                        case "GlobalOnly":
                            toolsWaqsPath = Path.Combine(toolsClientPCLPath, "ClientGlobalOnly.PCL.waqs");
                            break;
                    }
                    string clientPCLWAQS = Path.Combine(waqsDirectory, edmxName + ".Client.PCL.waqs");
                    if (toolsWaqsPath != null)
                    {
                        if (!File.Exists(clientPCLWAQS))
                        {
                            serverEntitiesSolutionPath = GetRelativePath(serverEntitiesSolutionPath, waqsDirectory);
                            serverEntitiesProjectPath = GetRelativePath(serverEntitiesProjectPath, waqsDirectory);
                            specificationsSlnFilePath = GetRelativePath(specificationsSlnFilePath, waqsDirectory);
                            specificationsCsprojPath = GetRelativePath(specificationsCsprojPath, waqsDirectory);
                            specificationsFolderPath = GetRelativePath(specificationsFolderPath, waqsDirectory);
                            serverEntitiesFolderPath = GetRelativePath(serverEntitiesFolderPath, waqsDirectory);
                            dtoSlnFilePath = GetRelativePath(dtoSlnFilePath, waqsDirectory);
                            dtoCsprojPath = GetRelativePath(dtoCsprojPath, waqsDirectory);
                            dtoFolderPath = GetRelativePath(dtoFolderPath, waqsDirectory);
                            CopyFile(toolsWaqsPath, clientPCLWAQS, new[] { "$edmxPath$", edmxPath.Contains(":") ? GetRelativePath(edmxPath, waqsDirectory) : "..\\" + edmxPath }, new[] { "$svcUrl$", svcUrl }, new[] { "$EntitiesNamespace$", entitiesNamespace }, new[] { "$ClientContextNamespace$", clientContextNamespace }, new[] { "$ClientContextInterfacesNamespace$", clientContextInterfacesNamespace }, new[] { "$SpecificationsSlnFilePath$", specificationsSlnFilePath }, new[] { "$SpecificationsCsprojPath$", specificationsCsprojPath }, new[] { "$SpecificationsFolderPath$", specificationsFolderPath + "\\" }, new[] { "$DTOSlnFilePath$", dtoSlnFilePath }, new[] { "$DTOCsprojPath$", dtoCsprojPath }, new[] { "$DTOFolderPath$", dtoFolderPath + "\\" }, new[] { "$ServerEntitiesSlnFilePath$", serverEntitiesSolutionPath }, new[] { "$ServerEntitiesCsprojPath$", serverEntitiesProjectPath }, new[] { "$ServerEntitiesFolderPath$", serverEntitiesFolderPath + "\\" }, new[] { "$ServerEntitiesNamespace$", serverEntitiesNamespace });
                        }

                        if (kind != "GlobalOnly")
                        {
                            string clientPCLTT = Path.Combine(waqsDirectory, edmxName + ".Client.PCL.tt");
                            if (!File.Exists(clientPCLTT))
                                CopyTTFile(Path.Combine(toolsClientPCLPath, "Client.PCL.tt"), clientPCLTT, sourceControl, slnTTIncludesPath, new[] { "$edmxName$", edmxName }, new[] { "$RootNamespace$", rootNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });
                        }
                    }

                    if (sourceControl)
                    {
                        string ttIncludesSourcePath = Path.Combine(Path.GetDirectoryName(toolsClientPCLPath), "ttincludes");
                        string slnLocalTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "PCLClientTemplates");
                        if (!Directory.Exists(slnLocalTTIncludesPath))
                            Directory.CreateDirectory(slnLocalTTIncludesPath);
                        CopyTTIncludes(vsVersion, netVersion, ttIncludesSourcePath, slnLocalTTIncludesPath);
                        ttIncludesSourcePath = Path.Combine(ttIncludesSourcePath, vsVersion);
                        CopyTTIncludes(vsVersion, netVersion, ttIncludesSourcePath, slnLocalTTIncludesPath);
                    }

                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }


        private static void CopyTTIncludes(string vsVersion, string netVersion, string ttIncludesSourcePath, string slnLocalTTIncludesPath)
        {
            foreach (var ttInclude in Directory.GetFiles(ttIncludesSourcePath).Where(i => Path.GetFileName(i).StartsWith("WCFAsyncQueryableServices.")))
            {
                var m = Regex.Match(ttInclude, @".(NET\d+).");
                if (!m.Success || m.Groups[1].Value == netVersion)
                {
                    string ttIncludeFileName = Path.GetFileName(ttInclude);
                    if (ttIncludeFileName.EndsWith(".ttinclude.x64"))
                    {
                        if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows") == null)
                            continue;
                        ttIncludeFileName = ttIncludeFileName.Substring(0, ttIncludeFileName.Length - 4 /*.x64*/);
                    }
                    else if (ttIncludeFileName.EndsWith(".ttinclude.x86"))
                    {
                        if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows") != null)
                            continue;
                        ttIncludeFileName = ttIncludeFileName.Substring(0, ttIncludeFileName.Length - 4 /*.x86*/);
                    }
                    var ttIncludeCopy = Path.Combine(slnLocalTTIncludesPath, ttIncludeFileName);
                    if (!(File.Exists(ttIncludeCopy) && File.ReadAllText(ttInclude) == File.ReadAllText(ttIncludeCopy)))
                        File.Copy(ttInclude, ttIncludeCopy, true);
                    if (ttInclude.Contains(string.Concat(".", vsVersion, ".", netVersion, ".")))
                    {
                        using (var sw = new StreamWriter(ttIncludeCopy.Substring(0, ttIncludeCopy.Length - 10/*".ttinclude".Length*/) + ".merge.tt"))
                        {
                            sw.WriteLine("<#@ include file=\"MergeT4Files.ttinclude\"#>");
                            sw.WriteLine(string.Concat("<# LocalTTIncludesMerge(\"", ttIncludeFileName, "\"); #>"));
                        }
                    }
                }
            }
        }

        private static void CopyTTFile(string fileIn, string fileOut, bool sourceControl, string slnTTIncludesPath, params string[][] replaces)
        {
            if (!sourceControl)
            {
                CopyFile(fileIn, fileOut, replaces);
                return;
            }
            CopyFile(fileIn, fileOut, s =>
            {
                var value = s;
                foreach (string[] replace in replaces)
                    value = value.Replace(replace[0], replace[1]);
                string slnDirectoryRelative = GetRelativePath(slnTTIncludesPath, Path.GetDirectoryName(fileOut));
                value = Regex.Replace(value, "<#@\\s+include\\s+file=\"([^\"]+).ttinclude\"\\s*#>", m => string.Concat("<#@ include file=\"", Path.Combine(slnDirectoryRelative, m.Groups[1].Value + ".merge.ttinclude"), "\"#>"));
                value = Regex.Replace(value, "WriteClient\\(((?:\\s*\"[^\"]+\"\\s*,?)+)\\)\\s*;", m => string.Concat("WriteClient(", m.Groups[1].Value, ", @\"", slnDirectoryRelative + "\\\");"));
                return value;
            });
        }

        private static void CopyFile(string fileIn, string fileOut, params string[][] replaces)
        {
            CopyFile(fileIn, fileOut, s =>
            {
                var value = s;
                foreach (string[] replace in replaces)
                    value = value.Replace(replace[0], replace[1]);
                return value;
            });
        }

        private static void CopyFile(string fileIn, string fileOut, Func<string, string> replace)
        {
            using (var sr = new StreamReader(fileIn))
            {
                using (var sw = new StreamWriter(fileOut))
                {
                    string value = sr.ReadToEnd();
                    value = replace(value);
                    sw.Write(value);
                }
            }
        }

        public static string GetRelativePath(string fullPath, string currentPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return null;
            if (!currentPath.EndsWith("\\"))
                currentPath = currentPath + "\\";
            return new Uri(currentPath).MakeRelativeUri(new Uri(fullPath)).ToString().Replace("/", "\\");
        }
    }
}
