using NuGet.VisualStudio;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WAQS
{
    /// <summary>
    /// Interaction logic for ViewModelWizard.xaml
    /// </summary>
    public partial class ViewModelWizard
    {
        private EnvDTE.DTE _dte;
        private EnvDTE.ProjectItem _viewModel;
        private IVsPackageInstallerServices _packageInstallerServices;

        public ViewModelWizard(EnvDTE.DTE dte, EnvDTE.ProjectItem viewModel, IVsPackageInstallerServices packageInstallerServices)
        {
            InitializeComponent();

            _dte = dte;
            _viewModel = viewModel;
            _packageInstallerServices = packageInstallerServices;

            var edmxs = _dte.GetSolutionEdmx(_viewModel.ContainingProject, skipWaqsAlreadyUsed: false).ToList();
            edmx.ItemsSource = edmxs;
            edmx.SelectedItem = edmxs.FirstOrDefault();

            var views = _dte.GetSolutionXaml(_viewModel.ContainingProject).ToList();
            views.Insert(0, new DTEExtensions.FilePathes { DisplayPath = "", FullPath = null });
            view.ItemsSource = views;
            view.SelectedItem = views.FirstOrDefault();
        }

        private void EdmxSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshGenerationEnabled();
        }

        private void RefreshGenerationEnabled()
        {
            init.IsEnabled = edmx.SelectedValue != null;
        }

        private void InitClick(object sender, RoutedEventArgs e)
        {
            var originalCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                var edmxName = Path.GetFileNameWithoutExtension((string)edmx.SelectedValue);
                var project = _viewModel.ContainingProject;
                var defaultNamespace = project.Properties.Cast<EnvDTE.Property>().FirstOrDefault(p => p.Name == "RootNamespace")?.Value;
                string targetFrameworkMoniker = project.GetTargetFrameworkMoniker();
                string clientVersion;
                if (targetFrameworkMoniker.StartsWith(".NETFramework,"))
                {
                    clientVersion = "WPF";
                }
                else if (targetFrameworkMoniker.StartsWith(".NETPortable,"))
                {
                    clientVersion = "PCL";
                }
                else
                {
                    throw new NotImplementedException();
                }
                var waqsFilePath = _dte.Solution.FindProjectItem(Path.Combine(Path.GetDirectoryName(project.FullName), "WAQS." + edmxName, edmxName + ".Client." + clientVersion + ".waqs")).GetFilePath();
                var viewModelPath = _viewModel.GetFilePath();
                var viewPath = (string)view.SelectedValue;
                try
                {
                    _dte.SourceControl.CheckOutItem(viewModelPath);
                    if (viewPath != null)
                    {
                        _dte.SourceControl.CheckOutItem(viewPath);
                    }
                }
                catch
                {
                }
                var toolsPath = Path.Combine(_packageInstallerServices.GetPackageLocation("WAQS.Client." + clientVersion), "tools");
                var toolsPathServer = Path.Combine(toolsPath, "Client." + clientVersion);
                var exePath = Path.Combine(toolsPathServer, "InitViewModel.exe");
                var exeArgs = "\"" + edmxName + "\" \"" + defaultNamespace + "\" \"" + viewModelPath + "\" \"" + waqsFilePath + "\" \"" + viewPath + "\"";
                var process = new Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = exeArgs.ToString();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
                Close();
            }
            finally
            {
                Cursor = originalCursor;
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
