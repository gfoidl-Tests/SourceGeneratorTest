// (c) gfoidl, all rights reserved

namespace Generator.NamedFormatGenerator.Models;

internal readonly record struct EmitterOptions(bool AllowUnsafe, bool OptimizeForSpeed, int BufferSize = -1);
