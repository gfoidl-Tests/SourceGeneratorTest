// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using Generator.NamedFormatGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Emitter;

internal sealed class OptimizingNamedFormatGeneratorEmitter : NamedFormatGeneratorEmitter
{
    private const int ThreshouldForStackAlloc = 128;
    //-------------------------------------------------------------------------
    private static readonly string[] s_additionalNamespaces =
    {
        "System.Buffers"
    };
    //-------------------------------------------------------------------------
    public OptimizingNamedFormatGeneratorEmitter(EmitterOptions options) : base(options)
    { }
    //-------------------------------------------------------------------------
    protected override string[]? AdditionalNamespaces => s_additionalNamespaces;
    //-------------------------------------------------------------------------
    protected override void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        //System.Diagnostics.Debugger.Launch();

        // Validation is already done, so here we can assume that the template is correct.

        if (_emitterOptions.BufferSize <= ThreshouldForStackAlloc)
        {
            writer.WriteLine($"Span<char> buffer = stackalloc char[{ThreshouldForStackAlloc}];");
        }
        else
        {
            writer.WriteLine($"char[] rentArray  = ArrayPool<char>.Shared.Rent({_emitterOptions.BufferSize});");
            writer.WriteLine("Span<char> buffer = rentArray;");
        }
        writer.WriteLine("int written       = 0;");
        writer.WriteLine("int charsWritten  = 0;");
        writer.WriteLine();

        ReadOnlySpan<char> template              = methodInfo.Template.AsSpan();
        ImmutableArray<ParameterInfo> parameters = methodInfo.Parameters;

        for (int parameterIndex = 0; parameterIndex < parameters.Length; ++parameterIndex)
        {
            if (!TryEmitCodeForParameter(writer, ref template, parameters, parameterIndex))
            {
                goto ExitError;
            }

            if (parameterIndex < parameters.Length - 1)
            {
                writer.WriteLine();
            }
        }

        if (!template.IsEmpty)
        {
            writer.WriteLine();
            writer.WriteLine("// Remainder of template");
            EmitLiteral(writer, template, parameterIndex: int.MaxValue, template.Length);
            template = template.Slice(template.Length);
        }
        else
        {
            writer.WriteLine();
        }

        if (_emitterOptions.BufferSize <= ThreshouldForStackAlloc)
        {
            writer.WriteLine("return new string(buffer.Slice(0, written));");
        }
        else
        {
            writer.WriteLine("string result = new string(buffer.Slice(0, written));");
            writer.WriteLine("ArrayPool<char>.Shared.Return(rentArray);");
            writer.WriteLine();
            writer.WriteLine("return result;");
        }

        Debug.Assert(template.IsEmpty);

        return;

    ExitError:
        // Should never happen, but generate code that compiles at least. TODO: find better way
        writer.WriteLine("return \"42\";");
    }
    //-------------------------------------------------------------------------
    private static bool TryEmitCodeForParameter(
        IndentedTextWriter            writer,
        ref ReadOnlySpan<char>        template,
        ImmutableArray<ParameterInfo> parameters,
        int                           parameterIndex)
    {
        ParameterInfo parameter = parameters[parameterIndex];

        writer.WriteLine($"// Parameter {parameterIndex}: {parameter.Name}");

        int index = template.IndexOf('{');
        if (index < 0)
        {
            return false;
        }

        EmitLiteral       (writer, template, parameterIndex, index);
        EmitParameterValue(writer, parameter);

        index = template.IndexOf('}');
        if (index < 0)
        {
            return false;
        }

        template = template.Slice(index + 1);   // include the }

        return true;
    }
    //-------------------------------------------------------------------------
    private static void EmitLiteral(
        IndentedTextWriter writer,
        ReadOnlySpan<char> template,
        int                parameterIndex,
        int                index)
    {
        string literal = template.Slice(0, index).ToString();

        if (literal.Length > 1)
        {
            if (parameterIndex == 0)
            {
                writer.WriteLine($"\"{literal}\".CopyTo(buffer);");
            }
            else
            {
                writer.WriteLine($"\"{literal}\".CopyTo(buffer.Slice(written));");
            }

            writer.WriteLine($"written += \"{literal}\".Length;");
        }
        else
        {
            writer.WriteLine($"buffer[written] = '{literal[0]}';");
            writer.WriteLine("written++;");
        }

        writer.WriteLine();
    }
    //-------------------------------------------------------------------------
    private static void EmitParameterValue(IndentedTextWriter writer, ParameterInfo parameter)
    {
        if (parameter.Type.SpecialType == SpecialType.System_String)
        {
            writer.WriteLine($"{parameter.Name}.CopyTo(buffer.Slice(written));");
            writer.WriteLine($"written += {parameter.Name}.Length;");
        }
        else if (HasInterface(parameter.Type, "ISpanFormattable", "System"))
        {
            writer.WriteLine($"{parameter.Name}.TryFormat(buffer.Slice(written), out charsWritten);");
            writer.WriteLine("written += charsWritten;");
        }
        else if (HasInterface(parameter.Type, "IFormattable", "System"))
        {
            writer.WriteLine("// Value can be written via IFormattable");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine($"string tmp = {parameter.Name}.ToString(null, null);");
            writer.WriteLine("tmp.CopyTo(buffer.Slice(written));");
            writer.WriteLine("written += tmp.Length;");

            writer.Indent--;
            writer.WriteLine("}");
        }
        else
        {
            writer.WriteLine("// No interface for faster formatting found, so just use object.ToString()");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine($"string tmp = {parameter.Name}.ToString();");
            writer.WriteLine("tmp.CopyTo(buffer.Slice(written));");
            writer.WriteLine("written += tmp.Length;");

            writer.Indent--;
            writer.WriteLine("}");
        }
    }
    //-------------------------------------------------------------------------
    private static bool HasInterface(ITypeSymbol typeSymbol, string interfaceName, string ns)
    {
        foreach (INamedTypeSymbol type in typeSymbol.Interfaces)
        {
            if (type.Name == interfaceName && type.ContainingNamespace.Name == ns)
            {
                return true;
            }
        }

        return false;
    }
}
