using System;
using System.Linq;
using Roslyn.Compilers.CSharp;
using RoslynHelper;

namespace InitWAQSServer
{
    public class GlobalWCFServiceRewriter : SyntaxRewriter
    {
        private string _edmxName;
        private bool _isSaveChanges;

        public GlobalWCFServiceRewriter(string edmxName)
        {
            _edmxName = edmxName;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.ValueText == "SaveChanges")
                _isSaveChanges = true;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            if (_isSaveChanges && node.Parent is UsingStatementSyntax && node.Statements.Count > 2)
            {
                _isSaveChanges = false;
                return node.WithStatements(
                    Syntax.List(
                        node.Statements.Take(node.Statements.Count - 2)
                            .Union(new[] { Syntax.ParseStatement(_edmxName + "SaveChanges(clientContexts);") })
                            .Union(node.Statements.Skip(node.Statements.Count - 2))));
            }
            return base.VisitBlock(node);
        }
    }
}
