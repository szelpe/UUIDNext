﻿using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace UUIDNext.Tools
{
    public static class RandomNumberGeneratorPolyfill
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillWithRandom(this Span<byte> span)
        {
#if NET472_OR_GREATER
            using var rng = RandomNumberGenerator.Create();
            var tempBytes = new byte[span.Length];
            rng.GetBytes(tempBytes);
            tempBytes.CopyTo(span);
#else
            RandomNumberGenerator.Fill(span);
#endif
        }
    }
}