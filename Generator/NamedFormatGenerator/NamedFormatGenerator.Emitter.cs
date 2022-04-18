// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator;

public partial class NamedFormatGenerator
{
    private static readonly string[] s_namespaces =
    {
        "System",
        "System.CodeDom.Compiler",
        "System.ComponentModel",
        "System.Runtime.CompilerServices"
    };
    //-------------------------------------------------------------------------
    private static void Emit(SourceProductionContext context, ImmutableArray<TemplateFormatMethod> methods, bool allowUnsafe)
    {
        //System.Diagnostics.Debugger.Launch();

        StringBuilder buffer                                      = new();
        IEnumerable<IGrouping<TypeInfo, MethodInfo>> methodGroups = methods.GroupBy(m => m.Type, m => m.Method);

        foreach (IGrouping<TypeInfo, MethodInfo> methodGroup in methodGroups)
        {
            TypeInfo containingType = methodGroup.Key;
            string code             = GenerateCode(containingType, methodGroup, allowUnsafe, buffer);
            string fileName         = GetFilename(containingType, buffer);

            context.AddSource(fileName, code);
        }
    }
    //-------------------------------------------------------------------------
    private static string GetFilename(TypeInfo typeInfo, StringBuilder buffer)
    {
        buffer.Clear();

        if (typeInfo.Namespace is { } ns)
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }
        buffer.Append(typeInfo.Name);
        buffer.Append("_NamedFormat.g.cs");

        return buffer.ToString();
    }
    //-------------------------------------------------------------------------
    private static string GenerateCode(TypeInfo typeInfo, IEnumerable<MethodInfo> methods, bool allowUnsafe, StringBuilder buffer)
    {
        buffer.Clear();
        using StringWriter sw           = new(buffer);
        using IndentedTextWriter writer = new(sw);

        foreach (string header in s_headers)
        {
            writer.WriteLine(header);
        }
        writer.WriteLine();

        foreach (string ns in s_namespaces)
        {
            writer.WriteLine($"using {ns};");
        }
        writer.WriteLine();

        if (typeInfo.Namespace is not null)
        {
            writer.WriteLine($"namespace {typeInfo.Namespace};");
            writer.WriteLine();
        }

        writer.Write("partial ");
        if (typeInfo.IsValueType)
        {
            writer.Write("struct ");
        }
        else
        {
            writer.Write("class ");
        }
        writer.WriteLine($"{typeInfo.Name}");
        writer.WriteLine("{");
        writer.Indent++;

        foreach (MethodInfo method in methods)
        {
            EmitMethod(writer, method, allowUnsafe);
        }

        writer.Indent--;
        writer.WriteLine("}");

        return sw.ToString();
    }
    //-------------------------------------------------------------------------
    private static void EmitMethod(IndentedTextWriter writer, MethodInfo methodInfo, bool allowUnsafe)
    {
        writer.WriteLine($"[{s_generatedCodeAttribute}]");
        writer.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");

        if (allowUnsafe)
        {
            writer.WriteLine("[SkipLocalsInit]");
        }

        writer.Write(AccessibilityText(methodInfo.Accessibility));
        writer.Write($" static partial string {methodInfo.Name}(");
        EmitParameters(writer, methodInfo);
        writer.WriteLine(")");
        writer.WriteLine("{");
        writer.Indent++;
        {
            EmitMethodBody(writer, methodInfo);
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
    //-------------------------------------------------------------------------
    private static void EmitParameters(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        for (int i = 0; i < methodInfo.Parameters.Length; ++i)
        {
            ParameterInfo parameter = methodInfo.Parameters[i];

            writer.Write($"{parameter.Type.ToDisplayString()} {parameter.Name}");

            if (i < methodInfo.Parameters.Length - 1)
            {
                writer.Write(", ");
            }
        }
    }
    //-------------------------------------------------------------------------
    private static void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        // Validation is already done, so here we can assume that the template is correct.

        writer.Write("string result = string.Create(null, stackalloc char[128], $\"");

        ReadOnlySpan<char> template              = methodInfo.Template.AsSpan();
        ImmutableArray<ParameterInfo> parameters = methodInfo.Parameters;

        for (int parameterIndex = 0; parameterIndex < parameters.Length; ++parameterIndex)
        {
            int index = template.IndexOf('{');
            if (index < 0)
            {
                goto Exit;
            }
            index++;    // include the {

            writer.Write(template.Slice(0, index).ToString());
            writer.Write(parameters[parameterIndex].Name);
            writer.Write("}");

            index = template.IndexOf('}');
            if (index < 0)
            {
                goto Exit;
            }

            template = template.Slice(index + 1);   // include the }
        }

    Exit:
        writer.WriteLine("\");");
        writer.WriteLine("return result;");
    }
    //-------------------------------------------------------------------------
    private static string AccessibilityText(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public               => "public",
        Accessibility.Protected            => "protected",
        Accessibility.Private              => "private",
        Accessibility.Internal             => "internal",
        Accessibility.ProtectedOrInternal  => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _                                  => throw new InvalidOperationException(),
    };
}
