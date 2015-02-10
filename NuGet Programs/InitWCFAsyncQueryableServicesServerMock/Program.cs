using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InitWCFAsyncQueryableServicesServerMock
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
                string projectDirectoryPath = args[argIndex++];
                string toolsServerMockPath = args[argIndex++];
                string rootNamespace = args[argIndex++];
                string waqsDirectory = args[argIndex++];
                string waqsGeneralDirectory = args[argIndex++];
                string entitiesSolutionPath = args[argIndex++];
                string entitiesProjectPath = args[argIndex++];
                string netVersion = args[argIndex++];
                string vsVersion = args[argIndex++];
                string kind = args[argIndex++];
                bool sourceControl = args[argIndex++] == "WithSourceControl";
                string slnFilePath = args[argIndex++];
                string slnTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "ServerMockTemplates");
                string entitiesFolderPath = null;
                string specificationsSlnFilePath = slnFilePath;
                string specificationsCsprojPath = null;
                string specificationsFolderPath = null;
                string serverEntitiesNamespace = null;
                string serverDALInterfacesNamespace = null;
                string serverDALNamespace = null;
                string dtoCsprojPath = null;
                string dtoFolderPath = null;
                string edmxProjectPath = null;
                string appConfigPath = null;

                if (kind == "All" || kind == "WithoutFramework")
                {
                    specificationsCsprojPath = args[argIndex++];
                    specificationsFolderPath = args[argIndex++];
                    dtoCsprojPath = args[argIndex++];
                    dtoFolderPath = args[argIndex++];
                    entitiesFolderPath = Path.GetDirectoryName(args[argIndex]);
                    string entityContent;
                    using (var sr = new StreamReader(args[argIndex++]))
                    {
                        entityContent = sr.ReadToEnd();
                    }
                    serverEntitiesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(entityContent));
                    string dalInterfacesContent;
                    using (var sr = new StreamReader(args[argIndex++]))
                    {
                        dalInterfacesContent = sr.ReadToEnd();
                    }
                    serverDALInterfacesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(dalInterfacesContent));
                    string dalContent;
                    using (var sr = new StreamReader(args[argIndex++]))
                    {
                        dalContent = sr.ReadToEnd();
                    }
                    serverDALNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(dalContent));
                    edmxProjectPath = args[argIndex++];
                    appConfigPath = args[argIndex++];
                }

                if (!Directory.Exists(waqsDirectory))
                {
                    string entitiesNamespace = null;
                    string clientContextNamespace = null;

                    Directory.CreateDirectory(waqsDirectory);

                    string toolsWaqsPath = null;
                    switch (kind)
                    {
                        case "All":
                            toolsWaqsPath = Path.Combine(toolsServerMockPath, "Server.Mock.waqs");
                            break;
                        case "WithoutFramework":
                            toolsWaqsPath = Path.Combine(toolsServerMockPath, "Server.MockWithoutFramework.waqs");
                            break;
                        case "FrameworkOnly":
                            toolsWaqsPath = Path.Combine(toolsServerMockPath, "Server.MockFrameworkOnly.waqs");
                            edmxName = "Framework";
                            break;
                    }
                    string serverMockWAQS = Path.Combine(waqsDirectory, edmxName + ".Server.Mock.waqs");
                    if (toolsWaqsPath != null)
                    {
                        if (!File.Exists(serverMockWAQS))
                        {
                            entitiesSolutionPath = GetRelativePath(entitiesSolutionPath, waqsDirectory);
                            entitiesProjectPath = GetRelativePath(entitiesProjectPath, waqsDirectory);
                            specificationsSlnFilePath = GetRelativePath(specificationsSlnFilePath, waqsDirectory);
                            specificationsCsprojPath = GetRelativePath(specificationsCsprojPath, waqsDirectory);
                            specificationsFolderPath = GetRelativePath(specificationsFolderPath, waqsDirectory);
                            dtoCsprojPath = GetRelativePath(dtoCsprojPath, waqsDirectory);
                            dtoFolderPath = GetRelativePath(dtoFolderPath, waqsDirectory);
                            entitiesFolderPath = GetRelativePath(entitiesFolderPath, waqsDirectory);
                            CopyFile(toolsWaqsPath, serverMockWAQS, new[] { "$edmxPath$", edmxPath.Contains(":") ? GetRelativePath(edmxPath, waqsDirectory) : "..\\" + edmxPath }, new[] { "$ServerEntitiesSolutionPath$", entitiesSolutionPath }, new[] { "$ServerEntitiesProjectPath$", entitiesProjectPath }, new[] { "$ServerDALInterfacesNamespace$", serverDALInterfacesNamespace }, new[] { "$ServerDALNamespace$", serverDALNamespace }, new[] { "$EntitiesNamespace$", entitiesNamespace }, new[] { "$ClientContextNamespace$", clientContextNamespace }, new[] { "$SpecificationsSlnFilePath$", specificationsSlnFilePath }, new[] { "$SpecificationsCsprojPath$", specificationsCsprojPath }, new[] { "$SpecificationsFolderPath$", specificationsFolderPath + "\\" }, new[] { "$DTOSlnFilePath$", specificationsSlnFilePath }, new[] { "$DTOCsprojPath$", dtoCsprojPath }, new[] { "$DTOFolderPath$", dtoFolderPath + "\\" }, new[] { "$ServerEntitiesSolutionPath$", specificationsSlnFilePath }, new[] { "$ServerEntitiesProjectPath$", specificationsCsprojPath }, new[] { "$ServerEntitiesFolderPath$", entitiesFolderPath + "\\" }, new[] { "$ServerEntitiesNamespace$", serverEntitiesNamespace });
                        }

                        string serverMockTT = Path.Combine(waqsDirectory, edmxName + ".Server.Mock.tt");
                        if (!File.Exists(serverMockTT))
                            CopyTTFile(Path.Combine(toolsServerMockPath, "Server.Mock.tt"), serverMockTT, sourceControl, slnTTIncludesPath, new[] { "$edmxName$", edmxName }, new[] { "$RootNamespace$", rootNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });
                    }

                    if (kind != "FrameworkOnly")
                    {
                        string appConfigFilePath = Path.Combine(projectDirectoryPath, "App.config");

                        if (!File.Exists(appConfigFilePath))
                            CopyFile(Path.Combine(toolsServerMockPath, "App.config"), appConfigFilePath);

                        XElement appConfig = XElement.Load(appConfigFilePath);
                        XElement connectionStrings = appConfig.Element("connectionStrings");
                        string edmxFolderPath = Path.GetDirectoryName(edmxPath);
                        string edmxProjectFolderPath = Path.GetDirectoryName(edmxProjectPath);
                        string folderDiff = edmxFolderPath.Substring(edmxProjectFolderPath.Length + (edmxFolderPath.Length > edmxProjectFolderPath.Length ? 1 : 0));
                        if (connectionStrings == null)
                        {
                            if (appConfigPath != null)
                            {
                                XElement templateConfig = XElement.Load(appConfigPath);
                                XElement appConnectionString = templateConfig.Element("connectionStrings").Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                                appConnectionString.Attribute("connectionString").Value = Regex.Replace(appConnectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
                                appConfig.Add(
                                    new XElement("connectionStrings", appConnectionString));
                            }
                        }
                        else
                        {
                            XElement connectionString = connectionStrings.Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                            if (connectionString == null)
                            {
                                if (appConfigPath != null)
                                {
                                    XElement templateConfig = XElement.Load(appConfigPath);
                                    XElement appConnectionString = templateConfig.Element("connectionStrings").Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                                    appConnectionString.Attribute("connectionString").Value = Regex.Replace(appConnectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
                                    connectionStrings.Add(appConnectionString);
                                }
                            }
                            else
                                connectionString.Attribute("connectionString").Value = Regex.Replace(connectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
                        }

                        appConfig.Save(appConfigFilePath);
                    }

                    if (sourceControl)
                    {
                        string ttIncludesSourcePath = Path.Combine(Path.GetDirectoryName(toolsServerMockPath), "ttincludes");
                        string slnLocalTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "ServerMockTemplates");
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
                value = Regex.Replace(value, "WriteServerMock\\(((?:\\s*\"[^\"]+\"\\s*,?)+)\\)\\s*;", m => string.Concat("WriteServerMock(", m.Groups[1].Value, ", @\"", slnDirectoryRelative + "\\\");"));
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
