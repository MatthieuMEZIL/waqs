using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace InitViewModel
{
    public class ViewRewriter : CSharpSyntaxRewriter
    {
        private string _viewModelTypeName;

        public ViewRewriter(string viewModelTypeName)
        {
            _viewModelTypeName = viewModelTypeName;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.ParameterList.Parameters.Count == 0)
                return node
                    .WithBody(
                        SyntaxFactory.Block(
                            statements: SyntaxFactory.List(node.Body.Statements.Union(new [] { SyntaxFactory.ParseStatement("DataContext = vm;") }))))
                    .WithParameterList(
                        SyntaxFactory.ParameterList(
                            SyntaxFactory.SeparatedList(
                                new[] { SyntaxFactory.Parameter(SyntaxFactory.Identifier("vm")).WithType(SyntaxFactory.ParseTypeName(_viewModelTypeName)) })));
            return base.VisitConstructorDeclaration(node);
        }
    }
}
