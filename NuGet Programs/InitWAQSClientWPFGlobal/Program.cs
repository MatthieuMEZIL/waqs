using Roslyn.Compilers.CSharp;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InitWAQSClientWPFGlobal
{
    class Program
    {
        static void Main(string[] args)
        {
            int argIndex = 0;
            var toolsPath = args[argIndex++];
            var projectDirectoryPath = args[argIndex++];
            string netVersion = args[argIndex++];
            string vsVersion = args[argIndex++];
            var svcUrl = args[argIndex++];
            var contextsFilePath = args[argIndex++];
            var clientContextNamespace = args[argIndex++] + ".ClientContext";
            var appConfigFilePath = args[argIndex++];
            var appXamlCsFilePath = args[argIndex++];
            bool sourceControl = args[argIndex++] == "WithSourceControl";
            string slnFilePath = args[argIndex++];
            string slnTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "WPFClientTemplates");

            var waqsGlobalDirectory = Path.Combine(projectDirectoryPath, "WAQSGlobal");
            var wsdlUrl = svcUrl + "?wsdl";

            if (!Directory.Exists(waqsGlobalDirectory))
                Directory.CreateDirectory(waqsGlobalDirectory);

            using (var sw = new StreamWriter(Path.Combine(waqsGlobalDirectory, "Global.Client.WPF.waqs")))
            {
                sw.WriteLine(string.Concat(
@"<?xml version=", "\"1.0\" encoding=\"utf-8\" ?>", @"
<WAQS.Client>
  <ClientContext WSDL=", "\"", wsdlUrl, "\" Contexts=\"", GetRelativePath(contextsFilePath, waqsGlobalDirectory), "\"", @" />
  <ClientContextInterfaces />
  <Framework>
    <ClientContextInterfaces NamespaceName=", "\"WAQS.ClientContext.Interfaces\" />", @"
    <ClientContext NamespaceName=", "\"WAQS.ClientContext\" />", @"
  </Framework>
</WAQS.Client>"));
            }

            string globalTT = Path.Combine(waqsGlobalDirectory, "Global.Client.WPF.tt");
            if (!File.Exists(globalTT))
                CopyTTFile(Path.Combine(toolsPath, "Client.WPF.Global.tt"), globalTT, sourceControl, slnTTIncludesPath, new[] { "$ClientContextNamespace$", clientContextNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });

            if (! string.IsNullOrEmpty(appConfigFilePath))
            {
                XElement clientConfig = XElement.Load(appConfigFilePath);
                XElement serviceModel = clientConfig.Element("system.serviceModel");
                XElement bindings;
                XElement customBinding;
                if (serviceModel == null)
                    clientConfig.Add(serviceModel = new XElement("system.serviceModel", new XElement("bindings", customBinding = new XElement("customBinding"))));
                else if ((bindings = serviceModel.Element("bindings")) == null)
                    serviceModel.Add(new XElement("bindings", customBinding = new XElement("customBinding")));
                else if ((customBinding = bindings.Element("customBinding")) == null)
                    bindings.Add(customBinding = new XElement("customBinding"));

                customBinding.Add(
                    new XElement("binding",
                        new XAttribute("name", "CustomBinding_IGlobalWCFService"),
                        new XElement("binaryMessageEncoding",
                            new XAttribute("maxReadPoolSize", "2147483647"),
                            new XAttribute("maxWritePoolSize", "2147483647"),
                            new XAttribute("maxSessionSize", "2147483647"),
                            new XElement("readerQuotas",
                                new XAttribute("maxDepth", "2147483647"),
                                new XAttribute("maxStringContentLength", "2147483647"),
                                new XAttribute("maxArrayLength", "2147483647"),
                                new XAttribute("maxBytesPerRead", "2147483647"),
                                new XAttribute("maxNameTableCharCount", "2147483647"))),
                        new XElement("httpTransport",
                            new XAttribute("manualAddressing", "false"),
                            new XAttribute("maxBufferPoolSize", "2147483647"),
                            new XAttribute("maxReceivedMessageSize", "2147483647"),
                            new XAttribute("allowCookies", "false"),
                            new XAttribute("authenticationScheme", "Anonymous"),
                            new XAttribute("bypassProxyOnLocal", "false"),
                            new XAttribute("decompressionEnabled", "true"),
                            new XAttribute("hostNameComparisonMode", "StrongWildcard"),
                            new XAttribute("keepAliveEnabled", "true"),
                            new XAttribute("maxBufferSize", "2147483647"),
                            new XAttribute("proxyAuthenticationScheme", "Anonymous"),
                            new XAttribute("realm", ""),
                            new XAttribute("transferMode", "Buffered"),
                            new XAttribute("unsafeConnectionNtlmAuthentication", "false"),
                            new XAttribute("useDefaultWebProxy", "true"))));
                XElement client;
                if ((client = serviceModel.Element("client")) == null)
                    serviceModel.Add(client = new XElement("client"));
                client.Add(
                    new XElement("endpoint",
                        new XAttribute("address", svcUrl),
                        new XAttribute("binding", "customBinding"),
                        new XAttribute("bindingConfiguration", "CustomBinding_IGlobalWCFService"),
                        new XAttribute("contract", string.Format("{0}.IGlobalWCFService", clientContextNamespace + ".ServiceReference")),
                        new XAttribute("name", "CustomBinding_IGlobalWCFService")));
                clientConfig.Save(appConfigFilePath);
            }

            if (!string.IsNullOrEmpty(appXamlCsFilePath))
            {
                string appXamlCsContent;
                using (var sr = new StreamReader(appXamlCsFilePath))
                {
                    appXamlCsContent = sr.ReadToEnd();
                }
                appXamlCsContent = new ApplicationStartupRewriter(clientContextNamespace).Visit(Syntax.ParseCompilationUnit(appXamlCsContent)).NormalizeWhitespace().ToString();
                using (var sw = new StreamWriter(appXamlCsFilePath))
                {
                    sw.Write(appXamlCsContent);
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
    }
}
