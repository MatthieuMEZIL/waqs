using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class ClassDeclaration
    {
        public static ClassDeclarationSyntax AddMember(this ClassDeclarationSyntax node, MemberDeclarationSyntax member)
        {
            return AddMembers(node, new[] { member });
        }

        public static ClassDeclarationSyntax AddMembers(this ClassDeclarationSyntax node, IEnumerable<MemberDeclarationSyntax> members)
        {
            return DefineMembers(node, node.Members.Union(members));
        }

        public static ClassDeclarationSyntax DefineMembers(this ClassDeclarationSyntax node, IEnumerable<MemberDeclarationSyntax> members)
        {
            return Syntax.ClassDeclaration(
                attributeLists: node.AttributeLists,
                modifiers: node.Modifiers,
                keyword: node.Keyword,
                identifier: node.Identifier,
                typeParameterList: node.TypeParameterList,
                baseList: node.BaseList,
                constraintClauses: node.ConstraintClauses,
                openBraceToken: node.OpenBraceToken,
                members: Syntax.List<MemberDeclarationSyntax>(members),
                closeBraceToken: node.CloseBraceToken,
                semicolonToken: node.SemicolonToken);
        }
    }
}
