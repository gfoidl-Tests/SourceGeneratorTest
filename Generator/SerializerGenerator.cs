using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class SerializerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SerializationGeneratorSyntaxReceiver());
        }
        //---------------------------------------------------------------------
        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            if (context.SyntaxReceiver is not SerializationGeneratorSyntaxReceiver serializationGeneratorSyntaxReceiver)
                return;

            ClassDeclarationSyntax? serializationClass             = serializationGeneratorSyntaxReceiver.ClassToAugment;
            List<InvocationExpressionSyntax>? candidateInvocations = serializationGeneratorSyntaxReceiver.CandidateInvocations;

            if (candidateInvocations is null)
                return;

            if (context.Compilation is not CSharpCompilation csharpCompilation)
                return;

            var serializerInvocations = GetSerializerInvocations(csharpCompilation, candidateInvocations, context.CancellationToken);
            var typesToSerialize      = GetTypesToSerialize(context, csharpCompilation, serializerInvocations, context.CancellationToken);

            string source         = Generate(serializationClass, csharpCompilation, typesToSerialize);
            SourceText sourceText = SourceText.From(source, Encoding.UTF8);
            context.AddSource("SimpleSerializer.generated", sourceText);
        }
        //---------------------------------------------------------------------
        private static IEnumerable<InvocationExpressionSyntax> GetSerializerInvocations(
            CSharpCompilation                       csharpCompilation,
            IEnumerable<InvocationExpressionSyntax> canditateInvocations,
            CancellationToken                       cancellationToken)
        {
            foreach (InvocationExpressionSyntax canditateInvocation in canditateInvocations)
            {
                // Nullability already checked in SerializationGeneratorSyntaxReceiver
                MemberAccessExpressionSyntax maes = (canditateInvocation.Expression as MemberAccessExpressionSyntax)!;
                IdentifierNameSyntax ins          = (maes.Expression as IdentifierNameSyntax)!;

                SemanticModel semanticModel = csharpCompilation.GetSemanticModel(ins.SyntaxTree);
                TypeInfo type               = semanticModel.GetTypeInfo(ins, cancellationToken);

                if (type.Type?.Name == "SimpleSerializer")
                {
                    yield return canditateInvocation;
                }
            }
        }
        //---------------------------------------------------------------------
        private static readonly DiagnosticDescriptor s_argumentCountMismatch = new DiagnosticDescriptor(
            id                : "SER001",
            title             : "There must be exactely one argument",
            messageFormat     : "There are {0} arguments, but only one is expected",
            category          : "SimpleSerializer",
            defaultSeverity   : DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        //---------------------------------------------------------------------
        private static HashSet<INamedTypeSymbol> GetTypesToSerialize(
            GeneratorExecutionContext               context,
            CSharpCompilation                       csharpCompilation,
            IEnumerable<InvocationExpressionSyntax> invocations,
            CancellationToken                       cancellationToken)
        {
            HashSet<INamedTypeSymbol> hashSet = new(EqualityComparer<INamedTypeSymbol>.Default);

            foreach (InvocationExpressionSyntax invocationExpression in invocations)
            {
                SeparatedSyntaxList<ArgumentSyntax> arguments = invocationExpression.ArgumentList.Arguments;

                if (arguments.Count != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_argumentCountMismatch, Location.None, arguments.Count));
                    continue;
                }

                ArgumentSyntax argument = arguments.First();

                if (argument.Expression is not IdentifierNameSyntax ins)
                    continue;

                SemanticModel semanticModel = csharpCompilation.GetSemanticModel(ins.SyntaxTree);

                if (semanticModel.GetTypeInfo(ins, cancellationToken).Type is not INamedTypeSymbol typeSymbol)
                    continue;

                if (typeSymbol.TypeArguments.Length != 1)
                    continue;

                if (typeSymbol.TypeArguments[0] is INamedTypeSymbol typeArg)
                {
                    hashSet.Add(typeArg);
                }
            }

            return hashSet;
        }
        //---------------------------------------------------------------------
        private static string Generate(
            ClassDeclarationSyntax?        serializationClass,
            CSharpCompilation             csharpCompilation,
            IEnumerable<INamedTypeSymbol> typesToSerialize)
        {
            StringBuilder sb = new();
            WriteStart(sb, serializationClass);

            foreach (INamedTypeSymbol typeToSerialize in typesToSerialize)
            {
                WriteMethod(sb, csharpCompilation, typeToSerialize);
            }

            WriteEnd(sb);
            return sb.ToString();
        }
        //---------------------------------------------------------------------
        private static void WriteStart(StringBuilder sb, ClassDeclarationSyntax? serializationClass)
        {
            sb.Append($@"using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable

namespace SourceGeneratorTest
{{
    [CompilerGenerated]
    public partial class {serializationClass?.Identifier.ToString() ?? "SimpleSerializer"}
    {{");
        }
        //---------------------------------------------------------------------
        private static void WriteMethod(StringBuilder sb, CSharpCompilation csharpCompilation, INamedTypeSymbol typeToSerialize)
        {
            sb.Append($@"
        public void Serialize(IEnumerable<{typeToSerialize.Name}> items)
        {{
            if (items is null) throw new ArgumentNullException(nameof(items));

            Console.WriteLine(""");

            ISymbol[] properties = typeToSerialize.GetMembers()
                .Where(s => s.Kind == SymbolKind.Property && s.DeclaredAccessibility == Accessibility.Public)
                .ToArray();

            foreach (ISymbol property in properties)
            {
                sb.Append(property.Name).Append("\\t");
            }

            sb.AppendLine("\");");

            sb.Append($@"
            foreach ({typeToSerialize.Name} item in items)
            {{
                Console.WriteLine($""");

            foreach (ISymbol property in properties)
            {
                sb.Append("{item.").Append(property.Name).Append("}\\t");
            }

            sb.Append(@""");
            }");

            sb.Append(@"
        }");
        }
        //---------------------------------------------------------------------
        private static void WriteEnd(StringBuilder sb)
        {
            sb.AppendLine(@"
    }
}");
        }
        //---------------------------------------------------------------------
        private class SerializationGeneratorSyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax? ClassToAugment { get; private set; }
            public List<InvocationExpressionSyntax>? CandidateInvocations { get; private set; }
            //-----------------------------------------------------------------
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds && cds.Identifier.ValueText == "SimpleSerializer")
                {
                    this.ClassToAugment = cds;
                }
                else if (syntaxNode is InvocationExpressionSyntax ies)
                {
                    if (ies.Expression           is not MemberAccessExpressionSyntax maes) return;
                    if (maes.Name                is not IdentifierNameSyntax ins) return;
                    if (ins.Identifier.ValueText is not "Serialize") return;

                    this.CandidateInvocations ??= new List<InvocationExpressionSyntax>();
                    this.CandidateInvocations.Add(ies);
                }
            }
        }
    }
}
