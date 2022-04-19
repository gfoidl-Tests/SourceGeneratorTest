// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Models;

internal record struct MethodInfo(string Name, Accessibility Accessibility, bool IsStatic, string Template, ImmutableArray<ParameterInfo> Parameters)
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

        return new MethodInfo(methodSymbol.Name, methodSymbol.DeclaredAccessibility, methodSymbol.IsStatic, template, builder.ToImmutableArray());
    }
}
