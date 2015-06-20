using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VSLangProj;

namespace WAQS
{
    /// <summary>
    /// Interaction logic for ClientWizard.xaml
    /// </summary>
    public partial class ClientWizard
    {
        private string _clientKind;
        private EnvDTE.DTE _dte;
        private EnvDTE.Project _project;
        private IVsUIShell _uiShell;
        private IVsPackageInstaller _packageInstaller;
        private IVsPackageInstallerServices _packageInstallerServices;

        public ClientWizard(string clientKind, EnvDTE.Project project, IVsUIShell uiShell, IVsPackageInstaller packageInstaller, IVsPackageInstallerServices packageInstallerServices)
        {
            InitializeComponent();

            _clientKind = clientKind;
            _dte = project.DTE;
            _project = project;
            _uiShell = uiShell;
            _packageInstaller = packageInstaller;
            _packageInstallerServices = packageInstallerServices;

            var edmxs = _dte.GetSolutionEdmx(_project).ToList();
            edmx.ItemsSource = edmxs;
            edmx.SelectedItem = edmxs.FirstOrDefault();

            var services = _dte.GetSolutionSvc(_project).ToList();
            service.ItemsSource = services;
            service.SelectedItem = services.FirstOrDefault();

            generationOptions.ItemsSource = new[] { GenerationOptions.GetViewModel(GenerationOptions.Kind.All), GenerationOptions.GetViewModel(GenerationOptions.Kind.WithoutGlobalWithoutFramework), GenerationOptions.GetViewModel(GenerationOptions.Kind.FrameworkOnly), GenerationOptions.GetViewModel(GenerationOptions.Kind.GlobalOnly) };
            generationOptions.SelectedIndex = edmxs.Count == 0 || services.Count == 0 ? 2 : 0;

            if (!GenerationOptions.CanBeRunnedWithNoCopy(_dte))
            {
                copyTemplates.IsChecked = true;
                copyTemplates.IsEnabled = false;
            }
        }

        private int _previousEdmxIndex;
        private int _previousServiceIndex;
        private void GenerationOptionsSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Cast<GenerationOptions.KindViewModel>().Single().Kind == GenerationOptions.Kind.FrameworkOnly)
            {
                _previousEdmxIndex = edmx.SelectedIndex;
                edmx.SelectedItem = null;
                edmx.IsEnabled = false;
                _previousServiceIndex = service.SelectedIndex;
                service.SelectedItem = null;
                service.IsEnabled = false;
            }
            else if (e.RemovedItems.Cast<GenerationOptions.KindViewModel>().SingleOrDefault()?.Kind == GenerationOptions.Kind.FrameworkOnly)
            {
                edmx.SelectedIndex = _previousEdmxIndex;
                edmx.IsEnabled = true;
                service.SelectedIndex = _previousServiceIndex;
                service.IsEnabled = true;
            }
            RefreshGenerationEnabled();
        }

        private void EdmxSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshGenerationEnabled();
        }

        private void ServiceSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshGenerationEnabled();
        }

        private void RefreshGenerationEnabled()
        {
            generate.IsEnabled = !(string.IsNullOrEmpty((string)edmx.SelectedValue) || string.IsNullOrEmpty((string)service.SelectedValue)) || (generationOptions.SelectedValue as GenerationOptions.Kind?) == GenerationOptions.Kind.FrameworkOnly;
        }

        private void GenerateClick(object sender, RoutedEventArgs e)
        {
            var originalCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                _packageInstaller.InstallPackage(_project, "WAQS.Client." + _clientKind, _packageInstallerServices);

                var edmxPath = edmx.SelectedValue as string;
                var servicePath = service.SelectedValue as string;
                var appKind = _project.Properties.Cast<EnvDTE.Property>().Any(p => p.Name.StartsWith("WebApplication")) ? "Web" : "App";
                var netVersion = _project.GetNetVersion();
                var kind = (GenerationOptions.KindViewModel)generationOptions.SelectedItem;

                var projectDirectoryPath = Path.GetDirectoryName(_project.FullName);
                string waqsDirectory;
                string edmxName = null;
                string waqsGeneralDirectory = null;
                string contextsPath = null;
                bool servicePathIsUrl = false;
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
                    if (!((servicePathIsUrl = Regex.IsMatch(servicePath, "^http(s)?://")) || servicePath.EndsWith(".svc") && File.Exists(servicePath)))
                    {
                        ShowError("Service path is not correct");
                        return;
                    }
                    edmxName = Path.GetFileNameWithoutExtension(edmxPath);
                    waqsDirectory = Path.Combine(projectDirectoryPath, "WAQS." + edmxName);
                }
                if (Directory.Exists(waqsDirectory))
                {
                    ShowError(waqsDirectory + "already exists");
                    return;
                }

                var projectUIHierarchyItems = _dte.GetProjectsUIHierarchyItems().First(uihi => ((EnvDTE.Project)uihi.Object).FullName == _project.FullName).UIHierarchyItems;
                var referencesUIHierarchyItems = projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().FirstOrDefault(uihi => uihi.Name == "References")?.UIHierarchyItems;
                var referencesExpanded = referencesUIHierarchyItems?.Expanded ?? false;

                var toolsPath = Path.Combine(_packageInstallerServices.GetPackageLocation("WAQS.Client." + _clientKind), "tools");
                var clientToolsPath = Path.Combine(toolsPath, "Client." + _clientKind);
                var defaultNamespace = _project.GetDefaultNamespace();
                var references = ((VSProject)_project.Object).References;
                references.Add("System");
                references.Add("System.Core");
                references.Add("System.Runtime.Serialization");
                references.Add("System.ServiceModel");
                if (_clientKind == GenerationOptions.WPF)
                {
                    references.Add("System.ComponentModel.DataAnnotations");
                    references.Add("System.Drawing");
                    references.Add("PresentationCore");
                    references.Add("PresentationFramework");
                    references.Add("System.Xaml");
                    references.Add("System.Xml");
                    _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "System.Windows.Interactivity.WPF", "2.0.20525", false);
                    _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "Unity", "3.5.1404", false);
                    _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "Rx-WPF", "2.2.5", false);
                }
                else if (_clientKind == GenerationOptions.PCL)
                {
                    _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "Microsoft.Bcl.Async", "1.0.168", false);
                }

                try
                {
                    referencesUIHierarchyItems.Expanded = referencesExpanded;
                }
                catch
                {
                }
                bool withGlobal = (kind.Kind & GenerationOptions.Kind.GlobalOnly) != 0;
                string appConfigPath = null;
                string appXamlPath = null;
                string appXamlCsPath = null;
                if (withGlobal)
                {
                    appConfigPath = Path.Combine(projectDirectoryPath, "app.config");
                    if (File.Exists(appConfigPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(appConfigPath);
                        }
                        catch
                        {
                        }
                    }
                    appXamlPath = Path.Combine(projectDirectoryPath, "App.xaml");
                    if (File.Exists(appXamlPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(appXamlPath);
                        }
                        catch
                        {
                        }
                    }
                    appXamlCsPath = Path.Combine(projectDirectoryPath, "App.xaml.cs");
                    if (File.Exists(appXamlCsPath))
                    {
                        try
                        {
                            _dte.SourceControl.CheckOutItem(appXamlCsPath);
                        }
                        catch
                        {
                        }
                    }
                    if (kind.Kind == GenerationOptions.Kind.GlobalOnly)
                    {
                        waqsGeneralDirectory = Path.Combine(projectDirectoryPath, "WAQS");
                        contextsPath = Path.Combine(waqsGeneralDirectory, "Contexts.xml");
                        if (File.Exists(contextsPath))
                        {
                            try
                            {
                                _dte.SourceControl.CheckOutItem(contextsPath);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                var entitiesProjectPath = _dte.Solution.FindProjectItem(edmxName + ".Server.Entities.tt")?.ContainingProject.FullName;
                string entitiesSolutionPath = null;
                if (!string.IsNullOrEmpty(entitiesProjectPath))
                {
                    entitiesSolutionPath = _dte.Solution.FileName;
                }
                string vsVersion = _dte.GetVsVersion();

                string svcUrl = null;
                if (servicePathIsUrl)
                {
                    svcUrl = servicePath;
                }
                else if (!string.IsNullOrEmpty(servicePath))
                {
                    var svcProjectProperties = _dte.Solution.FindProjectItem(servicePath)?.ContainingProject.Properties.Cast<EnvDTE.Property>();
                    if (svcProjectProperties != null && (kind.Kind != GenerationOptions.Kind.FrameworkOnly))
                    {
                        svcUrl = StartService(servicePath, vsVersion, svcProjectProperties);
                    }
                }

                var exePath = Path.Combine(clientToolsPath, "InitWAQSClient" + _clientKind + ".exe");
                var exeArgs = new StringBuilder("\"" + edmxPath + "\" \"" + (_clientKind == GenerationOptions.WPF ? projectDirectoryPath + "\" \"" : "") + clientToolsPath + "\" \"" + defaultNamespace + "\" \"" + svcUrl + "\" \"" + waqsDirectory + "\" \"" + waqsGeneralDirectory + "\" \"" + (kind.Kind == GenerationOptions.Kind.GlobalOnly ? _dte.Solution.FindProjectItem(edmxName + ".Client." + _clientKind + ".ClientContext.tt").ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name == edmxName + "ExpressionTransformer.cs")?.GetFilePath() : "") + "\" \"" + (kind.Kind == GenerationOptions.Kind.GlobalOnly ? _dte.Solution.FindProjectItem(edmxName + ".Client." + _clientKind + ".ServiceProxy.tt").ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name == "I" + edmxName + "Service.cs")?.GetFilePath() : "") + "\" \"" + (kind.Kind == GenerationOptions.Kind.GlobalOnly ? _dte.Solution.FindProjectItem(edmxName + ".Client." + _clientKind + ".Entities.tt")?.GetFirstCsFilePath() : "") + "\" \"" + (kind.Kind == GenerationOptions.Kind.GlobalOnly ? _dte.Solution.FindProjectItem(edmxName + ".Client." + _clientKind + ".ClientContext.tt").ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name == edmxName + "ClientContext.cs")?.GetFilePath() : "") + "\" \"" + (kind.Kind == GenerationOptions.Kind.GlobalOnly ? _dte.Solution.FindProjectItem(edmxName + ".Client." + _clientKind + ".ClientContext.Interfaces.tt").ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name == "I" + edmxName + "ClientContext.cs")?.GetFilePath() : "") + "\" \"" + entitiesSolutionPath + "\" \"" + entitiesProjectPath + "\" \"" + netVersion + "\" \"" + vsVersion + "\" \"" + kind.Key + "\" " + (copyTemplates.IsChecked == true ? "WithSourceControl" : "WithoutSourceControl") + " \"" + _dte.Solution.FullName + "\"");
                if ((kind.Kind & GenerationOptions.Kind.WithoutGlobalWithoutFramework) != 0)
                {
                    var projectsItems = _dte.GetProjects().SelectMany(p => p.GetAllProjectItems());
                    var specificationsProjectItem = projectsItems.FirstOrDefault(pi => ((string)pi.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "FullPath").Value).EndsWith("\\Specifications\\"));
                    exeArgs.Append(" \"" + specificationsProjectItem.ContainingProject.FullName + "\"");
                    exeArgs.Append(" \"" + Path.GetDirectoryName(specificationsProjectItem.GetFilePath()) + "\"");
                    var dtoProjectItem = projectsItems.FirstOrDefault(pi => ((string)pi.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "FullPath").Value).EndsWith("\\DTO\\"));
                    exeArgs.Append(" \"" + dtoProjectItem.ContainingProject.FullName + "\"");
                    exeArgs.Append(" \"" + Path.GetDirectoryName(dtoProjectItem.GetFilePath()) + "\"");
                    exeArgs.Append(" \"" + _dte.Solution.FindProjectItem(edmxName + ".Server.Entities.tt")?.GetFirstCsFilePath() + "\"");
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
                    TemplatesCopying.CopyTemplates(_dte, _clientKind + "ClientTemplates", netVersion, toolsPath, vsVersion, out templatesFolder, out existingTTIncludes, out templatesProjectItems);
                    var ttInclude = @"%AppData%\WAQS\Templates\Includes\WAQS.Roslyn.Assemblies.ttinclude";
                    var ttIncludeName = Path.GetFileName(ttInclude);
                    TemplatesCopying.AddItem(ttInclude, vsVersion, netVersion, ttIncludeName, templatesFolder, existingTTIncludes, templatesProjectItems);
                }

                if (kind.Kind == GenerationOptions.Kind.FrameworkOnly)
                {
                    edmxName = "Framework";
                }
                _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Client." + _clientKind + ".waqs"));
                if (kind.Kind != GenerationOptions.Kind.GlobalOnly)
                {
                    _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Client." + _clientKind + ".tt"));
                }
                try
                {
                    projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().First(uihi => uihi.Name == "WAQS." + edmxName).UIHierarchyItems.Expanded = false;
                }
                catch
                {
                }
                if (withGlobal)
                {
                    if (_clientKind == GenerationOptions.WPF)
                    {
                        _project.ProjectItems.AddFromFile(appConfigPath);
                    }
                    if (kind.Kind == GenerationOptions.Kind.GlobalOnly)
                    {
                        EnvDTE.UIHierarchyItems waqsGeneralUIHierarchyItems = null;
                        Action setWAQSGeneralUIHierarchyItems = () => waqsGeneralUIHierarchyItems = projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().FirstOrDefault(uihi => uihi.Name == "WAQS")?.UIHierarchyItems;
                        setWAQSGeneralUIHierarchyItems();
                        bool waqsGeneralUIHierarchyItemsExpanded = false;
                        if (waqsGeneralUIHierarchyItems != null)
                        {
                            waqsGeneralUIHierarchyItemsExpanded = waqsGeneralUIHierarchyItems.Expanded;
                        }
                        _project.ProjectItems.AddFromFile(contextsPath);
                        if (waqsGeneralUIHierarchyItems == null)
                        {
                            try
                            {
                                setWAQSGeneralUIHierarchyItems();
                                waqsGeneralUIHierarchyItems.Expanded = false;
                            }
                            catch
                            {
                            }
                        }
                        else
                        {
                            try
                            {
                                waqsGeneralUIHierarchyItems.Expanded = waqsGeneralUIHierarchyItemsExpanded;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                try
                {
                    _dte.ExecuteCommand("File.TfsRefreshStatus");
                }
                catch
                {
                }
                _dte.ItemOperations.Navigate("https://github.com/MatthieuMEZIL/waqs/blob/master/README.md");
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

        private string StartService(string servicePath, string vsVersion, IEnumerable<EnvDTE.Property> svcProjectProperties)
        {
            _dte.Solution.SolutionBuild.Build(true);
            return DTEExtensions.StartService(servicePath, vsVersion, svcProjectProperties);
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
