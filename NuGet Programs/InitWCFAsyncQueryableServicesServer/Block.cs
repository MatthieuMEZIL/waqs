using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class Block
    {
        public static BlockSyntax AddStatements(this BlockSyntax node, params ExpressionSyntax[] statements)
        {
            return Syntax.Block(node.OpenBraceToken, Syntax.List<StatementSyntax>(node.Statements.Union(statements.Select(s => Syntax.ExpressionStatement(s)))), node.CloseBraceToken);
        }
    }
}
