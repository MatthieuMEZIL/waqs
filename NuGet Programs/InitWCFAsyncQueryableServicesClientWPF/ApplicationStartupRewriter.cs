using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using RoslynHelper;

namespace InitWCFAsyncQueryableServicesClientWPF
{
    public class ApplicationStartupRewriter : SyntaxRewriter
    {
        private string _edmxName;
        private string _rootNamespace;
        private string _clientContextNamespace;
        private string _clientInterfacesContextNamespace;
        private string _pageTypeName;
        private bool _first;
        private bool _initWCFAsyncQueryableServicesModules;
        private bool _addApplicationStart = true;
        private bool _addInitWCFAsyncQueryableServicesModules = true;

        public ApplicationStartupRewriter(string edmxName, string rootNamespace, string clientContextNamespace, string clientInterfacesContextNamespace, string pageTypeName = null, bool first = true)
        {
            _edmxName = edmxName;
            _rootNamespace = rootNamespace;
            _clientContextNamespace = clientContextNamespace;
            _clientInterfacesContextNamespace = clientInterfacesContextNamespace;
            _pageTypeName = pageTypeName;
            _first = first;
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var value = ((CompilationUnitSyntax)base.VisitCompilationUnit(node));
            if (_first)
                value = value.AddUsing("System.Threading", "Microsoft.Practices.ServiceLocation", "Microsoft.Practices.Unity", _clientContextNamespace, "WCFAsyncQueryableServices.ClientContext.Interfaces.ExpressionSerialization", "WCFAsyncQueryableServices.ComponentModel", "WCFAsyncQueryableServices.Controls");
            value = value.AddUsing(_clientInterfacesContextNamespace, _clientContextNamespace, _clientContextNamespace + ".ServiceReference");
            return value;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var value = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            if (_first)
            {
                var statements = new[] { string.Format("            unityContainer.RegisterType<I{0}Service, {0}ServiceClient>(new InjectionConstructor());\r\n", _edmxName), string.Format("            unityContainer.RegisterType<I{0}ClientContext, {0}ClientContext>();\r\n", _edmxName) };
                if (_addApplicationStart)
                    value = value.AddMember(
                        Syntax.MethodDeclaration(Syntax.PredefinedType(Syntax.Token(SyntaxKind.VoidKeyword)), "OnStartup")
                            .WithModifiers(Syntax.TokenList(
                                Syntax.Token(SyntaxKind.ProtectedKeyword),
                                Syntax.Token(SyntaxKind.OverrideKeyword)))
                            .WithParameterList(Syntax.ParameterList(
                                parameters: Syntax.SeparatedList<ParameterSyntax>(
                                    Syntax.Parameter(Syntax.Identifier("e")).WithType(Syntax.ParseTypeName("StartupEventArgs")))))
                            .WithBody(Syntax.Block(
                                statements: Syntax.List<StatementSyntax>(
                                    Syntax.ParseStatement("IUnityContainer unityContainer = new UnityContainer();"),
                                    Syntax.ParseStatement("unityContainer.RegisterType<IMessageBoxService, MessageBoxService>();"),
                                    Syntax.ParseStatement("DispatcherUnhandledException += (sender, ex) => { unityContainer.Resolve<IMessageBoxService>().ShowError(ex.Exception.Message);ex.Handled = true; };"),
                                    Syntax.ParseStatement("TaskScheduler.UnobservedTaskException += (sender, ex) => { unityContainer.Resolve<IMessageBoxService>().ShowError(ex.Exception.InnerException.Message);ex.SetObserved(); };"),
                                    Syntax.ParseStatement("InitWCFAsyncQueryableServicesModules(unityContainer);"),
                                    Syntax.ParseStatement("UIThread.Dispatcher = Application.Current.Dispatcher;"),
                                    Syntax.ParseStatement("UIThread.TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();"),
                                    Syntax.ParseStatement(string.Format("unityContainer.Resolve<{0}>().Show();", _pageTypeName))))));
                if (_addInitWCFAsyncQueryableServicesModules)
                    return value.AddMember(Syntax.MethodDeclaration(Syntax.PredefinedType(Syntax.Token(SyntaxKind.VoidKeyword)), "InitWCFAsyncQueryableServicesModules")
                        .WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.PrivateKeyword)))
                        .WithParameterList(Syntax.ParameterList(
                            parameters: Syntax.SeparatedList<ParameterSyntax>(Syntax.Parameter(Syntax.Identifier("unityContainer")).WithType(Syntax.ParseTypeName("IUnityContainer")))))
                        .WithBody(Syntax.Block(
                            statements: Syntax.List<StatementSyntax>(statements.Select(s => Syntax.ParseStatement(s))))));
            }
            return value;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            switch (node.Identifier.ValueText)
            {
                case "Application_Startup":
                    _addApplicationStart = false;
                    break;
                case "InitWCFAsyncQueryableServicesModules":
                    _addInitWCFAsyncQueryableServicesModules = false;
                    _initWCFAsyncQueryableServicesModules = true;
                    break;
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (_initWCFAsyncQueryableServicesModules)
            {
                var statements = new[] { string.Format("            unityContainer.RegisterType<I{0}Service, {0}ServiceClient>(new InjectionConstructor());\r\n", _edmxName), string.Format("            unityContainer.RegisterType<I{0}ClientContext, {0}ClientContext>();\r\n", _edmxName) };
                var value = node.InsertStatements(statements.Select(s => Syntax.ParseStatement(s)));
                _initWCFAsyncQueryableServicesModules = false;
                return value;
            }
            return base.VisitBlock(node);
        }
    }
}
