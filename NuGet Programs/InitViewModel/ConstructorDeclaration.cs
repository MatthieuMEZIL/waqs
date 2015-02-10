using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class ConstructorDeclaration
    {
        public static ConstructorDeclarationSyntax DefineParameters(this ConstructorDeclarationSyntax node, BlockSyntax body, params ParameterSyntax[] parameters)
        {
            return DefineParameters(node, body, (IEnumerable<ParameterSyntax>)parameters);
        }

        public static ConstructorDeclarationSyntax DefineParameters(this ConstructorDeclarationSyntax node, BlockSyntax body, IEnumerable<ParameterSyntax> parameters)
        {
            return Syntax.ConstructorDeclaration(
                node.AttributeLists,
                node.Modifiers,
                node.Identifier,
                Syntax.ParameterList(
                    node.ParameterList.OpenParenToken,
                    SeparatedList.Create(parameters),
                    node.ParameterList.CloseParenToken),
                    node.Initializer,
                    body,
                    node.SemicolonToken);
        }
    }
}
