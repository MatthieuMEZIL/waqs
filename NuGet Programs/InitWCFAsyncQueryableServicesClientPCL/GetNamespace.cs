using System.Linq;
using Roslyn.Compilers.CSharp;

namespace InitWCFAsyncQueryableServicesClientPCL
{
    public class GetNamespace : SyntaxVisitor<string>
    {
        public override string VisitCompilationUnit(CompilationUnitSyntax node)
        {
            return Visit(node.ChildNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault());
        }

        public override string VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return node.Name.ToString();
        }
    }
}
