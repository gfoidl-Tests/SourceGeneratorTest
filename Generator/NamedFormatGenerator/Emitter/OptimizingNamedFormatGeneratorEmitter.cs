// (c) gfoidl, all rights reserved

using System.CodeDom.Compiler;
using Generator.NamedFormatGenerator.Models;

namespace Generator.NamedFormatGenerator.Emitter;

internal sealed class OptimizingNamedFormatGeneratorEmitter : NamedFormatGeneratorEmitter
{
    public OptimizingNamedFormatGeneratorEmitter(EmitterOptions options) : base(options)
    { }
    //-------------------------------------------------------------------------
    protected override void EmitMethodBody(IndentedTextWriter writer, MethodInfo methodInfo)
    {
        writer.WriteLine(@"return ""42"";");
    }
}
