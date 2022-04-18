// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator;

public partial class NamedFormatGenerator
{
    private const string AttributeCode = @$"[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class {NamedFormatAttributeName} : Attribute
{{
    public string Template {{ get; }}
    //-------------------------------------------------------------------------
    public {NamedFormatAttributeName}(string template) => this.Template = template;
}}";
    //-------------------------------------------------------------------------
    private static void AddAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        using StringWriter sw           = new();
        using IndentedTextWriter writer = new(sw);

        foreach (string header in s_headers)
        {
            writer.WriteLine(header);
        }
        writer.WriteLine();

        writer.WriteLine("using System;");
        writer.WriteLine("using System.ComponentModel;");
        writer.WriteLine("using System.CodeDom.Compiler;");
        writer.WriteLine();
        writer.WriteLine($"[{s_generatedCodeAttribute}]");
        writer.WriteLine("[EditorBrowsable(EditorBrowsableState.Always)]");
        writer.WriteLine(AttributeCode);

        string code = sw.ToString();
        context.AddSource("NamedFormatAttributes.g.cs", code);
    }
}
