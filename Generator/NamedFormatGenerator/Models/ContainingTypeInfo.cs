// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Models;

internal readonly record struct ContainingTypeInfo(string? Namespace, string Name, bool IsValueType)
{
    public static ContainingTypeInfo Create(IMethodSymbol methodSymbol)
    {
        INamedTypeSymbol containingType = methodSymbol.ContainingType;
        string? ns                      = GetNamespace(containingType);

        return new ContainingTypeInfo(ns, containingType.Name, containingType.IsValueType);
    }
    //-------------------------------------------------------------------------
    private static string? GetNamespace(INamedTypeSymbol namedTypeSymbol)
    {
        INamespaceSymbol containingNamespace = namedTypeSymbol.ContainingNamespace;
        return string.IsNullOrEmpty(containingNamespace.Name) ? null : containingNamespace.ToDisplayString();
    }
}
