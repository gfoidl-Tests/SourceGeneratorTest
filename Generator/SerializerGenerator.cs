using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class SerializerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            MySyntaxReceiver? syntaxReceiver = context.SyntaxReceiver as MySyntaxReceiver;
            ClassDeclarationSyntax? userClass = syntaxReceiver?.ClassToAugment;

            if (userClass is null)
            {
                return;
            }

            StringBuilder sb = new();

            sb.AppendLine($@"
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable

namespace SourceGeneratorTest
{{
    [CompilerGenerated]
    public partial class {userClass.Identifier}
    {{
        public void Serialize(IEnumerable<Person> values)
        {{
            //@Impl
        }}
    }}
}}
");

            SourceText sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
            context.AddSource("SimpleSerializer.generated", sourceText);
        }

        private class MySyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax? ClassToAugment { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds && cds.Identifier.ValueText == "SimpleSerializer")
                {
                    this.ClassToAugment = cds;
                }
            }
        }
    }
}
