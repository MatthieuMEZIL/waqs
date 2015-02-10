using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace TestsDependences
{
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

        public static implicit operator PropertySymbolInfo(PropertySymbol propertySymbol)
        {
            return new PropertySymbolInfo(propertySymbol);
        }

        public static implicit operator PropertySymbol(PropertySymbolInfo propertySymbolInfo)
        {
            return propertySymbolInfo._propertySymbol;
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
            if (namedTypeSymbol == null || namedTypeSymbol.TypeArguments.Count == 0)
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

        public static implicit operator TypeSymbolInfo(TypeSymbol typeSymbol)
        {
            return new TypeSymbolInfo(typeSymbol);
        }

        public static implicit operator TypeSymbol(TypeSymbolInfo typeSymbolInfo)
        {
            return typeSymbolInfo._typeSymbol;
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
            return SpecificationMethods.GetPropertyNameFromMethod(method);
        }
    }

    public class SpecificationsElements
    {
        public Dictionary<string, List<string>> ClassesPerInterfaces = new Dictionary<string, List<string>>();
        public List<TypeSymbol> TypeSymbols = new List<TypeSymbol>();
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
