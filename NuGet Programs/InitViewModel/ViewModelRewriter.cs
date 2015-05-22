using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace InitViewModel
{
    public class ViewModelRewriter : CSharpSyntaxRewriter
    {
        private string _edmxName;
        private string _entitiesNamespace;
        private string _clientContextNamespace;
        private string _clientContextInterfacesNamespace;
        private string _waqsClientContextNamespace;
        private string _waqsClientContextInterfacesNamespace;
        private string _waqsComponentModelNamespace;

        public ViewModelRewriter(string edmxName, string entitiesNamespace, string clientContextNamespace, string clientContextInterfacesNamespace, string waqsClientContextNamespace, string waqsClientContextInterfacesNamespace, string waqsComponentModelNamespace)
        {
            _edmxName = edmxName;
            _entitiesNamespace = entitiesNamespace;
            _clientContextNamespace = clientContextNamespace;
            _clientContextInterfacesNamespace = clientContextInterfacesNamespace;
            _waqsClientContextNamespace = waqsClientContextNamespace;
            _waqsClientContextInterfacesNamespace = waqsClientContextInterfacesNamespace;
            _waqsComponentModelNamespace = waqsComponentModelNamespace;
        }

        public string NamespaceName { get; private set; }
        public string TypeName { get; private set; }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            return ((CompilationUnitSyntax)base.VisitCompilationUnit(node)).WithUsings(SyntaxFactory.List(node.Usings.Select(u => u.Name.ToString()).Union( new[] { "System", "System.Collections.Generic", "System.Linq", _entitiesNamespace, _clientContextNamespace, _clientContextInterfacesNamespace, _clientContextInterfacesNamespace + ".Errors", _waqsClientContextNamespace, _waqsClientContextInterfacesNamespace, _waqsClientContextInterfacesNamespace + ".Errors", _waqsClientContextInterfacesNamespace + ".Querying", _waqsComponentModelNamespace }).Distinct().OrderBy(u => u).Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)))));
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            NamespaceName = node.Name.ToString();
            return base.VisitNamespaceDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            TypeName = node.Identifier.ValueText;

            TypeSyntax clientContextInterface = SyntaxFactory.ParseTypeName(string.Format("I{0}ClientContext", _edmxName));
            var value =  node.AddMembers(
                SyntaxFactory.FieldDeclaration(
                    attributeLists: default(SyntaxList<AttributeListSyntax>),
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                    declaration: SyntaxFactory.VariableDeclaration(
                        clientContextInterface,
                        SyntaxFactory.SeparatedList(
                            new[] { SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("_context")) }))),
                SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier(node.Identifier.ValueText))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context")).WithType(clientContextInterface))
                    .WithInitializer(SyntaxFactory.ConstructorInitializer(
                        kind: SyntaxKind.BaseConstructorInitializer,
                        argumentList: SyntaxFactory.ArgumentList(
                            arguments: SyntaxFactory.SeparatedList(
                                new[] { SyntaxFactory.Argument(
                                    expression: SyntaxFactory.IdentifierName("context")) })))
                        .WithThisOrBaseKeyword(SyntaxFactory.Token(SyntaxKind.BaseKeyword)))
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("_context = context;"))))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ViewModelBase")));
            if (!value.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword || m.Kind() == SyntaxKind.InternalKeyword))
                value = value.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            return value;
        }
    }
}
