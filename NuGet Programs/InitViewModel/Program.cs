using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Xml.Linq;

namespace InitViewModel
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string edmxName = args[0];
                string rootNamespace = args[1];
                string filePath = args[2];
                string waqsFilePath = args[3];
                string xamlCsFilePath = args.Length > 4 ? args[4] : null;
                if (string.IsNullOrEmpty(xamlCsFilePath))
                    xamlCsFilePath = null;
                else
                    xamlCsFilePath = xamlCsFilePath + ".cs";

                string content;
                using (var sr = new StreamReader(filePath))
                {
                    content = sr.ReadToEnd();
                }
                XElement waqs = XElement.Load(waqsFilePath);
                XElement entities = waqs.Element("Entities");
                XAttribute entitiesAttribute = entities.Attribute("NamespaceName");
                string entitiesNamespace = entitiesAttribute == null ? rootNamespace : entitiesAttribute.Value;
                XElement clientContext = waqs.Element("ClientContext");
                XAttribute clientContextAttribute = clientContext.Attribute("NamespaceName");
                string clientContextNamespace = clientContextAttribute == null ? rootNamespace + ".ClientContext" : clientContextAttribute.Value;
                XElement clientContextInterfaces = waqs.Element("ClientContextInterfaces");
                XAttribute clientContextInterfacesAttribute = clientContextInterfaces.Attribute("NamespaceName");
                string clientContextInterfacesNamespace = clientContextInterfacesAttribute == null ? rootNamespace + ".ClientContext.Interfaces" : clientContextInterfacesAttribute.Value;
                XElement framework = waqs.Element("Framework");
                XElement waqsClientContext = framework.Element("ClientContext");
                XAttribute waqsClientContextAttribute = waqsClientContext.Attribute("NamespaceName");
                string waqsClientContextNamespace = waqsClientContextAttribute == null ? "WAQS.ClientContext" : waqsClientContextAttribute.Value;
                XElement waqsClientContextInterfaces = framework.Element("ClientContextInterfaces");
                XAttribute waqsClientContextInterfacesAttribute = waqsClientContextInterfaces.Attribute("NamespaceName");
                string waqsClientContextInterfacesNamespace = waqsClientContextInterfacesAttribute == null ? "WAQS.ClientContext.Interfaces" : waqsClientContextInterfacesAttribute.Value;
                XElement waqsComponentModel = framework.Element("ComponentModel");
                XAttribute waqsComponentModelAttribute = waqsComponentModel.Attribute("NamespaceName");
                string waqsComponentModelNamespace = waqsComponentModelAttribute == null ? "WAQS.ComponentModel" : waqsComponentModelAttribute.Value;
                var viewModelRewriter = new ViewModelRewriter(edmxName, entitiesNamespace, clientContextNamespace, clientContextInterfacesNamespace, waqsClientContextNamespace, waqsClientContextInterfacesNamespace, waqsComponentModelNamespace);
                content = viewModelRewriter.Visit(SyntaxFactory.ParseCompilationUnit(content)).NormalizeWhitespace().ToString();
                using (var sw = new StreamWriter(filePath))
                {
                    sw.Write(content);
                }

                string viewModelTypeName;
                if (!(xamlCsFilePath == null || string.IsNullOrEmpty(viewModelTypeName = viewModelRewriter.TypeName)))
                {
                    string viewContent;
                    using (var sr = new StreamReader(xamlCsFilePath))
                    {
                        viewContent = sr.ReadToEnd();
                    }
                    string viewModelNamespaceName = viewModelRewriter.NamespaceName;
                    if (!string.IsNullOrEmpty(viewModelNamespaceName))
                        viewModelNamespaceName += ".";
                    viewContent = new ViewRewriter((viewModelNamespaceName ?? "") + viewModelTypeName).Visit(SyntaxFactory.ParseCompilationUnit(viewContent)).NormalizeWhitespace().ToString();
                    using (var sw = new StreamWriter(xamlCsFilePath))
                    {
                        sw.Write(viewContent);
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
    }
}
