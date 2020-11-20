using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class GeneratedAnswerGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            const string source = @"using System.Runtime.CompilerServices;

#nullable enable

namespace GeneratedAnswer
{
    [CompilerGenerated]
    public class Answers
    {
        public int GetAnswer(object? question = null) => 42;
    }
}
";
            SourceText sourceText = SourceText.From(source, Encoding.UTF8);
            context.AddSource("GeneratedAnswer.generated", sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nop
        }
    }
}
