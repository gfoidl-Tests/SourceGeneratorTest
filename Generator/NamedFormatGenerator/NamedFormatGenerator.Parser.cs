// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Generator.NamedFormatGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Generator.NamedFormatGenerator;

public partial class NamedFormatGenerator
{
    internal const string NamedFormatAttributeName = "NamedFormatTemplateAttribute";
    //-------------------------------------------------------------------------
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _)
        // We don't have a semantic model here, so the best we can do is say whether there are any attributes.
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
    //-------------------------------------------------------------------------
    /// <summary>
    /// Returns <c>null</c> if nothing to do, <see cref="Diagnostic"/> if there's an error to report,
    /// or <see cref="TemplateFormatMethod"/> if the type was analyzed successfully.
    /// </summary>
    private static object? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken _)
    {
        SemanticModel semanticModel                     = context.SemanticModel;
        MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

        Compilation compilation                      = semanticModel.Compilation;
        INamedTypeSymbol? namedFormatAttributeSymbol = compilation.GetBestTypeByMetadataName(NamedFormatAttributeName);

        if (namedFormatAttributeSymbol is null)
        {
            // Required types aren't available
            return null;
        }

        if (semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not { } methodSymbol) return null;
        if (!methodSymbol.IsPartialDefinition || !methodSymbol.IsStatic)                      return null;
        if (!ReturnsString(methodSymbol))                                                     return null;

        object? templateOrDiagnostic = GetNamedFormatTemplateAttributeOrDiagnostic(methodSymbol, methodDeclarationSyntax);

        if (templateOrDiagnostic is string template)
        {
            if (ValidateTemplate(template, methodSymbol, methodDeclarationSyntax, out Diagnostic? diagnostic))
            {
                return TemplateFormatMethod.Create(methodSymbol, template);
            }

            templateOrDiagnostic = diagnostic;
        }

        // Diagnostic or null (which is filtered out later)
        return templateOrDiagnostic;
    }
    //-------------------------------------------------------------------------
    private static bool ReturnsString(IMethodSymbol methodSymbol)
    {
        //return methodSymbol.ReturnType is INamedTypeSymbol namedTypeSymbol
        //    && namedTypeSymbol.ToDisplayString() == "System.String";

        return methodSymbol.ReturnType.SpecialType == SpecialType.System_String;
    }
    //-------------------------------------------------------------------------
    private static object? GetNamedFormatTemplateAttributeOrDiagnostic(IMethodSymbol methodSymbol, CSharpSyntaxNode syntaxNode)
    {
        foreach (AttributeData attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == NamedFormatAttributeName)
            {
                ImmutableArray<TypedConstant> ctorArgs = attribute.ConstructorArguments;

                if (ctorArgs.Length != 1)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.AttributeArgumentCountMismatch, syntaxNode.GetLocation());
                }

                if (ctorArgs[0].Value is string template)
                {
                    return template;
                }
            }
        }

        return null;
    }
    //-------------------------------------------------------------------------
    private static bool ValidateTemplate(
        string           template,
        IMethodSymbol    methodSymbol,
        CSharpSyntaxNode syntaxNode,
        [NotNullWhen(false)] out Diagnostic? diagnostic)
    {
        // Further analysis could be done here. In case of failure, report a diagnostic.
        // TODO: validate names of parameters to match the holes

        if (string.IsNullOrWhiteSpace(template))
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.TemplateIsNullOrEmpty, syntaxNode.GetLocation());
            return false;
        }

        if (!TemplateValidator.ValidateTemplate(template, out int noOfHoles))
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.TemplateNotWellFormed, syntaxNode.GetLocation());
            return false;
        }

        if (noOfHoles != methodSymbol.Parameters.Length)
        {
            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.TemplateHolesDontMatchParameterCount,
                syntaxNode.GetLocation(),
                noOfHoles, methodSymbol.Parameters.Length);

            return false;
        }

        diagnostic = null;
        return true;
    }
}
