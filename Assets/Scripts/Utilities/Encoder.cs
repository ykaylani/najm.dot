using Unity.Mathematics;
using Unity.Burst;

public static class Encoder
{
    private const int bits = 21;
    
    [BurstCompile]
    public static ulong Morton(double3 position, double3 minBounds, double3 maxBounds)
    {
        double3 normalized = (position - minBounds) / (maxBounds - minBounds);
        
        ulong x = (ulong)(normalized.x * ((1ul << bits) - 1));
        ulong y = (ulong)(normalized.y * ((1ul << bits) - 1));
        ulong z = (ulong)(normalized.z * ((1ul << bits) - 1));

        return SpreadBits(x) | (SpreadBits(y) << 1) | (SpreadBits(z) << 2);
    }

    [BurstCompile]
    static ulong SpreadBits(ulong spreading)
    {
        spreading &= 0x1FFFFF;
        spreading = (spreading | (spreading << 32)) & 0x001F00000000FFFF;
        spreading = (spreading | (spreading << 16)) & 0x001F0000FF0000FF;
        spreading = (spreading | (spreading << 8)) & 0x100F00F00F00F00F;
        spreading = (spreading | (spreading << 4)) & 0x10C30C30C30C30C3;
        spreading = (spreading | (spreading << 2)) & 0x1249249249249249;
        return spreading;
    }

    [BurstCompile]
    public static int ChildIndex(ulong encoded, int depth)
    {
        int shift = 61 - 3 * depth;
        return (int)((encoded >> shift) & 0x7ul);
    }
    
}
