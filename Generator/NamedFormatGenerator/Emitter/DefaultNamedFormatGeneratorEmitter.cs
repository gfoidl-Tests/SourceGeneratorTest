// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Generator.NamedFormatGenerator.Models;

namespace Generator.NamedFormatGenerator.Emitter;

internal sealed class DefaultNamedFormatGeneratorEmitter : NamedFormatGeneratorEmitter
{
    public DefaultNamedFormatGeneratorEmitter(EmitterOptions options) : base(options)
    { }
    //-------------------------------------------------------------------------
    protected override void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
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
}
