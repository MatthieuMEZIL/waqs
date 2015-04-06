using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace WAQS
{ 
    public class GetNamespace : CSharpSyntaxVisitor<string> 
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
