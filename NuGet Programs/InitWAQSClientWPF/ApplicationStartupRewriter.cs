using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InitWAQSClientWPF
{
    public class ApplicationStartupRewriter : CSharpSyntaxRewriter
    {
        private string _edmxName;
        private string _rootNamespace;
        private string _clientContextNamespace;
        private string _clientInterfacesContextNamespace;
        private string _pageTypeName;
        private bool _first;
        private bool _initWAQSModules;
        private bool _addApplicationStart = true;
        private bool _addInitWAQSModules = true;

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
                value = value.AddUsings(new[] { "System.Threading", "Microsoft.Practices.Unity", "WAQS.ClientContext.Interfaces", "WAQS.ComponentModel", "WAQS.Controls", _clientInterfacesContextNamespace, _clientContextNamespace, _clientContextNamespace + ".ServiceReference" }.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))).ToArray());
            return value;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var value = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            if (_first)
            {
                var statements = new[] { string.Format("            unityContainer.RegisterType<I{0}Service, {0}ServiceClient>(new InjectionConstructor());\r\n", _edmxName), string.Format("            unityContainer.RegisterType<I{0}ClientContext, {0}ClientContext>();\r\n", _edmxName), string.Format("            ClientContextFactory<I{0}ClientContext>.Factory = () => unityContainer.Resolve<I{0}ClientContext>();\r\n", _edmxName) };
                if (_addApplicationStart)
                    value = value.AddMembers(
                        SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "OnStartup")
                            .WithModifiers(SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                            .AddParameterListParameters(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")).WithType(SyntaxFactory.ParseTypeName("StartupEventArgs")))
                            .WithBody(SyntaxFactory.Block().AddStatements(
                                SyntaxFactory.ParseStatement("IUnityContainer unityContainer = new UnityContainer();"),
                                SyntaxFactory.ParseStatement("unityContainer.RegisterType<IMessageBoxService, MessageBoxService>();"),
                                SyntaxFactory.ParseStatement("DispatcherUnhandledException += (sender, ex) => { unityContainer.Resolve<IMessageBoxService>().ShowError(ex.Exception.Message);ex.Handled = true; };"),
                                SyntaxFactory.ParseStatement("TaskScheduler.UnobservedTaskException += (sender, ex) => { unityContainer.Resolve<IMessageBoxService>().ShowError(ex.Exception.InnerException.Message);ex.SetObserved(); };"),
                                SyntaxFactory.ParseStatement("InitWAQSModules(unityContainer);"),
                                SyntaxFactory.ParseStatement("UIThread.Dispatcher = Application.Current.Dispatcher;"),
                                SyntaxFactory.ParseStatement("UIThread.TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();"),
                                SyntaxFactory.ParseStatement(string.Format("unityContainer.Resolve<{0}>().Show();", _pageTypeName)))));
                if (_addInitWAQSModules)
                    return value.AddMembers(
                        SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "InitWAQSModules")
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                            .AddParameterListParameters(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("unityContainer")).WithType(SyntaxFactory.ParseTypeName("IUnityContainer")))
                            .WithBody(SyntaxFactory.Block().AddStatements(
                                statements.Select(s => SyntaxFactory.ParseStatement(s)).ToArray())));
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
                case "InitWAQSModules":
                    _addInitWAQSModules = false;
                    _initWAQSModules = true;
                    break;
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (_initWAQSModules)
            {
                var statements = new[] { string.Format("            unityContainer.RegisterType<I{0}Service, {0}ServiceClient>(new InjectionConstructor());\r\n", _edmxName), string.Format("            unityContainer.RegisterType<I{0}ClientContext, {0}ClientContext>();\r\n", _edmxName) };
                var value = node.WithStatements(
                    SyntaxFactory.List(
                        statements.Select(s => SyntaxFactory.ParseStatement(s)).Union(node.Statements)));
                _initWAQSModules = false;
                return value;
            }
            return base.VisitBlock(node);
        }
    }
}
