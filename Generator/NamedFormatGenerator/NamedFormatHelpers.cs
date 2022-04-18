// (c) gfoidl, all rights reserved

#if NET7_0_OR_GREATER

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal static class NamedFormatHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyLiteral(ReadOnlySpan<char> literal, Span<char> buffer)
    {
        ref ushort literalRef = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(literal));
        ref ushort bufferRef  = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(buffer));

        nuint i = 0;
        nuint n = (uint)literal.Length;

        if (Vector128.IsHardwareAccelerated)
        {
            if (n >= (uint)Vector128<ushort>.Count)
            {
                do
                {
                    Vector128<ushort> vec = Vector128.LoadUnsafe(ref literalRef, i);
                    vec.StoreUnsafe(ref bufferRef, i);

                    i += (uint)Vector128<ushort>.Count;
                } while (i <= n - (uint)Vector128<ushort>.Count);
            }
        }
        else
        {
            if (n >= sizeof(long) / sizeof(ushort))
            {
                do
                {
                    long tmp = Unsafe.ReadUnaligned<long>(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref literalRef, i)));
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref bufferRef, i)), tmp);

                    i += sizeof(long) / sizeof(ushort);
                } while (i <= n - sizeof(long) / sizeof(ushort));
            }
        }

        if (i <= (n - sizeof(long) / sizeof(ushort)))
        {
            long tmp = Unsafe.ReadUnaligned<long>(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref literalRef, i)));
            Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref bufferRef, i)), tmp);
            i += sizeof(long) / sizeof(ushort);
        }

        if (i <= (n - sizeof(int) / sizeof(ushort)))
        {
            int tmp = Unsafe.ReadUnaligned<int>(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref literalRef, i)));
            Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref bufferRef, i)), tmp);
            i += sizeof(int) / sizeof(ushort);
        }

        if (i < n)
        {
            Unsafe.Add(ref bufferRef, i) = Unsafe.Add(ref literalRef, i);
            i++;
        }

        return (int)i;
    }
}

#endif
