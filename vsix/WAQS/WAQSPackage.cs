using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Windows;
using NuGet.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;

namespace WAQS
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidWAQSPkgString)]
    public sealed class WAQSPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public WAQSPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID waqsServerCommandID = new CommandID(GuidList.guidWAQSProjectCmdSet, (int)PkgCmdIDList.WAQSServerId);
                MenuCommand waqsServerItem = new MenuCommand(WAQSServerCallback, waqsServerCommandID);
                mcs.AddCommand(waqsServerItem);

                CommandID waqsServerMockCommandID = new CommandID(GuidList.guidWAQSProjectCmdSet, (int)PkgCmdIDList.WAQSServerMockId);
                MenuCommand waqsServerMockItem = new MenuCommand(WAQSServerMockCallback, waqsServerMockCommandID);
                mcs.AddCommand(waqsServerMockItem);

                CommandID waqsClientWPFCommandID = new CommandID(GuidList.guidWAQSProjectCmdSet, (int)PkgCmdIDList.WAQSClientWPFId);
                MenuCommand waqsClientWPFItem = new MenuCommand(WAQSClientWPFCallback, waqsClientWPFCommandID);
                mcs.AddCommand(waqsClientWPFItem);

                CommandID waqsClientPCLCommandID = new CommandID(GuidList.guidWAQSProjectCmdSet, (int)PkgCmdIDList.WAQSClientPCLId);
                MenuCommand waqsClientPCLItem = new MenuCommand(WAQSClientPCLCallback, waqsClientPCLCommandID);
                mcs.AddCommand(waqsClientPCLItem);

                CommandID waqsUpdateGeneratedCodeCommandID = new CommandID(GuidList.guidWAQSProjectCmdSet, (int)PkgCmdIDList.WAQSUpdateGeneratedCodeId);
                MenuCommand waqsUpdateGeneratedCodeItem = new MenuCommand(WAQSUpdateGeneratedCodeCallback, waqsUpdateGeneratedCodeCommandID);
                mcs.AddCommand(waqsUpdateGeneratedCodeItem);

                CommandID waqsInitVMCommandID = new CommandID(GuidList.guidWAQSFileCmdSet, (int)PkgCmdIDList.WAQSInitVMId);
                MenuCommand waqsInitVMItem = new MenuCommand(WAQSInitVMCallback, waqsInitVMCommandID);
                mcs.AddCommand(waqsInitVMItem);
            }
        }
        #endregion

        private void WAQSServerCallback(object sender, EventArgs e)
        {
            WAQSWizardCallback((project, uiShell, packageInstaller, installerServices) => new ServerWizard(project, uiShell, packageInstaller, installerServices));

        }

        private void WAQSServerMockCallback(object sender, EventArgs e)
        {
            WAQSWizardCallback((project, uiShell, packageInstaller, installerServices) => new ServerMockWizard(project, uiShell, packageInstaller, installerServices));

        }

        private void WAQSClientWPFCallback(object sender, EventArgs e)
        {
            WAQSWizardCallback((project, uiShell, packageInstaller, installerServices) => new ClientWizard(GenerationOptions.WPF, project, uiShell, packageInstaller, installerServices));
        }

        private void WAQSClientPCLCallback(object sender, EventArgs e)
        {
            WAQSWizardCallback((project, uiShell, packageInstaller, installerServices) => new ClientWizard(GenerationOptions.PCL, project, uiShell, packageInstaller, installerServices));
        }

        private void WAQSWizardCallback(Func<Project, IVsUIShell, IVsPackageInstaller, IVsPackageInstallerServices, System.Windows.Window> createWizard)
        {
            var dte = GetService(typeof(DTE)) as DTE;
            if (dte.SelectedItems.Count != 1)
                return;

            var project = dte.SelectedItems.Item(1).Project;
            if (project == null)
                return;

            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var packageInstaller = componentModel.GetService<IVsPackageInstaller>();
            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            var wizard = createWizard(project, uiShell, packageInstaller, installerServices);
            wizard.Owner = Application.Current.MainWindow;
            wizard.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wizard.ShowDialog();
        }

        private void WAQSUpdateGeneratedCodeCallback(object sender, EventArgs e)
        {
            var dte = GetService(typeof(DTE)) as DTE;
            var wizard = new UpdateGeneratedCodeWizard(dte);
            wizard.Owner = Application.Current.MainWindow;
            wizard.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wizard.ShowDialog();
        }

        private void WAQSInitVMCallback(object sender, EventArgs e)
        {
            var dte = GetService(typeof(DTE)) as DTE;
            var viewModel = dte.SelectedItems.Item(1).ProjectItem;
            if (! viewModel.Name.EndsWith(".cs"))
            {
                MessageBox.Show(Application.Current.MainWindow, "The view model must be a C# file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();
            var wizard = new ViewModelWizard(dte, viewModel, installerServices);
            wizard.Owner = Application.Current.MainWindow;
            wizard.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wizard.ShowDialog();
        }
    }
}
