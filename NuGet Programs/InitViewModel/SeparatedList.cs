using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace RoslynHelper
{
    public static class SeparatedList
    {
        public static SeparatedSyntaxList<T> CreateFromSingle<T>(T value)
            where T : SyntaxNode
        {
            return Create<T>(new[] { value });
        }

        public static SeparatedSyntaxList<T> Create<T>(params T[] values)
            where T : SyntaxNode
        {
            return Create<T>((IEnumerable<T>)values);
        }

        public static SeparatedSyntaxList<T> Create<T>(IEnumerable<T> values, SyntaxKind separator = SyntaxKind.CommaToken)
            where T : SyntaxNode
        {
            return Syntax.SeparatedList<T>(values, values.Skip(1).Select(v => Syntax.Token(separator)));
        }
    }
}
