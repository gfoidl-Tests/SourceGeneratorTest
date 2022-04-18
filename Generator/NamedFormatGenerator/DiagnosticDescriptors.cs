// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator;

internal static class DiagnosticDescriptors
{
    private const string Category = "NamedFormatGenerator";
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor AttributeArgumentCountMismatch { get; } = new DiagnosticDescriptor(
        id                : "NFG0001",
        title             : "Argument count mismatch",
        messageFormat     : $"The {NamedFormatGenerator.NamedFormatAttributeName} expects exactly 1 argument for the format.",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor TemplateIsNullOrEmpty { get; } = new DiagnosticDescriptor(
        id                : "NFG0002",
        title             : "Template is null or empty",
        messageFormat     : "The given template must not be null or an empty string.",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor TemplateNotWellFormed { get; } = new DiagnosticDescriptor(
        id                : "NFG0003",
        title             : "Template not well-formed",
        messageFormat     : "The template's open ('{') and close ('}') brackets do not match.",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor TemplateHolesDontMatchParameterCount { get; } = new DiagnosticDescriptor(
        id                : "NFG0004",
        title             : "Template placeholders don't match parameter count",
        messageFormat     : "The template has {0} placeholders, but {1} parameters are given.",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
