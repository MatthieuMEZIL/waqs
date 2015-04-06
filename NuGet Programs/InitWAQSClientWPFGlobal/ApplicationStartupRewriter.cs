using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace InitWAQSClientWPFGlobal
{
    public class ApplicationStartupRewriter : CSharpSyntaxRewriter
    {
        private string _clientContextNamespace;
        private bool _initWAQSModules;


        public ApplicationStartupRewriter(string clientContextNamespace)
        {
            _clientContextNamespace = clientContextNamespace;
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var value = ((CompilationUnitSyntax)base.VisitCompilationUnit(node));
            return value.WithUsings(
                SyntaxFactory.List(
                    value.Usings.Union(new[] { _clientContextNamespace, _clientContextNamespace + ".ServiceReference" }.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))))));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            switch (node.Identifier.ValueText)
            {
                case "InitWAQSModules":
                    _initWAQSModules = true;
                    break;
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (_initWAQSModules)
            {
                var statements = new[] { "unityContainer.RegisterType<IGlobalWCFService, GlobalWCFServiceClient>(new InjectionConstructor());", "unityContainer.RegisterType<IGlobalClientContext, GlobalClientContext>();" };
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
