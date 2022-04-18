// (c) gfoidl, all rights reserved

#define INCORPORATE_COMPILATION_OPTIONS

using System.Collections.Immutable;
using System.Diagnostics;
using Generator.NamedFormatGenerator.Emitter;
using Generator.NamedFormatGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator.NamedFormatGenerator;

[Generator(LanguageNames.CSharp)]
public partial class NamedFormatGenerator : IIncrementalGenerator
{
    // Name is lowercased
    private const string NamedFormatGeneratorOptimizationEnabledMetadata = "build_property.namedformatgeneratoroptimizationenabled";
    //-------------------------------------------------------------------------
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        context.RegisterPostInitializationOutput(AddAttribute);

        IncrementalValueProvider<ImmutableArray<object?>> codeOrDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _)    => IsSyntaxTargetForGeneration(node, _),
                transform: static (context, _) => GetSemanticTargetForGeneration(context, _)
            )
            .Where(templateFormatMethod => templateFormatMethod is not null)
            // This step isn't necessary. One could further transform the so-far collected data.
            // Here it's just sanitation.
            .Select(static (state, _) =>
            {
                if (state is not TemplateFormatMethod templateFormatMethod)
                {
                    Debug.Assert(state is Diagnostic);
                    return state;
                }

                return state;
            })
            .Collect();

#if !INCORPORATE_COMPILATION_OPTIONS
        context.RegisterImplementationSourceOutput(codeProvider, Emit);
#else

        // To avoid invalidating every generator's output when anything from the compilation
        // changes, we extract from it only things we care about: whether
        // * unsafe code is allowed
        // * file scoped namespaces
        // are allowed, and only that information is then fed into RegisterSourceOutput along with all
        // of the cached generated data from each named format.
        IncrementalValueProvider<(bool AllowUnsafe, string? AssemblyName)> compilationData = context.CompilationProvider
            .Select(static (c, _) => (c.Options is CSharpCompilationOptions { AllowUnsafe: true }, c.AssemblyName));

        IncrementalValueProvider<bool> configOptionsData = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                bool optimizeForSpeed = false;

                if (options.GlobalOptions.TryGetValue(NamedFormatGeneratorOptimizationEnabledMetadata, out string? value))
                {
                    optimizeForSpeed = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                return optimizeForSpeed;
            });

        var combined = codeOrDiagnostics
            .Combine(compilationData)
            .Combine(configOptionsData);

        context.RegisterSourceOutput(combined, static (context, compilationData) =>
        {
            ImmutableArray<object?> results = compilationData.Left.Left;
            var (allowUnsafe, assemblyName) = compilationData.Left.Right;
            bool optimizeForSpeed           = compilationData.Right;

            bool allFailures                                     = true;
            ImmutableArray<TemplateFormatMethod>.Builder builder = ImmutableArray.CreateBuilder<TemplateFormatMethod>();

            // Report any top-level diagnostics.
            foreach (object? result in results)
            {
                if (result is Diagnostic d)
                {
                    context.ReportDiagnostic(d);
                }
                else if (result is TemplateFormatMethod templateFormatMethod)
                {
                    allFailures = false;
                    builder.Add(templateFormatMethod);
                }
                else
                {
                    throw new InvalidOperationException("Should not be here");
                }
            }

            if (allFailures)
            {
                return;
            }

            ImmutableArray<TemplateFormatMethod> templateFormatMethods = builder.ToImmutableArray();
            EmitterOptions emitterOptions                              = new(allowUnsafe, optimizeForSpeed);
            NamedFormatGeneratorEmitter emitter                        = NamedFormatGeneratorEmitter.Create(emitterOptions);

            emitter.Emit(context, templateFormatMethods);
        });
#endif
    }
}
