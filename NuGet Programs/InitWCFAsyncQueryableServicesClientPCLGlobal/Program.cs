using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InitWCFAsyncQueryableServicesClientPCLGlobal
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
            var defaultNamespace = args[argIndex++] + ".ClientContext";
            bool sourceControl = args[argIndex++] == "WithSourceControl";
            string slnFilePath = args[argIndex++];
            string slnTTIncludesPath = Path.Combine(Path.GetDirectoryName(slnFilePath), "PCLClientTemplates");

            var waqsGlobalDirectory = Path.Combine(projectDirectoryPath, "WAQSGlobal");
            var wsdlUrl = svcUrl + "?wsdl";

            if (!Directory.Exists(waqsGlobalDirectory))
                Directory.CreateDirectory(waqsGlobalDirectory);

            using (var sw = new StreamWriter(Path.Combine(waqsGlobalDirectory, "Global.Client.PCL.waqs")))
            {
                sw.WriteLine(string.Concat(
@"<?xml version=", "\"1.0\" encoding=\"utf-8\" ?>", @"
<WCFAsyncQueryableServices.Client>
  <ClientContext WSDL=", "\"", wsdlUrl, "\" Contexts=\"", GetRelativePath(contextsFilePath, waqsGlobalDirectory), "\"", @" />
  <ClientContextInterfaces />
  <Framework>
    <ClientContextInterfaces NamespaceName=", "\"WCFAsyncQueryableServices.ClientContext.Interfaces\" />", @"
    <ClientContext NamespaceName=", "\"WCFAsyncQueryableServices.ClientContext\" />", @"
  </Framework>
</WCFAsyncQueryableServices.Client>"));
            }
            string globalTT = Path.Combine(waqsGlobalDirectory, "Global.Client.PCL.tt");
            if (!File.Exists(globalTT))
                CopyTTFile(Path.Combine(toolsPath, "Client.PCL.Global.tt"), globalTT, sourceControl, slnTTIncludesPath, new[] { "$ClientContextNamespace$", defaultNamespace }, new[] { "$NetVersion$", netVersion }, new[] { "$VSVersion$", vsVersion });
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
