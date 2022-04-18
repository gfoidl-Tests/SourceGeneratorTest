// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Generator.NamedFormatGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Emitter;

internal readonly record struct EmitterOptions(bool AllowUnsafe, bool OptimizeForSpeed);
//-----------------------------------------------------------------------------
internal abstract class NamedFormatGeneratorEmitter
{
    private static readonly string[] s_namespaces =
    {
        "System",
        "System.CodeDom.Compiler",
        "System.ComponentModel",
        "System.Runtime.CompilerServices"
    };
    //-------------------------------------------------------------------------
    protected readonly EmitterOptions _emitterOptions;
    //-------------------------------------------------------------------------
    protected NamedFormatGeneratorEmitter(EmitterOptions emitterOptions)
    {
        _emitterOptions = emitterOptions;
    }
    //-------------------------------------------------------------------------
    public static NamedFormatGeneratorEmitter Create(EmitterOptions options)
    {
        if (options.OptimizeForSpeed)
        {
            return new OptimizingNamedFormatGeneratorEmitter(options);
        }

        return new DefaultNamedFormatGeneratorEmitter(options);
    }
    //-------------------------------------------------------------------------
    public virtual void Emit(SourceProductionContext context, ImmutableArray<TemplateFormatMethod> methods)
    {
        //System.Diagnostics.Debugger.Launch();

        StringBuilder buffer = new();

        IEnumerable<IGrouping<ContainingTypeInfo, MethodInfo>> methodGroups = methods.GroupBy(m => m.Type, m => m.Method);
        foreach (IGrouping<ContainingTypeInfo, MethodInfo> methodGroup in methodGroups)
        {
            ContainingTypeInfo containingType = methodGroup.Key;
            string code                       = this.GenerateCode(containingType, methodGroup, buffer);
            string fileName                   = GetFilename(containingType, buffer);

            context.AddSource(fileName, code);
        }
    }
    //-------------------------------------------------------------------------
    private static string GetFilename(ContainingTypeInfo typeInfo, StringBuilder buffer)
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
    protected virtual string GenerateCode(ContainingTypeInfo typeInfo, IEnumerable<MethodInfo> methods, StringBuilder buffer)
    {
        buffer.Clear();
        using StringWriter sw           = new(buffer);
        using IndentedTextWriter writer = new(sw);

        foreach (string header in Globals.Headers)
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
            this.EmitMethod(writer, method);
        }

        writer.Indent--;
        writer.WriteLine("}");

        return sw.ToString();
    }
    //-------------------------------------------------------------------------
    protected virtual void EmitMethod(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        writer.WriteLine($"[{Globals.GeneratedCodeAttribute}]");
        writer.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");

        if (_emitterOptions.AllowUnsafe)
        {
            writer.WriteLine("[SkipLocalsInit]");
        }

        writer.Write(AccessibilityText(methodInfo.Accessibility));
        writer.Write($" static partial string {methodInfo.Name}(");
        this.EmitParameters(writer, methodInfo);
        writer.WriteLine(")");
        writer.WriteLine("{");
        writer.Indent++;
        {
            this.EmitMethodBody(writer, methodInfo);
        }
        writer.Indent--;
        writer.WriteLine("}");
    }
    //-------------------------------------------------------------------------
    protected virtual void EmitParameters(IndentedTextWriter writer, MethodInfo methodInfo)
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
    protected abstract void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo);
    //-------------------------------------------------------------------------
    protected static string AccessibilityText(Accessibility accessibility) => accessibility switch
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
