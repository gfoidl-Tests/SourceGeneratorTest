// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator;

public partial class NamedFormatGenerator
{
    private readonly record struct ParameterInfo(string Name, ITypeSymbol Type);
    //-------------------------------------------------------------------------
    private record struct MethodInfo(string Name, Accessibility Accessibility, string Template, ImmutableArray<ParameterInfo> Parameters)
    {
        public static MethodInfo Create(IMethodSymbol methodSymbol, string template)
        {
            ImmutableArray<ParameterInfo>.Builder builder = ImmutableArray.CreateBuilder<ParameterInfo>(methodSymbol.Parameters.Length);

            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                string parameterName   = parameter.Name;
                ITypeSymbol typeSymbol = parameter.Type;

                builder.Add(new ParameterInfo(parameterName, typeSymbol));
            }

            return new MethodInfo(methodSymbol.Name, methodSymbol.DeclaredAccessibility, template, builder.ToImmutableArray());
        }
    }
    //-------------------------------------------------------------------------
    private readonly record struct TypeInfo(string? Namespace, string Name, bool IsValueType)
    {
        public static TypeInfo Create(IMethodSymbol methodSymbol)
        {
            INamedTypeSymbol containingType = methodSymbol.ContainingType;
            string? ns                      = GetNamespace(containingType);

            return new TypeInfo(ns, containingType.Name, containingType.IsValueType);
        }
        //---------------------------------------------------------------------
        private static string? GetNamespace(INamedTypeSymbol namedTypeSymbol)
        {
            INamespaceSymbol containingNamespace = namedTypeSymbol.ContainingNamespace;
            return string.IsNullOrEmpty(containingNamespace.Name) ? null : containingNamespace.ToDisplayString();
        }
    }
    //-------------------------------------------------------------------------
    private record TemplateFormatMethod(TypeInfo Type, MethodInfo Method)
    {
        public static TemplateFormatMethod Create(IMethodSymbol methodSymbol, string template)
        {
            TypeInfo typeInfo     = TypeInfo.Create(methodSymbol);
            MethodInfo methodInfo = MethodInfo.Create(methodSymbol, template);

            return new TemplateFormatMethod(typeInfo, methodInfo);
        }
    }
}
