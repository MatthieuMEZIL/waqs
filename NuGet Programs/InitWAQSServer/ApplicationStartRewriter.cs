using System;
using System.IO;
using System.Linq;
using Roslyn.Compilers.CSharp;
using RoslynHelper;

namespace InitWAQSServer
{
    public class ApplicationStartRewriter : SyntaxRewriter
    {
        private string _edmxName;
        private bool _applicationStart;
        private bool _notPartialClass;
        private string _globalAsaxCsModelPath;
        private string _globalModel;
        private CompilationUnitSyntax _globalModelCompilationUnit;

        public ApplicationStartRewriter(string edmxName, string globalAsaxCsModelPath)
        {
            _edmxName = edmxName;
            _globalAsaxCsModelPath = globalAsaxCsModelPath;
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var value = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
            if (_notPartialClass)
            {
                var usings = _globalModelCompilationUnit.Usings.Where(u => !value.Usings.Any(u2 => u2.Name.ToString() == u.Name.ToString())).ToList();
                if (usings.Count != 0)
                {
                    value = value.WithUsings(Syntax.List<UsingDirectiveSyntax>(value.Usings.Union(usings).OrderBy(u => u.Name.ToString())));
                }
            }
            return value;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.Kind == SyntaxKind.PartialKeyword))
                return base.VisitClassDeclaration(node);
            _notPartialClass = true;
            return ((ClassDeclarationSyntax)base.VisitClassDeclaration(node)).WithModifiers(Syntax.TokenList(node.Modifiers.Union(new[] { Syntax.Token(SyntaxKind.PartialKeyword) })));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.ValueText == "Application_Start")
                _applicationStart = true;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (_applicationStart)
            {
                _applicationStart = false;
                if (_notPartialClass)
                {
                    using (var globalModelSR = new StreamReader(_globalAsaxCsModelPath))
                    {
                        _globalModel = globalModelSR.ReadToEnd();
                    }
                    _globalModelCompilationUnit = Syntax.ParseCompilationUnit(_globalModel);
                    var globalModelNamespace = _globalModelCompilationUnit.ChildNodes().OfType<NamespaceDeclarationSyntax>().First();
                    var globalModelClass = globalModelNamespace.ChildNodes().OfType<ClassDeclarationSyntax>().First();
                    var globalModelApplicationStart = globalModelClass.ChildNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.ValueText == "Application_Start");
                    var globalModelApplicationStartBody = globalModelApplicationStart.Body;
                    node = node.AddStatements(globalModelApplicationStartBody.Statements.ToArray());
                }
                return node.AddStatements(Syntax.ParseExpression("Global." + _edmxName + "ApplicationStart(unityContainer);"));
            }
            return base.VisitBlock(node);
        }
    }
}
