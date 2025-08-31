using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/*#if !USE_FLOAT
    using Precision = System.Single;
    using Precision3 = Unity.Mathematics.float3;
    using Precision4x2 = Unity.Mathematics.float4x2;
#else
    using Precision = System.Double;
    using Precision3 = Unity.Mathematics.double3;
    using Precision4x2 = Unity.Mathematics.double4x2;
#endif*/

using Precision = System.Double;
using Precision3 = Unity.Mathematics.double3;
using Precision4 = Unity.Mathematics.double4;
using Precision4x2 = Unity.Mathematics.double4x2;

public static class Encoder
{
    private const int bits = 21;
    
    [BurstCompile] [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Morton(Precision3 position, Precision3 minBounds, Precision3 maxBounds)
    {
        Precision3 normalized = (position - minBounds) / (maxBounds - minBounds);
        
        ulong x = (ulong)(normalized.x * ((1ul << bits) - 1));
        ulong y = (ulong)(normalized.y * ((1ul << bits) - 1));
        ulong z = (ulong)(normalized.z * ((1ul << bits) - 1));

        return SpreadBits(x) | (SpreadBits(y) << 1) | (SpreadBits(z) << 2);
    }

    [BurstCompile] [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong SpreadBits(ulong spreading)
    {
        spreading &= 0x1FFFFF;
        spreading = (spreading | (spreading << 32)) & 0x001F00000000FFFFul;
        spreading = (spreading | (spreading << 16)) & 0x001F0000FF0000FFul;
        spreading = (spreading | (spreading << 8)) & 0x100F00F00F00F00Ful;
        spreading = (spreading | (spreading << 4)) & 0x10C30C30C30C30C3ul;
        spreading = (spreading | (spreading << 2)) & 0x1249249249249249ul;
        return spreading;
    }

    [BurstCompile]
    public static int ChildIndex(ulong encoded, int depth)
    {
        if (depth < 0) return 0;
        int shift = 3 * (bits - depth);
        return (int)((encoded >> shift) & 0x7ul);
    }
    
    [BurstCompile]
    public struct BodyEncoding : IJobParallelFor
    {
        public Precision3 maxBounds;
        [ReadOnly] public NativeArray<Precision3> positions;
        [WriteOnly] public NativeArray<ulong> encodings;
    
        public void Execute(int body)
        {
            encodings[body] = Morton(positions[body], -maxBounds, maxBounds);
        }
    }
}