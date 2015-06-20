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
    /// Interaction logic for ServerMockWizard.xaml
    /// </summary>
    public partial class ServerMockWizard
    {
        private EnvDTE.DTE _dte;
        private EnvDTE.Project _project;
        private IVsUIShell _uiShell;
        private IVsPackageInstaller _packageInstaller;
        private IVsPackageInstallerServices _packageInstallerServices;

        public ServerMockWizard(EnvDTE.Project project, IVsUIShell uiShell, IVsPackageInstaller packageInstaller, IVsPackageInstallerServices packageInstallerServices)
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

            generationOptions.ItemsSource = new[] { GenerationOptions.GetViewModel(GenerationOptions.Kind.All), GenerationOptions.GetViewModel(GenerationOptions.Kind.WithoutGlobalWithoutFramework), GenerationOptions.GetViewModel(GenerationOptions.Kind.FrameworkOnly) };
            generationOptions.SelectedIndex = edmxs.Count == 0 ? 2 : 0;

            if (!GenerationOptions.CanBeRunnedWithNoCopy(_dte))
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
                _packageInstaller.InstallPackage(_project, "WAQS.Server.Mock", _packageInstallerServices);

                var edmxPath = edmx.SelectedValue as string;
                var appKind = _project.Properties.Cast<EnvDTE.Property>().Any(p => p.Name.StartsWith("WebApplication")) ? "Web" : "App";
                var netVersion = _project.GetNetVersion();
                var kind = (GenerationOptions.KindViewModel)generationOptions.SelectedItem;

                var projectDirectoryPath = Path.GetDirectoryName(_project.FullName);
                string waqsDirectory;
                string waqsGeneralDirectory = null;
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

                var toolsPath = Path.Combine(_packageInstallerServices.GetPackageLocation("WAQS.Server.Mock"), "tools");
                var toolsPathServerMock = Path.Combine(toolsPath, "Server.Mock");
                var defaultNamespace = _project.GetDefaultNamespace();
                var references = ((VSProject)_project.Object).References;
                references.Add("System");
                references.Add("System.Core");
                references.Add("System.Data");
                references.Add("Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll");
                var dalInterfacesProjectItem = _dte.Solution.FindProjectItem(edmxName + ".Server.DAL.Interfaces.tt");
                var dalInterfacesProject = dalInterfacesProjectItem?.ContainingProject;
                if (dalInterfacesProject != null)
                {
                    references.AddProject(dalInterfacesProject);
                }
                var dalProjectItem = _dte.Solution.FindProjectItem(edmxName + ".Server.DAL.tt");
                var dalProject = dalProjectItem?.ContainingProject;
                if (dalProject != null)
                {
                    references.AddProject(dalProject);
                }
                var dtoProject = _dte.Solution.FindProjectItem(edmxName + ".Server.DTO.tt")?.ContainingProject;
                if (dtoProject != null)
                {
                    references.AddProject(dtoProject);
                }
                var entitiesProjectItem = _dte.Solution.FindProjectItem(edmxName + ".Server.Entities.tt");
                var entitiesProject = entitiesProjectItem?.ContainingProject;
                if (entitiesProject != null)
                {
                    references.AddProject(entitiesProject);
                }
                var serviceProject = _dte.Solution.FindProjectItem(edmxName + ".Server.Service.tt")?.ContainingProject;
                if (serviceProject != null)
                {
                    references.AddProject(serviceProject);
                }
                EnvDTE.Project fxProject = null;
                if (kind.Kind == GenerationOptions.Kind.WithoutGlobalWithoutFramework)
                {
                    fxProject = _dte.Solution.FindProjectItem("WAQS.Server.Fx.DAL.Mock.tt")?.ContainingProject;
                    if (fxProject != null)
                    {
                        references.AddProject(fxProject);
                    }
                }
                _packageInstaller.InstallPackage("http://packages.nuget.org", _project, "EntityFramework", "6.1.2", false);
                try
                {
                    referencesUIHierarchyItems.Expanded = referencesExpanded;
                }
                catch
                {
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

                var entitiesProjectPath = entitiesProject.FullName;
                var entitiesSolutionPath = entitiesProject == null ? null : _dte.Solution.FileName;

                var exePath = Path.Combine(toolsPathServerMock, "InitWAQSServerMock.exe");
                var exeArgs = new StringBuilder("\"" + edmxPath + "\" \"" + projectDirectoryPath + "\" \"" + toolsPathServerMock + "\" \"" + defaultNamespace + "\" \"" + waqsDirectory + "\" \"" + waqsGeneralDirectory + "\" \"" + entitiesSolutionPath + "\" \"" + entitiesProjectPath + "\" \"" + netVersion + "\" \"" + vsVersion + "\" \"" + kind.Key + "\" \"" + (copyTemplates.IsChecked == true ? "WithSourceControl" : "WithoutSourceControl") + "\" \"" + _dte.Solution.FullName + "\"");
                if ((kind.Kind & GenerationOptions.Kind.WithoutGlobalWithoutFramework) != 0)
                {
                    var specificationsProjectItem = entitiesProject?.GetAllProjectItems().FirstOrDefault(pi => ((string)pi.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "FullPath").Value).EndsWith("\\Specifications\\"));
                    exeArgs.Append(" \"" + specificationsProjectItem?.ContainingProject.FullName + "\"");
                    exeArgs.Append(" \"" + specificationsProjectItem?.GetFilePath() + "\"");
                    var dtoProjectItem = (dtoProject ?? entitiesProject).GetAllProjectItems().FirstOrDefault(pi => ((string)pi.Properties.Cast<EnvDTE.Property>().First(p => p.Name == "FullPath").Value).EndsWith("\\DTO\\"));
                    exeArgs.Append(" \"" + dtoProjectItem?.ContainingProject.FullName + "\"");
                    exeArgs.Append(" \"" + dtoProjectItem?.GetFilePath() + "\"");
                    var entityCsProjectItem = entitiesProjectItem?.ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name.EndsWith(".cs"));
                    exeArgs.Append(" \"" + entityCsProjectItem?.GetFilePath() + "\"");
                    var dalInterfaceCsProjectItem = dalInterfacesProjectItem?.ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name.EndsWith(".cs"));
                    exeArgs.Append(" \"" + dalInterfaceCsProjectItem?.GetFilePath() + "\"");
                    var dalCsProjectItem = dalProjectItem?.ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => pi.Name.EndsWith(".cs"));
                    exeArgs.Append(" \"" + dalCsProjectItem?.GetFilePath() + "\"");
                    exeArgs.Append(" \"" + edmxProjectPath + "\"");
                    var configProjectItem = edmxProjectItem.ContainingProject.ProjectItems.Cast<EnvDTE.ProjectItem>().FirstOrDefault(pi => string.Equals(pi.Name, "App.config", StringComparison.CurrentCultureIgnoreCase) || string.Equals(pi.Name, "Web.config", StringComparison.CurrentCultureIgnoreCase));
                    exeArgs.Append(" \"" + configProjectItem?.GetFilePath() + "\"");
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
                    TemplatesCopying.CopyTemplates(_dte, "ServerMockTemplates", netVersion, toolsPath, vsVersion, out templatesFolder, out existingTTIncludes, out templatesProjectItems);
                    var ttInclude = @"%AppData%\WAQS\Templates\Includes\WAQS.Roslyn.Assemblies.ttinclude";
                    var ttIncludeName = Path.GetFileName(ttInclude);
                    TemplatesCopying.AddItem(ttInclude, vsVersion, netVersion, ttIncludeName, templatesFolder, existingTTIncludes, templatesProjectItems);
                }

                if (kind.Kind == GenerationOptions.Kind.FrameworkOnly)
                {
                    edmxName = "Framework";
                }
                _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Server.Mock.waqs"));
                _project.ProjectItems.AddFromFile(Path.Combine(waqsDirectory, edmxName + ".Server.Mock.tt"));
                try
                {
                    projectUIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>().First(uihi => uihi.Name == "WAQS." + edmxName).UIHierarchyItems.Expanded = false;
                }
                catch
                {
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
