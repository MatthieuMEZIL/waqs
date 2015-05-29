using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

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
            Logs.Add(new LogViewModel { Message = "Build solution" });
            Logs.Add(new LogViewModel()); //Empty line
            _dte.Solution.SolutionBuild.Build(true);
            if (_dte.Solution.SolutionBuild.LastBuildInfo != 0)
            {
                Logs.Add(new LogViewModel { Message = "Build failed", Error = true });
                return;
            }

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                goto Close;
            }

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
                        }, item => Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation succeed", item.GetDisplayPath()) }), _cancellationTokenSource.Token);
                    if (error)
                    {
                        return;
                    }
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        goto Close;
                    }
                }
            }
            Logs.Add(new LogViewModel()); //Empty line
            Logs.Add(new LogViewModel { Message = "Build solution" });
            _dte.Solution.SolutionBuild.Build(true);
            if (_dte.Solution.SolutionBuild.LastBuildInfo != 0)
            {
                Logs.Add(new LogViewModel { Message = "Build failed", Error = true });
                return;
            }
            Logs.Add(new LogViewModel()); //Empty line
            Logs.Add(new LogViewModel { Message = "Run services" });
            _dte.StartServices();
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
                        }, item => Logs.Add(new LogViewModel { Message = string.Format("\t{0} code transformation succeed", item.GetDisplayPath()) }), _cancellationTokenSource.Token);
                    if (error)
                    {
                        return;
                    }
                    if (_cancellationTokenSource.IsCancellationRequested)
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
            _cancellationTokenSource.Cancel();
            Close();
        }

        public class LogViewModel
        {
            public string Message { get; set; }
            public bool Error { get; set; }
        }
    }
}
