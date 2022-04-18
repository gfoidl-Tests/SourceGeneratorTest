// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Models;

internal record TemplateFormatMethod(ContainingTypeInfo Type, MethodInfo Method)
{
    public static TemplateFormatMethod Create(IMethodSymbol methodSymbol, string template)
    {
        ContainingTypeInfo typeInfo = ContainingTypeInfo.Create(methodSymbol);
        MethodInfo methodInfo       = MethodInfo.Create(methodSymbol, template);

        return new TemplateFormatMethod(typeInfo, methodInfo);
    }
}
