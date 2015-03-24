using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class Block
    {
        public static BlockSyntax DefineStatements(this BlockSyntax node, IEnumerable<StatementSyntax> statements)
        {
            return Syntax.Block(node.OpenBraceToken, Syntax.List<StatementSyntax>(statements), node.CloseBraceToken);
        }

        public static BlockSyntax InsertStatements(this BlockSyntax node, IEnumerable<StatementSyntax> statements)
        {
            return Syntax.Block(node.OpenBraceToken, Syntax.List<StatementSyntax>(statements.Union(node.Statements)), node.CloseBraceToken);
        }

        public static BlockSyntax InsertStatements(this BlockSyntax node, params StatementSyntax[] statements)
        {
            return InsertStatements(node, (IEnumerable<StatementSyntax>)statements);
        }
    }
}
