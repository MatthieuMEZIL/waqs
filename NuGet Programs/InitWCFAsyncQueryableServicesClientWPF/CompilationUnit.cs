using System.Linq;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class CompilationUnit
    {
        public static CompilationUnitSyntax AddUsing(this CompilationUnitSyntax node, params string[] namespaces)
        {
            return Syntax.CompilationUnit(
                node.Externs,
                Syntax.List<UsingDirectiveSyntax>(
                    node.Usings.Union(namespaces.Where(ns => ! node.Usings.Any(u2 => u2.Name.ToString() == ns.Trim())).Select(ns => Syntax.UsingDirective(
                        name: Syntax.IdentifierName(ns))))),
                    node.AttributeLists,
                    node.Members,
                    node.EndOfFileToken);
        }
    }
}
