// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.NamedFormatGenerator.Models;

internal readonly record struct ParameterInfo(string Name, ITypeSymbol Type);
