using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VSLangProj;

namespace WAQS
{
    /// <summary>
    /// Interaction logic for Wizard.xaml
    /// </summary>
    public partial class ServerWizard : Window
    {
        private EnvDTE.DTE _dte;
        private EnvDTE.Project _project;
        private IVsUIShell _uiShell;
        private IVsPackageInstaller _packageInstaller;
        private IVsPackageInstallerServices _packageInstallerServices;

        public ServerWizard(EnvDTE.Project project, IVsUIShell uiShell, IVsPackageInstaller packageInstaller, IVsPackageInstallerServices packageInstallerServices)
        {
            InitializeComponent();

            _dte = project.DTE;
            _project = project;
            _uiShell = uiShell;
            _packageInstaller = packageInstaller;
            _packageInstallerServices = packageInstallerServices;

            var edmxs = _dte.GetSolutionEdmx(_project).ToList();
            edmx.ItemsSource = edmxs;
            edmx.SelectedItem = edmxs.FirstOrDefault();

            generationOptions.ItemsSource = new[] { GenerationOptions.GetViewModel(GenerationOptions.Kind.All), GenerationOptions.GetViewModel(GenerationOptions.Kind.WithoutGlobalWithoutFramework), GenerationOptions.GetViewModel(GenerationOptions.Kind.FrameworkOnly), GenerationOptions.GetViewModel(GenerationOptions.Kind.GlobalOnly) };
            generationOptions.SelectedIndex = edmxs.Count == 0 ? 2 : 0;

            if (! GenerationOptions.CanBeRunnedWithNoCopy(_dte))
            {
                copyTemplates.IsChecked = true;
                copyTemplates.IsEnabled = false;
            }
        }

        private int _previousEdmxIndex;
        private void GenerationOptionsSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Cast<GenerationOptions.KindViewModel>().Single().Kind == GenerationOptions.Kind.FrameworkOnly)
            {
                _previousEdmxIndex = edmx.SelectedIndex;
                edmx.SelectedItem = null;
                edmx.IsEnabled = false;
            }
            else if (e.RemovedItems.Cast<GenerationOptions.KindViewModel>().SingleOrDefault()?.Kind == GenerationOptions.Kind.FrameworkOnly)
            {
                edmx.SelectedIndex = _previousEdmxIndex;
                edmx.IsEnabled = true;
            }
            RefreshGenerationEnabled();
        }

        private void EdmxSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshGenerationEnabled();
        }

        private void RefreshGenerationEnabled()
        {
            generate.IsEnabled = !string.IsNullOrEmpty((string)edmx.SelectedValue) || (GenerationOptions.Kind)generationOptions.SelectedValue == GenerationOptions.Kind.FrameworkOnly;
        }

        private void GenerateClick(object sender, RoutedEventArgs e)
        {
            var originalCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                _packageInstaller.InstallPackage(_project, "WAQS.Server", _packageInstallerServices);

                var edmxPath = edmx.SelectedValue as string;
                var appKind = _project.Properties.Cast<EnvDTE.Property>().Any(p => p.Name.StartsWith("WebApplication")) ? "Web" : "App";
                var netVersion = _project.GetNetVersion();
                var kind = (GenerationOptions.KindViewModel)generationOptions.SelectedItem;

                var projectDirectoryPath = Path.GetDirectoryName(_project.FullName);
                string waqsDirectory;
                string edmxName = null;
                EnvDTE.ProjectItem edmxProjectItem = null;
                string edmxProjectPath = null;
                if (kind.Kind == GenerationOptions.Kind.FrameworkOnly)
                {
                    waqsDirectory = Path.Combine(projectDirectoryPath, "WAQS.Framework");
                }
                else
                {
                    if (!(edmxPath.EndsWith(".edmx") && File.Exists(edmxPath)))
                    {
                        ShowError("Edmx path is not correct");
                        return;
                    }
                    edmxName = Path.GetFileNameWithoutExtension(edmxPath);
                    waqsDirectory = Path.Combine(projectDirectoryPath, "WAQS." + edmxName);
                    edmxProjectItem = _dte.Solution.FindProjectItem(edmxPath);
                    edmxProjectPath = edmxProjectItem.ContainingProject.FullName;
                }
                if (Directory.Exists(waqsDirectory))
                {
                    ShowError(waqsDirectory + "already exists");
                    return;
                }

                var projectUIHierarchyItems = _dte.GetProjectsUIHierarchyItems().First(uihi => ((EnvDTE.Project)uihi.Object).FullName == _project.FullName).UIHierarchyItems;
                var referencesUIHierarchyItems = projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().First(uihi => uihi.Name == "References").UIHierarchyItems;
                var referencesExpanded = referencesUIHierarchyItems.Expanded;

                var toolsPath = Path.Combine(_packageInstallerServices.GetPackageLocation("WAQS.Server"), "tools");
                var toolsPathServer = Path.Combine(toolsPath, "Server");
                var defaultNamespace = _project.GetDefaultNamespace();
                EnvDTE.Project fxProject;
                if (kind.Kind == GenerationOptions.Kind.GlobalOnly || kind.Kind == GenerationOptions.Kind.WithoutGlobalWithoutFramework)
                {
                    fxProject = _dte.Solution.FindProjectItem("ExpressionExtension.cs").ContainingProject;
                }
                else
                {
                    fxProject = _project;
                }
                var assemblyName = (string)fxProject.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "AssemblyName").Value;
                var assemblyVersion = (string)fxProject.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "AssemblyVersion").Value;
                var references = ((VSProject)_project.Object).References;
                references.Add("System");
                references.Add("System.Configuration");
                references.Add("System.Core");
                references.Add("System.Data");
                references.Add("System.Runtime.Serialization");
                references.Add("System.ServiceModel");
                references.Add("System.ServiceModel.Activation");
                references.Add("System.ServiceModel.Channels");
                references.Add("System.Transactions");
                references.Add("System.Web");
                references.Add("System.Xml");
                _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "Unity", "3.0.1304.1", false);
                _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "CommonServiceLocator", "1.2.0", false);
                _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "EntityFramework", "6.1.2", false);
                try
                {
                    referencesUIHierarchyItems.Expanded = referencesExpanded;
                }
                catch
                {
                }
                bool withGlobal = (kind.Kind & GenerationOptions.Kind.GlobalOnly) != 0;
                var globalDirectory = Path.Combine(projectDirectoryPath, "Global");
                string webConfigPath = null;
                string globalAsaxPath = null;
                string globalAsaxCsPath = null;
                string globalWCFService = null;
                if (withGlobal)
                {
                    webConfigPath = Path.Combine(projectDirectoryPath, "Web.config");
                    if (File.Exists(webConfigPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(webConfigPath);
                        }
                        catch
                        {
                        }
                    }
                    globalAsaxPath = Path.Combine(projectDirectoryPath, "Global.asax");
                    if (File.Exists(globalAsaxPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(globalAsaxPath);
                        }
                        catch
                        {
                        }
                    }
                    globalAsaxCsPath = Path.Combine(projectDirectoryPath, "Global.asax.cs");
                    if (File.Exists(globalAsaxCsPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(globalAsaxCsPath);
                        }
                        catch
                        {
                        }
                    }
                    globalWCFService = Path.Combine(projectDirectoryPath, "GlobalWCFService.cs");
                    if (File.Exists(globalWCFService))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(globalWCFService);
                        }
                        catch
                        {
                        }
                    }
                }
                string vsVersion;
                switch (_dte.Version)
                {
                    case "12.0":
                        vsVersion = "VS12";
                        break;
                    case "14.0":
                    default:
                        vsVersion = "VS14";
                        break;
                }
                var exePath = Path.Combine(toolsPathServer, "InitWAQSServer.exe");
                string specificationsFolder = null;
                string dtoFolder = null;
                var exeArgs = new StringBuilder("\"" + edmxPath + "\" \"" + edmxProjectPath + "\" \"" + projectDirectoryPath + "\" \"" + toolsPathServer + "\" \"" + defaultNamespace + "\" \"" + assemblyName + "\" \"" + assemblyVersion + "\" \"" + netVersion + "\" \"" + vsVersion + "\" \"" + kind.Key + "\" \"" + appKind + "\" \"" + waqsDirectory + "\" \"" + (edmxPath == null ? "" : _dte.Solution.FindProjectItem(edmxPath).ContainingProject.ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name == "App.Config")?.GetFilePath()) + "\" " + (copyTemplates.IsChecked == true ? "WithSourceControl" : "WithoutSourceControl") + " \"" + _dte.Solution.FullName + "\" WCF");
                if (kind.Kind == GenerationOptions.Kind.GlobalOnly)
                {
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem(edmxName + ".Server.DAL.Interfaces.tt").GetFirstCsFilePath() + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem(edmxName + ".Server.DAL.tt").GetFirstCsFilePath() + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem("I" + edmxName + "Service.cs").GetFilePath() + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem(edmxName + "Service.cs").GetFilePath() + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem("I" + edmxName + "WCFService.cs").GetFilePath() + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem(edmxName + "WCFService.cs").GetFilePath() + "\"");
                    exeArgs.Append(" \"" + edmxProjectPath + "\"");
                    var edmxProjectFolderPath = Path.GetDirectoryName(edmxProjectPath);
                    exeArgs.Append(" \"" + Path.Combine(edmxProjectFolderPath, "Specifications") + "\"");
                    exeArgs.Append(" \"" + Path.Combine(edmxProjectFolderPath, "DTO") + "\"");
                }
                else if (kind.Kind != GenerationOptions.Kind.FrameworkOnly)
                {
                    exeArgs.Append(" \"" + _project.FullName + "\"");
                    specificationsFolder = Path.Combine(projectDirectoryPath, "Specifications");
                    exeArgs.Append(" \"" + specificationsFolder + "\"");
                    dtoFolder = Path.Combine(projectDirectoryPath, "DTO");
                    exeArgs.Append(" \"" + dtoFolder + "\"");
                }
                var process = new Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = exeArgs.ToString();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();

                if (copyTemplates.IsChecked == true)
                {
                    string templatesFolder;
                    HashSet<string> existingTTIncludes;
                    EnvDTE.ProjectItems templatesProjectItems;
                    TemplatesCopying.CopyTemplates(_dte, "ServerTemplates", netVersion, toolsPath, vsVersion, out templatesFolder, out existingTTIncludes, out templatesProjectItems);
                    var ttInclude = @"%AppData%\WAQS\Templates\Includes\WAQS.Roslyn.Assemblies.ttinclude";
                    var ttIncludeName = Path.GetFileName(ttInclude);
                    TemplatesCopying.AddItem(ttInclude, vsVersion, netVersion, ttIncludeName, templatesFolder, existingTTIncludes, templatesProjectItems);
                }

                if ((kind.Kind & GenerationOptions.Kind.WithoutGlobalWithoutFramework) != 0)
                {
                    var edmxFileProperties = edmxProjectItem.Properties;
                    edmxFileProperties.Item("CustomTool").Value = "";
                    edmxFileProperties.Item("BuildAction").Value = 0;
                    foreach (var ttPath in edmxProjectItem.ProjectItems.Cast<EnvDTE.ProjectItem>().Where(pi => pi.Name.EndsWith(".tt")).Select(pi => pi.GetFilePath()))
                    {
                        _dte.Solution.FindProjectItem(ttPath).Delete();
                    }
                }
                if (kind.Kind == GenerationOptions.Kind.FrameworkOnly)
                {
                    edmxName = "Framework";
                }
                _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Server.waqs"));
                _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Server.tt"));
                var dalItem = _dte.Solution.FindProjectItem(Path.Combine(waqsDirectory, edmxName + ".Server.DAL.tt"));
                if (dalItem != null && dalItem.ProjectItems.Count < 2) // strange bug: sometimes code is not generated for this T4 only
                {
                    ((VSProjectItem)dalItem.Object).RunCustomTool();
                }
                try
                {
                    projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().First(uihi => uihi.Name == "WAQS." + edmxName).UIHierarchyItems.Expanded = false;
                }
                catch
                {
                }
                if (withGlobal && appKind == "Web")
                {
                    _project.ProjectItems.AddFromFile(webConfigPath);
                    _project.ProjectItems.AddFromFile(globalAsaxPath);
                    _project.ProjectItems.AddFromFile(globalAsaxCsPath);
                    _project.ProjectItems.AddFromFile(Path.Combine(projectDirectoryPath, edmxName + ".svc"));
                    if (kind.Kind == GenerationOptions.Kind.GlobalOnly)
                    {
                        var globalUIHierarchyItem = projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().FirstOrDefault(uihi => uihi.Name == "Global");
                        EnvDTE.UIHierarchyItems globalUIHierarchyItems = null;
                        bool globalUIHierarchyItemsExpanded = false;
                        if (globalUIHierarchyItem != null)
                        {
                            globalUIHierarchyItems = globalUIHierarchyItem.UIHierarchyItems;
                            globalUIHierarchyItemsExpanded = globalUIHierarchyItems.Expanded;
                        }
                        _project.ProjectItems.AddFromFile(Path.Combine(globalDirectory, "GlobalWCFServiceContract.tt"));
                        _project.ProjectItems.AddFromFile(Path.Combine(globalDirectory, "GlobalWCFService.cs"));
                        _project.ProjectItems.AddFromFile(Path.Combine(projectDirectoryPath, "Global.svc"));
                        if (globalUIHierarchyItem == null)
                        {
                            try
                            {
                                globalUIHierarchyItems = projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().FirstOrDefault(uihi => uihi.Name == "Global")?.UIHierarchyItems;
                            }
                            catch
                            {
                            }
                        }
                        if (globalUIHierarchyItems != null)
                        {
                            globalUIHierarchyItems.Expanded = globalUIHierarchyItemsExpanded;
                        }
                    }
                }
                if (specificationsFolder != null)
                {
                    _project.ProjectItems.AddFromDirectory(specificationsFolder);
                }
                if (dtoFolder != null)
                {
                    _project.ProjectItems.AddFromDirectory(dtoFolder);
                }
                try
                {
                    _dte.ExecuteCommand("File.TfsRefreshStatus");
                }
                catch
                {
                }
                _dte.ItemOperations.Navigate("https://waqs.codeplex.com/documentation");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().ToString() + "\r\n" + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
                Cursor = originalCursor;
            }
        }

        private void ShowError(string message)
        {
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(_uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "WAQS Generation",
                       message,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_CRITICAL,
                       0,
                       out result));
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
