using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace InitWAQSServer
{
    public class GlobalWCFServiceRewriter : CSharpSyntaxRewriter
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
                    SyntaxFactory.List(
                        node.Statements.Take(node.Statements.Count - 2)
                            .Union(new[] { SyntaxFactory.ParseStatement(_edmxName + "SaveChanges(clientContexts);") })
                            .Union(node.Statements.Skip(node.Statements.Count - 2))));
            }
            return base.VisitBlock(node);
        }
    }
}
