using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class DumpExportsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }
        //---------------------------------------------------------------------
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)
                return;

            StringBuilder sb = new();

            sb.Append(@"using System;
using System.Runtime.CompilerServices;

#nullable enable

[CompilerGenerated]
public static class ExportDumper
{
    public static void Dump()
    {");

            foreach (BaseTypeDeclarationSyntax tds in syntaxReceiver.Types)
            {
                sb.Append($@"
        Console.WriteLine(""type: {GetType(tds)}\tname: {tds.Identifier}\tfile: {Path.GetFileName(tds.SyntaxTree.FilePath)}"");");
            }

            sb.AppendLine(@"
    }
}");

            SourceText sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);
            context.AddSource("DumpExports.generated", sourceText);

            static string GetType(BaseTypeDeclarationSyntax tds) => tds switch
            {
                ClassDeclarationSyntax  => "class",
                RecordDeclarationSyntax => "record",
                StructDeclarationSyntax => "struct",
                _                       => "-"
            };
        }
        //---------------------------------------------------------------------
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<BaseTypeDeclarationSyntax> Types { get; } = new List<BaseTypeDeclarationSyntax>();
            //-----------------------------------------------------------------
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is BaseTypeDeclarationSyntax sn)
                {
                    this.Types.Add(sn);
                }
            }
        }
    }
}
