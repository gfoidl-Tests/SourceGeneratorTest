using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    class DoubleConstantGenerator : ISourceGenerator
    {
        private const string AttributeText = @"using System;

#nullable enable

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class DoubleConstantAttribute : Attribute
{
    public double  Value    { get; }
    public string? Equation { get; }

    public DoubleConstantAttribute(double value)    => this.Value    = value;
    public DoubleConstantAttribute(string equation) => this.Equation = equation;
}
";
        //---------------------------------------------------------------------
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
        //---------------------------------------------------------------------
        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            SourceText attributeSource = SourceText.From(AttributeText, Encoding.UTF8);
            context.AddSource("DoubleConstantAttribute.generated", attributeSource);

            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
                return;

            CSharpParseOptions options         = (CSharpParseOptions)((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;
            SyntaxTree attributeTextSyntaxTree = CSharpSyntaxTree.ParseText(attributeSource, options);
            Compilation compilation            = context.Compilation.AddSyntaxTrees(attributeTextSyntaxTree);

            if (compilation.GetTypeByMetadataName("DoubleConstantAttribute") is not INamedTypeSymbol attributeSymbol) return;
            if (compilation.GetTypeByMetadataName("System.Double")           is not INamedTypeSymbol doubleSymbol)    return;

            StringBuilder sb = new();

            var methods               = GetMethods(compilation, syntaxReceiver.CandidateMethods, attributeSymbol, doubleSymbol);
            var methodsGroupedByClass = methods.GroupBy(m => m.containingType, m => (m.methodName, m.value, m.accessibility));

            foreach (var grp in methodsGroupedByClass)
            {
                INamedTypeSymbol containingType = grp.Key;
                string generatedSource          = Generate(sb, containingType, grp);
                string fileName                 = GetFileName(sb, containingType);

                context.AddSource(fileName, SourceText.From(generatedSource, Encoding.UTF8));
            }
        }
        //---------------------------------------------------------------------
        private static IEnumerable<(INamedTypeSymbol containingType, string methodName, double value, Accessibility accessibility)> GetMethods(
            Compilation                   compilation,
            List<MethodDeclarationSyntax> canditateMethods,
            INamedTypeSymbol              attributeSymbol,
            INamedTypeSymbol              doubleSymbol)
        {
            foreach (MethodDeclarationSyntax canditateMethod in canditateMethods)
            {
                if (!IsStaticPartial(canditateMethod))
                    continue;

                if (canditateMethod.ParameterList.Parameters.Count != 0)
                    continue;

                SemanticModel semanticModel = compilation.GetSemanticModel(canditateMethod.SyntaxTree);

                if (!ReturnsDouble(semanticModel, canditateMethod, doubleSymbol))
                    continue;

                if (!TryGetDoubleConstantAttribute(semanticModel, canditateMethod, attributeSymbol, out var tmp))
                    continue;

                var (value, containingType, accessibility) = tmp;
                yield return (containingType, canditateMethod.Identifier.ValueText, value, accessibility);
            }
        }
        //---------------------------------------------------------------------
        private static bool IsStaticPartial(MemberDeclarationSyntax mds)
        {
            bool isStatic  = false;
            bool isPartial = false;

            foreach (SyntaxToken modifier in mds.Modifiers)
            {
                isStatic  |= modifier.Text == "static";
                isPartial |= modifier.Text == "partial";
            }

            return isStatic && isPartial;
        }
        //---------------------------------------------------------------------
        private static bool ReturnsDouble(SemanticModel semanticModel, MethodDeclarationSyntax mds, INamedTypeSymbol doubleSymbol)
        {
            return semanticModel.GetSymbolInfo(mds.ReturnType).Symbol is INamedTypeSymbol s
                && SymbolEqualityComparer.Default.Equals(s, doubleSymbol);
        }
        //---------------------------------------------------------------------
        private static bool TryGetDoubleConstantAttribute(
            SemanticModel           semanticModel,
            MethodDeclarationSyntax mds,
            INamedTypeSymbol        attributeSymbol,
            out (double value, INamedTypeSymbol containingType, Accessibility accessibility) doubleConstAttribute)
        {
            if (semanticModel.GetDeclaredSymbol(mds) is IMethodSymbol methodSymbol)
            {
                foreach (AttributeData attribute in methodSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
                    {
                        ImmutableArray<TypedConstant> args = attribute.ConstructorArguments;
                        if (args.Length != 1) continue;

                        if (args[0].Value is string equation)
                        {
                            if (double.TryParse(equation, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                            {
                                doubleConstAttribute = (value, methodSymbol.ContainingType, methodSymbol.DeclaredAccessibility);
                                return true;
                            }
                        }
                        else if (args[0].Value is double value)
                        {
                            doubleConstAttribute = (value, methodSymbol.ContainingType, methodSymbol.DeclaredAccessibility);
                            return true;
                        }
                    }
                }
            }

            doubleConstAttribute = default;
            return false;
        }
        //---------------------------------------------------------------------
        private static string Generate(
            StringBuilder    sb,
            INamedTypeSymbol containingType,
            IEnumerable<(string methodName, double value, Accessibility accessibility)> methods)
        {
            sb.Clear();

            bool hasNamespace = !string.IsNullOrWhiteSpace(containingType.ContainingNamespace.Name);

            if (hasNamespace)
            {
                sb.Append($@"using System;

namespace {containingType.ContainingNamespace}
{{");
            }

            sb.Append(@$"
    partial class {containingType.Name}
    {{");

            foreach (var (methodName, value, accessibility) in methods)
            {
                string randomSuffix = GetRandomSuffix();
                string constName    = $"{methodName}Long_{randomSuffix}";
                string modifier     = AccessibilityText(accessibility);

                sb.Append($@"
        private const long {constName} = {BitConverter.DoubleToInt64Bits(value)};
        {modifier} static partial double {methodName}() => BitConverter.Int64BitsToDouble({constName});");
            }

            sb.Append(@"
    }
");

            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
        //---------------------------------------------------------------------
        private static readonly Random s_rnd = new Random();

        private static unsafe string GetRandomSuffix()
        {
            const int suffixLength = 4;

            char* buffer = stackalloc char[suffixLength + 1];
            buffer[suffixLength] = '\0';

            for (int i = 0; i < suffixLength; ++i)
            {
                buffer[i] = (char)s_rnd.Next('a', 'z' + 1);
            }

            return new string(buffer);
        }
        //---------------------------------------------------------------------
        private static string AccessibilityText(Accessibility accessibility)
            => accessibility switch
            {
                Accessibility.Private              => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Protected            => "protected",
                Accessibility.Internal             => "internal",
                Accessibility.ProtectedOrInternal  => "protected internal",
                Accessibility.Public               => "public",
                _                                  => throw new InvalidOperationException()
            };
        //---------------------------------------------------------------------
        private static string GetFileName(StringBuilder sb, INamedTypeSymbol type)
        {
            sb.Clear();

            foreach (SymbolDisplayPart part in type.ContainingNamespace.ToDisplayParts())
            {
                if (part.Symbol is { Name: string name } && !string.IsNullOrWhiteSpace(name))
                {
                    sb.Append(name).Append('_');
                }
            }

            sb.Append(type.Name).Append("_DoubleConstant.cs");

            return sb.ToString();
        }
        //---------------------------------------------------------------------
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();
            //-----------------------------------------------------------------
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Any method with at least one attribute is candidate
                if (syntaxNode is MethodDeclarationSyntax mds && mds.AttributeLists.Count > 0)
                {
                    this.CandidateMethods.Add(mds);
                }
            }
        }
    }
}
