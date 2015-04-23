using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WAQS
{
    /// <summary>
    /// Interaction logic for UpdateGeneratedCodeWizard.xaml
    /// </summary>
    public partial class UpdateGeneratedCodeWizard
    {
        private EnvDTE.DTE _dte;
        private bool _cancel;

        public UpdateGeneratedCodeWizard(EnvDTE.DTE dte)
        {
            InitializeComponent();
            DataContext = this;
            _dte = dte;
            Loaded += (_, __) => Run().ConfigureAwait(true);
        }

        private ObservableCollection<LogViewModel> _logs;
        public ObservableCollection<LogViewModel> Logs
        {
            get { return _logs ?? (_logs = new ObservableCollection<LogViewModel>()); }
        }

        private async Task Run()
        {
            await Task.Delay(100);
            var referencedProjects = new HashSet<EnvDTE.Project>();

            var serverProjects = new HashSet<EnvDTE.Project>();
            foreach (var project in _dte.GetProjects())
            {
                bool error = false;
                foreach (var tt in project.GetAllProjectItems(tt => tt.Name.EndsWith(".Server.tt")).Where(tt => tt.Name.EndsWith(".Server.tt")))
                {
                    serverProjects.Add(tt.ContainingProject);
                    Logs.Add(new LogViewModel { Message = project.GetDisplayPath() });
                    await tt.RecursiveT4RunCustomToolAsync(
                        (item, exception) =>
                        {
                            error = true;
                            Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation failed", item.GetDisplayPath()), Error = true });
                            Logs.Add(new LogViewModel { Message = exception.Message, Error = true });
                        }, item => Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation succeed", item.GetDisplayPath()) }));
                    if (error)
                    {
                        return;
                    }
                    if (_cancel)
                    {
                        goto Close;
                    }
                }
            }
            Logs.Add(new LogViewModel()); //Empty line
            Logs.Add(new LogViewModel { Message = "Build solution" });
            _dte.Solution.SolutionBuild.Build(true);
            Logs.Add(new LogViewModel()); //Empty line
            foreach (var project in _dte.GetProjects().Where(p => ! serverProjects.Contains(p)))
            {
                bool error = false;
                var rootsTT = new List<string>() { ".Server.Mock.tt", ".Client.WPF.tt", ".Client.PCL.tt" };
                foreach (var tt in project.GetAllProjectItems(tt => rootsTT.Any(r => tt.Name.EndsWith(r))).Where(tt => rootsTT.Any(r => tt.Name.EndsWith(r))))
                {
                    Logs.Add(new LogViewModel { Message = project.GetDisplayPath() });
                    await tt.RecursiveT4RunCustomToolAsync(
                        (item, exception) =>
                        {
                            error = true;
                            Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation failed", item.GetDisplayPath()), Error = true });
                            Logs.Add(new LogViewModel { Message = exception.Message, Error = true });
                        }, item => Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation succeed", item.GetDisplayPath()) }));
                    if (error)
                    {
                        return;
                    }
                    if (_cancel)
                    {
                        goto Close;
                    }
                }
            }

            Close:
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            _cancel = true;
            Close();
        }

        public class LogViewModel
        {
            public string Message { get; set; }
            public bool Error { get; set; }
        }
    }
}
