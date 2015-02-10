using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class ClassDeclaration
    {
        public static ClassDeclarationSyntax AddMembers(this ClassDeclarationSyntax node, params MemberDeclarationSyntax[] members)
        {
            return AddMembers(node, (IEnumerable<MemberDeclarationSyntax>)members);
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

        public static ClassDeclarationSyntax DefineBaseClass(this ClassDeclarationSyntax node, string typeName)
        {
            return Syntax.ClassDeclaration(
                attributeLists: node.AttributeLists,
                modifiers: node.Modifiers,
                keyword: node.Keyword,
                identifier: node.Identifier,
                typeParameterList: node.TypeParameterList,
                baseList: Syntax.BaseList(
                    types: SeparatedList.Create(new[] { Syntax.ParseTypeName(typeName) }.Union(node.BaseList == null ? Enumerable.Empty<TypeSyntax>() : node.BaseList.Types))),
                constraintClauses: node.ConstraintClauses,
                openBraceToken: node.OpenBraceToken,
                members: node.Members,
                closeBraceToken: node.CloseBraceToken,
                semicolonToken: node.SemicolonToken);
        }
    }
}
