using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using MethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using PropertySymbol = Microsoft.CodeAnalysis.IPropertySymbol;
using TypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;
using ISemanticModel = Microsoft.CodeAnalysis.SemanticModel;

namespace TestsDependences
{
    public enum RoslynSyntaxKind
    {
        AssignExpression = SyntaxKind.SimpleAssignmentExpression,
        AddAssignExpression = SyntaxKind.AddAssignmentExpression,
        SubtractAssignExpression = SyntaxKind.SubtractAssignmentExpression,
        MultiplyAssignExpression = SyntaxKind.MultiplyAssignmentExpression,
        DivideAssignExpression = SyntaxKind.DivideAssignmentExpression,
        ModuloAssignExpression = SyntaxKind.ModuloAssignmentExpression,
        AsExpression = SyntaxKind.AsExpression
    }

    partial class GetMembersVisitor
    {
        public static SyntaxKind GetKind(CSharpSyntaxNode node)
        {
            return node.Kind();
        }

        public static SyntaxKind GetKind(SyntaxNode node)
        {
            return GetKind((CSharpSyntaxNode)node);
        }

        public static SyntaxKind GetKind(SyntaxToken node)
        {
            return node.Kind();
        }

        public static bool IsAssignExpression(SyntaxKind kind)
        {
            return kind == SyntaxKind.SimpleAssignmentExpression;
        }
    }

    public class PropertySymbolInfo
    {
        private PropertySymbol _propertySymbol;

        public PropertySymbolInfo(TypeSymbolInfo type, string name, TypeSymbolInfo containingType,
            MethodDeclarationSyntax method = null)
        {
            Type = type;
            Name = name;
            ContainingType = containingType;
            GetMethod = method;
        }

        public PropertySymbolInfo(PropertySymbol propertySymbol)
        {
            _propertySymbol = propertySymbol;
            Type = new TypeSymbolInfo(propertySymbol.Type);
            Name = propertySymbol.Name;
            ContainingType = new TypeSymbolInfo(propertySymbol.ContainingType);
        }

        public TypeSymbolInfo Type { get; private set; }
        public string Name { get; private set; }
        public TypeSymbolInfo ContainingType { get; set; }
        public bool FromOriginalMethod { get; set; }
        public MethodDeclarationSyntax GetMethod { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public static PropertySymbolInfo Get(PropertySymbol propertySymbol)
        {
            return new PropertySymbolInfo(propertySymbol);
        }
    }

    public class TypeSymbolInfo
    {
        private TypeSymbol _typeSymbol;

        public TypeSymbolInfo(TypeSymbol typeSymbol)
        {
            _typeSymbol = typeSymbol;
            Name = typeSymbol.Name;
            FullName = typeSymbol.ToString();
            ContainingNamespace = typeSymbol.ContainingNamespace == null
                ? null
                : typeSymbol.ContainingNamespace.ToString();
            AllInterfaces = GetAllInterfaces(typeSymbol);
            TypeArguments = GetTypeArguments(typeSymbol);
        }

        public static IEnumerable<TypeSymbolInfo> GetAllInterfaces(TypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.OfType<TypeSymbol>().Select(i => new TypeSymbolInfo(i));
        }

        public static TypeSymbolInfo[] GetTypeArguments(TypeSymbol typeSymbol)
        {
            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
            return namedTypeSymbol == null
                ? new TypeSymbolInfo[0]
                : namedTypeSymbol.TypeArguments.OfType<TypeSymbol>().Select(i => new TypeSymbolInfo(i)).ToArray();
        }

        public static string GetBasicTypeName(TypeSymbol type, Func<TypeSymbol, TypeSymbol> transformType = null)
        {
            return GetBasicTypeNameInternal(type, transformType);
        }

        private static string GetBasicTypeNameInternal(TypeSymbol type, Func<TypeSymbol, TypeSymbol> transformType)
        {
            if (transformType != null)
                type = transformType(type);
            var namedTypeSymbol = type as INamedTypeSymbol;
            if (namedTypeSymbol == null || ! namedTypeSymbol.TypeArguments.Any())
                return type.Name;
            return string.Concat(type.Name, "<",
                namedTypeSymbol.TypeArguments.OfType<TypeSymbol>()
                    .Select(ta => GetBasicTypeNameInternal(ta, transformType))
                    .Aggregate((t1, t2) => string.Concat(t1, ",", t2)), ">");
        }

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string ContainingNamespace { get; private set; }
        public IEnumerable<TypeSymbolInfo> AllInterfaces { get; private set; }
        public TypeSymbolInfo[] TypeArguments { get; private set; }

        public override string ToString()
        {
            return FullName;
        }

        public static TypeSymbolInfo Get(TypeSymbol typeSymbol)
        {
            return new TypeSymbolInfo(typeSymbol);
        }

        public bool IsAssignableFrom(TypeSymbolInfo typeSymbolInfo)
        {
            var typeSymbolString = _typeSymbol.ToString();
            for (var typeSymbolLoop = typeSymbolInfo._typeSymbol; typeSymbolLoop != null; typeSymbolLoop = typeSymbolLoop.BaseType)
            {
                if (typeSymbolLoop.ToString() == typeSymbolString)
                    return true;

                var typeSymbolLoopString = typeSymbolLoop.ToString();

                if (typeSymbolLoop.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name == "op_Implicit" || m.Name == "op_Explicit").Any(op => op.ReturnType.ToString() == typeSymbolString) || _typeSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name == "op_Implicit" || m.Name == "op_Explicit").Any(op => op.ReturnType.ToString() == typeSymbolLoopString))
                    return true;
            }
            if (typeSymbolInfo._typeSymbol.AllInterfaces.Any(i => i.ToString() == typeSymbolString))
                return true;
            return false;
        }
    }

    public class SpecificationMethods
    {
        public static string GetPropertyNameFromMethodName(string methodName)
        {
            return SpecificationMethods.GetPropertyNameFromMethodName(methodName);
        }

        public static string GetPropertyNameFromMethod(MethodDeclarationSyntax method)
        {
            return method.Identifier.ValueText.Substring(3);
        }
    }

    public class SpecificationsElements
    {
        public Dictionary<string, List<string>> ClassesPerInterfaces = new Dictionary<string, List<string>>();
        public Dictionary<string, TypeSymbol> TypeSymbols = new Dictionary<string, TypeSymbol>();
        public bool GetSpecificationEquivalentMethod(ref MethodSymbol methodSymbol, List<TypeSymbol> argumentTypes = null)
        {
            return false;
        }
    }

    public class SpecificationEquivalentMethod
    {
        public static bool GetSpecificationEquivalentMethod(SpecificationsElements specificationsElements,
            ref MethodSymbol methodSymbol, Dictionary<MethodDeclarationSyntax, ISemanticModel> semanticModelPerMethods,
            Dictionary<string, List<MethodDeclarationSyntax>> specificationGetMethods,
            List<TypeSymbol> argumentTypes = null)
        {
            return false;
        }

        public static bool GetSpecificationEquivalentMethod(SpecificationsElements specificationsElements,
            ref MethodSymbol methodSymbol, Dictionary<MethodDeclarationSyntax, ISemanticModel> semanticModelPerMethods,
            Func<IEnumerable<MethodDeclarationSyntax>> getMethods)
        {
            return false;
        }

        public static bool GetSpecificationEquivalentMethod(SpecificationsElements specificationsElements,
            ref MethodSymbol methodSymbol, Dictionary<MethodDeclarationSyntax, ISemanticModel> semanticModelPerMethods,
            IEnumerable<MethodDeclarationSyntax> candidatesMethods, string defaultClassName = null)
        {
            return false;
        }
    }
}
