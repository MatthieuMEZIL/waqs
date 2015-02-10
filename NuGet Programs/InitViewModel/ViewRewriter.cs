using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using RoslynHelper;

namespace InitViewModel
{
    public class ViewRewriter : SyntaxRewriter
    {
        private string _viewModelTypeName;

        public ViewRewriter(string viewModelTypeName)
        {
            _viewModelTypeName = viewModelTypeName;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (!node.ParameterList.Parameters.Any())
                return node.DefineParameters(
                    Syntax.Block(
                        statements: Syntax.List(node.Body.Statements.Union(new [] { Syntax.ParseStatement("DataContext = vm;") }))), 
                    Syntax.Parameter(Syntax.Identifier("vm")).WithType(Syntax.ParseTypeName(_viewModelTypeName)));
            return base.VisitConstructorDeclaration(node);
        }
    }
}
