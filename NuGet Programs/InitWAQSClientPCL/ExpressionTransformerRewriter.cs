using System.Linq;
using Roslyn.Compilers.CSharp;
using RoslynHelper;

namespace InitWAQSClientPCL
{
    class ExpressionTransformerRewriter : SyntaxRewriter
    {
        private string _edmxName;
        private string _expressionTransformerNamespaceDot;
        private bool _isExpressionTransformer;
        private bool _transform;

        public ExpressionTransformerRewriter(string edmxName, string expressionTransformerNamespace)
        {
            _edmxName = edmxName;
            _expressionTransformerNamespaceDot = string.IsNullOrEmpty(expressionTransformerNamespace) ? "" : (expressionTransformerNamespace + ".");
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.ValueText == "TransformExpression")
                _isExpressionTransformer = true;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var value = base.VisitBlock(node);
            if (_isExpressionTransformer && _transform && node.Parent is MethodDeclarationSyntax)
            {
                _isExpressionTransformer = false;
                _transform = false;
                var newStatements = new[] { string.Concat("value = new ", _expressionTransformerNamespaceDot, _edmxName, @"ExpressionTransformer().TransformExpression(expression, contextName);"), @"
            if (value != expression)
                return value;" };

                var statements = node.ChildNodes().OfType<StatementSyntax>().ToList();
                return node.DefineStatements(statements.Take(statements.Count - 1).Union(newStatements.Select(s => Syntax.ParseStatement(s))).Union(new[] { statements[statements.Count - 1] }));
            }
            return value;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var memberAccessExpression = node.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpression != null && memberAccessExpression.Name.ToString() == "TransformExpression")
                _transform = true;
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            var section = node.Sections.FirstOrDefault();
            if (section != null)
            {
                if (_isExpressionTransformer)
                {
                    _isExpressionTransformer = false;
                    Visit(section);
                    if (_transform)
                    {
                        _isExpressionTransformer = false;
                        _transform = false;
                        return node.AddSections(
                            Syntax.SwitchSection(
                                Syntax.List(
                                    Syntax.SwitchLabel(
                                        SyntaxKind.CaseSwitchLabel,
                                        Syntax.ParseExpression(string.Concat("\"", _expressionTransformerNamespaceDot, _edmxName, "ClientContext\"")))),
                                Syntax.List(
                                    Syntax.ParseStatement(string.Concat("return new ", _expressionTransformerNamespaceDot, _edmxName, @"ExpressionTransformer().TransformExpression(expression, contextName);")))));
                    }
                }
            }
            return base.VisitSwitchStatement(node);
        }
    }
}
