using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Roslyn.Compilers.CSharp;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace InitWAQSServer
{
    class Program
    {
        static void Main(string[] args)
        {
            bool addGlobalService = false;
            try
            {
                int argIndex = 0;
                string edmxPath = args[argIndex++];
                string edmxName;
                if (string.IsNullOrEmpty(edmxPath))
                {
                    edmxPath = null;
                    edmxName = null;
                }
                else
                    edmxName = Path.GetFileNameWithoutExtension(edmxPath);
                string edmxProjectPath = args[argIndex++];
                string projectDirectoryPath = args[argIndex++];
                string toolsServerPath = args[argIndex++];
                string rootNamespace = args[argIndex++];
                string assemblyName = args[argIndex++];
                string assemblyVersion = args[argIndex++];
                string netVersion = args[argIndex++];
                string vsVersion = args[argIndex++];
                string kind = args[argIndex++];
                string appKind = args[argIndex++];
                string waqsDirectory = args[argIndex++];
                string appConfigPath = args[argIndex++];
                bool sourceControl = args[argIndex++] == "WithSourceControl";
                string slnFilePath = args[argIndex++];
                bool isWCF = args[argIndex++] == "WCF";
                string slnTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "ServerTemplates");

                string dalInterfacesNamespace = null;
                string dalNamespace = null;
                string serviceInterfacesNamespace = null;
                string serviceNamespace = null;
                string wcfServiceContractNamespace = null;
                string wcfServiceNamespace = null;
                string csprojPath = null;
                string specificationsFolderPath = null;
                string dtoFolderPath = null;

                string assemblyNetVersion = null;
                switch (netVersion)
                {
                    case "NET40":
                        assemblyVersion = "4.0";
                        break;
                    case "NET45":
                        assemblyNetVersion = "4.5";
                        break;
                    case "NET46":
                        assemblyNetVersion = "4.6";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (kind == "GlobalOnly")
                {
                    string dalInterfaces;
                    using (StreamReader sr = new StreamReader(args[argIndex++]))
                    {
                        dalInterfaces = sr.ReadToEnd();
                    }
                    dalInterfacesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(dalInterfaces));
                    string dalFileName = args[argIndex++];
                    if (!string.IsNullOrEmpty(dalFileName))
                    {
                        string dal;
                        using (StreamReader sr = new StreamReader(dalFileName))
                        {
                            dal = sr.ReadToEnd();
                        }
                        dalNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(dal));
                    }
                    string serviceInterfacesFileName = args[argIndex++];
                    if (!string.IsNullOrEmpty(serviceInterfacesFileName))
                    {
                        string serviceInterfaces;
                        using (StreamReader sr = new StreamReader(serviceInterfacesFileName))
                        {
                            serviceInterfaces = sr.ReadToEnd();
                        }
                        serviceInterfacesNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(serviceInterfaces));
                    }
                    string serviceFileName = args[argIndex++];
                    if (!string.IsNullOrEmpty(serviceFileName))
                    {
                        string service;
                        using (StreamReader sr = new StreamReader(serviceFileName))
                        {
                            service = sr.ReadToEnd();
                        }
                        serviceNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(service));
                    }
                    if (isWCF)
                    {
                        string wcfServiceContractFileName = args[argIndex++];
                        if (!string.IsNullOrEmpty(wcfServiceContractFileName))
                        {
                            string wcfServiceContract;
                            using (StreamReader sr = new StreamReader(wcfServiceContractFileName))
                            {
                                wcfServiceContract = sr.ReadToEnd();
                            }
                            wcfServiceContractNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(wcfServiceContract));
                        }
                        string wcfServiceFileName = args[argIndex++];
                        if (!string.IsNullOrEmpty(wcfServiceFileName))
                        {
                            string wcfService;
                            using (StreamReader sr = new StreamReader(wcfServiceFileName))
                            {
                                wcfService = sr.ReadToEnd();
                            }
                            wcfServiceNamespace = new GetNamespace().Visit(Syntax.ParseCompilationUnit(wcfService));
                        }
                        string globalDirectoryPath = Path.Combine(projectDirectoryPath, "Global");
                        if (Directory.Exists(globalDirectoryPath))
                        {
                            string globalWCFServicePath = Path.Combine(globalDirectoryPath, "GlobalWCFService.cs");
                            if (File.Exists(globalWCFServicePath))
                            {
                                string globalWCFServiceContent;
                                using (var sr = new StreamReader(globalWCFServicePath))
                                {
                                    globalWCFServiceContent = sr.ReadToEnd();
                                }
                                using (var sw = new StreamWriter(globalWCFServicePath))
                                {
                                    sw.WriteLine(new GlobalWCFServiceRewriter(edmxName).Visit(Syntax.ParseCompilationUnit(globalWCFServiceContent)).NormalizeWhitespace().ToString());
                                }
                            }
                        }
                        else
                        {
                            addGlobalService = true;
                            Directory.CreateDirectory(globalDirectoryPath);
                            using (var sw = new StreamWriter(Path.Combine(globalDirectoryPath, "GlobalWCFService.cs")))
                            {
                                sw.WriteLine(string.Concat(
    @"using System.Transactions;

    namespace ", rootNamespace, @"
    {
        public partial class GlobalWCFService : IGlobalWCFService
        {
            public GlobalSerializedContexts SaveChanges(GlobalSerializedContexts clientContexts)
            {
                using (var transaction = new TransactionScope())
                {
                    ", edmxName, @"SaveChanges(clientContexts);
                    transaction.Complete();
                    return clientContexts;
                }
            }
        }
    }"));
                            }
                            string globalTTFilePath = Path.Combine(globalDirectoryPath, "GlobalWCFServiceContract.tt");
                            if (!File.Exists(globalTTFilePath))
                                CopyTTFile(Path.Combine(toolsServerPath, "Server.Global.tt"), globalTTFilePath, sourceControl, slnTTIncludesPath, new[] { "$RootNamespace$", rootNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });

                            using (var sw = new StreamWriter(Path.Combine(projectDirectoryPath, "Global.svc")))
                            {
                                sw.WriteLine(string.Concat("<%@ ServiceHost Language=\"C#\" Service=\"", rootNamespace, ".GlobalWCFService\" %>"));
                            }
                        }
                    }
                }

                if (kind != "FrameworkOnly")
                {
                    csprojPath = args[argIndex++];
                    specificationsFolderPath = args[argIndex++];
                    dtoFolderPath = args[argIndex++];
                    if (kind != "GlobalOnly")
                    {
                        Directory.CreateDirectory(specificationsFolderPath);
                        Directory.CreateDirectory(dtoFolderPath);
                    }
                }

                if (kind == "All" || kind == "WithoutFramework" || kind == "GlobalOnly")
                {
                    if (appKind == "App")
                        WriteAppGlobal(edmxPath, edmxName, edmxProjectPath, projectDirectoryPath, toolsServerPath, rootNamespace, assemblyName, assemblyVersion, appConfigPath, addGlobalService);
                    else
                        WriteWebGlobal(edmxPath, edmxName, edmxProjectPath, projectDirectoryPath, toolsServerPath, rootNamespace, assemblyNetVersion, assemblyName, assemblyVersion, wcfServiceNamespace, appConfigPath, addGlobalService, isWCF);
                }

                if (!Directory.Exists(waqsDirectory))
                {
                    Directory.CreateDirectory(waqsDirectory);

                    string waqsToolsPath = null;
                    switch (kind)
                    {
                        case "All":
                            waqsToolsPath = Path.Combine(toolsServerPath, "Server.waqs");
                            break;
                        case "WithoutGlobal":
                            waqsToolsPath = Path.Combine(toolsServerPath, "ServerWithoutGlobal.waqs");
                            break;
                        case "WithoutFramework":
                            waqsToolsPath = Path.Combine(toolsServerPath, "ServerWithoutFramework.waqs");
                            break;
                        case "WithoutGlobalWithoutFramework":
                            waqsToolsPath = Path.Combine(toolsServerPath, "ServerWithoutGlobalWithoutFramework.waqs");
                            break;
                        case "FrameworkOnly":
                            waqsToolsPath = Path.Combine(toolsServerPath, "ServerFrameworkOnly.waqs");
                            edmxName = "Framework";
                            break;
                        case "GlobalOnly":
                            waqsToolsPath = Path.Combine(toolsServerPath, "ServerGlobalOnly.waqs");
                            break;
                    }
                    string serverWAQS = Path.Combine(waqsDirectory, edmxName + ".Server.waqs");
                    if (!File.Exists(serverWAQS))
                    {
                        csprojPath = GetRelativePath(csprojPath, waqsDirectory);
                        var slnRelativeFilePath = GetRelativePath(slnFilePath, waqsDirectory);
                        specificationsFolderPath = GetRelativePath(specificationsFolderPath, waqsDirectory);
                        dtoFolderPath = GetRelativePath(dtoFolderPath, waqsDirectory);
                        CopyFile(waqsToolsPath, serverWAQS, new[] { "$edmxPath$", edmxPath == null ? null : edmxPath.Contains(":") ? GetRelativePath(edmxPath, waqsDirectory) : "..\\" + edmxPath }, new[] { "$DALInterfacesNamespace$", dalInterfacesNamespace }, new[] { "$DALNamespace$", dalNamespace }, new[] { "$ServiceInterfacesNamespace$", serviceInterfacesNamespace }, new[] { "$ServiceNamespace$", serviceNamespace }, new[] { "$WCFServiceContractNamespace$", wcfServiceContractNamespace }, new[] { "$WCFServiceNamespace$", wcfServiceNamespace }, new[] { "$SpecificationsSlnFilePath$", slnRelativeFilePath }, new[] { "$SpecificationsCsprojPath$", csprojPath }, new[] { "$SpecificationsFolderPath$", specificationsFolderPath + "\\" }, new[] { "$DTOSlnFilePath$", slnRelativeFilePath }, new[] { "$DTOCsprojPath$", csprojPath }, new[] { "$DTOFolderPath$", dtoFolderPath + "\\" }, new[] { "$EntitiesSlnFilePath$", slnRelativeFilePath }, new[] { "$EntitiesCsprojPath$", csprojPath }, new[] { "$EntitiesFolderPath$", "." });
                    }

                    string serverTT = Path.Combine(waqsDirectory, edmxName + ".Server.tt");
                    if (!File.Exists(serverTT))
                        CopyTTFile(Path.Combine(toolsServerPath, "Server.tt"), serverTT, sourceControl, slnTTIncludesPath, new[] { "$edmxName$", edmxName }, new[] { "$RootNamespace$", rootNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });

                    if (sourceControl)
                    {
                        string ttIncludesSourcePath = Path.Combine(Path.GetDirectoryName(toolsServerPath), "ttincludes");
                        string slnLocalTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "ServerTemplates");
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
            foreach (var ttInclude in Directory.GetFiles(ttIncludesSourcePath).Where(i => Path.GetFileName(i).StartsWith("WAQS.")))
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

        private static void WriteWebGlobal(string edmxPath, string edmxName, string edmxProjectPath, string projectDirectoryPath, string toolsPath, string rootNamespace, string assemblyNetVersion, string assemblyName, string assemblyVersion, string wcfServiceNamespace, string templateWebConfigPath, bool addGlobalService, bool isWCF)
        {
            if (wcfServiceNamespace == null)
                wcfServiceNamespace = rootNamespace + ".WCFService";
            string webConfigFilePath = Path.Combine(projectDirectoryPath, "Web.config");
            if (isWCF)
            {
                string globalAsaxFilePath = Path.Combine(projectDirectoryPath, "Global.asax");
                string globalAsaxCsFilePath = Path.Combine(projectDirectoryPath, "Global.asax.cs");
                if (!File.Exists(globalAsaxFilePath))
                    CopyFile(Path.Combine(toolsPath, "Global.asax"), globalAsaxFilePath, new[] { "$RootNamespace$", rootNamespace });

                string globalAsaxCsModelPath = Path.Combine(toolsPath, "Global.asax.cs");
                if (!File.Exists(globalAsaxCsFilePath))
                    CopyFile(globalAsaxCsModelPath, globalAsaxCsFilePath, new[] { "$RootNamespace$", rootNamespace });

                CopyFile(Path.Combine(toolsPath, "server.svc"), Path.Combine(projectDirectoryPath, edmxName + ".svc"), new[] { "$WCFServiceNamespace$", wcfServiceNamespace }, new[] { "$edmxName$", edmxName });

                string globalAsaxCsContent;
                using (var sr = new StreamReader(globalAsaxCsFilePath))
                {
                    globalAsaxCsContent = new ApplicationStartRewriter(edmxName, globalAsaxCsModelPath).Visit(Syntax.ParseCompilationUnit(sr.ReadToEnd())).NormalizeWhitespace().ToString();
                }
                using (var sw = new StreamWriter(globalAsaxCsFilePath))
                {
                    sw.Write(globalAsaxCsContent);
                }
            }

            if (!File.Exists(webConfigFilePath))
                CopyFile(Path.Combine(toolsPath, "Web.config"), webConfigFilePath, new[] { "$AssemblyNETVersion$", assemblyNetVersion }, new[] { "$AssemblyName$", assemblyName }, new[] { "$AssemblyVersion$", assemblyVersion });

            XElement webConfig = XElement.Load(webConfigFilePath);
            XElement connectionStrings = webConfig.Element("connectionStrings");
            string edmxFolderPath = Path.GetDirectoryName(edmxPath);
            string edmxProjectFolderPath = Path.GetDirectoryName(edmxProjectPath);
            string folderDiff = edmxFolderPath.Substring(edmxProjectFolderPath.Length + (edmxFolderPath.Length > edmxProjectFolderPath.Length ? 1 : 0));
            if (connectionStrings == null)
            {
                if (templateWebConfigPath != null)
                {
                    XElement templateConfig = XElement.Load(templateWebConfigPath);
                    XElement appConnectionString = templateConfig.Element("connectionStrings").Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                    appConnectionString.Attribute("connectionString").Value = Regex.Replace(appConnectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
                    webConfig.Add(
                        new XElement("connectionStrings", appConnectionString));
                }
            }
            else
            {
                XElement connectionString = connectionStrings.Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                if (connectionString == null)
                {
                    if (templateWebConfigPath != null)
                    {
                        XElement templateConfig = XElement.Load(templateWebConfigPath);
                        XElement appConnectionString = templateConfig.Element("connectionStrings").Elements("add").FirstOrDefault(add => add.Attribute("connectionString").Value.Contains(edmxName + ".csdl"));
                        appConnectionString.Attribute("connectionString").Value = Regex.Replace(appConnectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
                        connectionStrings.Add(appConnectionString);
                    }
                }
                else
                    connectionString.Attribute("connectionString").Value = Regex.Replace(connectionString.Attribute("connectionString").Value, string.Concat(@"(?:[^\|\/]+\.)?", edmxName, "(.(?:csdl|ssdl|msl))"), m => string.Concat("WAQS.", edmxName, ".", edmxName, m.Groups[1].Value));
            }

            if (isWCF)
            {
                XElement serviceModel = webConfig.Element("system.serviceModel");
                if (serviceModel == null)
                {
                    webConfig.Add(serviceModel = XElement.Parse(XElement.Load(Path.Combine(toolsPath, "Web.config")).Element("system.serviceModel").ToString().Replace("$AssemblyName$", assemblyName).Replace("$AssemblyVersion$", assemblyVersion)));
                }

                string service = string.Format("<service behaviorConfiguration=\"WAQSServiceBehavior\" name=\"{0}.{1}WCFService\"><endpoint behaviorConfiguration=\"WAQSEndpointBehavior\" address=\"\" binding=\"customBinding\" bindingConfiguration=\"HttpBinaryBinding\" contract=\"{0}.Contract.I{1}WCFService\"/></service>", wcfServiceNamespace, edmxName);

                var servicesElement = serviceModel.Element("services");
                servicesElement.Add(XElement.Parse(service));

                if (addGlobalService)
                {
                    string globalService = string.Format("<service behaviorConfiguration=\"WAQSServiceBehavior\" name=\"{0}.GlobalWCFService\"><endpoint behaviorConfiguration=\"WAQSEndpointBehavior\" address=\"\" binding=\"customBinding\" bindingConfiguration=\"HttpBinaryBinding\" contract=\"{0}.IGlobalWCFService\"/></service>", rootNamespace);

                    servicesElement.Add(XElement.Parse(globalService));
                }
            }

            webConfig.Save(webConfigFilePath);
        }

        private static void WriteAppGlobal(string edmxPath, string edmxName, string edmxProjectPath, string projectDirectoryPath, string toolsPath, string rootNamespace, string assemblyName, string assemblyVersion, string templateConfigPath, bool addGlobalService)
        {
            string appConfigFilePath = Path.Combine(projectDirectoryPath, "App.config");

            if (!File.Exists(appConfigFilePath))
                CopyFile(Path.Combine(toolsPath, "App.config"), appConfigFilePath, new[] { "$AssemblyName$", assemblyName }, new[] { "$AssemblyVersion$", assemblyVersion });

            XElement appConfig = XElement.Load(appConfigFilePath);
            XElement connectionStrings = appConfig.Element("connectionStrings");
            string edmxFolderPath = Path.GetDirectoryName(edmxPath);
            string edmxProjectFolderPath = Path.GetDirectoryName(edmxProjectPath);
            string folderDiff = edmxFolderPath.Substring(edmxProjectFolderPath.Length + (edmxFolderPath.Length > edmxProjectFolderPath.Length ? 1 : 0));
            if (connectionStrings == null)
            {
                if (templateConfigPath != null)
                {
                    XElement templateConfig = XElement.Load(templateConfigPath);
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
                    if (templateConfigPath != null)
                    {
                        XElement templateConfig = XElement.Load(templateConfigPath);
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
                value = Regex.Replace(value, "WriteServer\\(((?:\\s*\"[^\"]+\"\\s*,?)+)\\)\\s*;", m => string.Concat("WriteServer(", m.Groups[1].Value, ", @\"", slnDirectoryRelative + "\\\");"));
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

        public static string wcfServiceContract { get; set; }

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
