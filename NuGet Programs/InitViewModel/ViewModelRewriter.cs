using Roslyn.Compilers.CSharp;
using RoslynHelper;
using System.Linq;

namespace InitViewModel
{
    public class ViewModelRewriter : SyntaxRewriter
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
            return ((CompilationUnitSyntax)base.VisitCompilationUnit(node)).AddUsing("System", "System.Collections.Generic", "System.Linq", _entitiesNamespace, _clientContextNamespace, _clientContextInterfacesNamespace, _clientContextInterfacesNamespace + ".Errors", _waqsClientContextNamespace, _waqsClientContextInterfacesNamespace, _waqsClientContextInterfacesNamespace + ".Errors", _waqsClientContextInterfacesNamespace + ".Querying", _waqsComponentModelNamespace);
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            NamespaceName = node.Name.ToString();
            return base.VisitNamespaceDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            TypeName = node.Identifier.ValueText;

            TypeSyntax clientContextInterface = Syntax.ParseTypeName(string.Format("I{0}ClientContext", _edmxName));
            var value =  node.AddMembers(
                Syntax.FieldDeclaration(
                    null,
                    modifiers: Syntax.TokenList(
                        Syntax.Token(SyntaxKind.PrivateKeyword)),
                    declaration: Syntax.VariableDeclaration(
                        clientContextInterface,
                        SeparatedList.CreateFromSingle(
                            Syntax.VariableDeclarator(
                                Syntax.Identifier("_context"))))),
                Syntax.ConstructorDeclaration(Syntax.Identifier(node.Identifier.ValueText))
                    .WithModifiers(Syntax.TokenList(Syntax.Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(Syntax.ParameterList(
                        parameters: SeparatedList.CreateFromSingle(
                            Syntax.Parameter(Syntax.Identifier("context")).WithType(clientContextInterface))))
                    .WithInitializer(Syntax.ConstructorInitializer(
                        kind: SyntaxKind.BaseConstructorInitializer,
                        argumentList: Syntax.ArgumentList(
                            arguments: SeparatedList.CreateFromSingle(
                                Syntax.Argument(
                                    expression: Syntax.IdentifierName("context")))))
                        .WithThisOrBaseKeyword(Syntax.Token(SyntaxKind.BaseKeyword)))
                    .WithBody(Syntax.Block(
                        statements: Syntax.List<StatementSyntax>(
                            Syntax.ParseStatement("_context = context;"))))).DefineBaseClass("ViewModelBase");
            if (!value.Modifiers.Any(m => m.Kind == SyntaxKind.PublicKeyword || m.Kind == SyntaxKind.InternalKeyword))
                value = value.WithModifiers(Syntax.TokenList(node.Modifiers.Union(new[] { Syntax.Token(SyntaxKind.PublicKeyword) })));
            return value;
        }
    }
}
