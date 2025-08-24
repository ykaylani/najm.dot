using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public static class Sorter
{
    [BurstCompile]
    public static void QS(NativeArray<ulong> array, int lo, int hi, NativeArray<double3> positions, NativeArray<double3> velocities,  NativeArray<double> masses)
    {
        if (lo < hi)
        {
            int pivot = Partition(array, lo, hi, positions, velocities, masses);
            
            QS(array, lo, pivot - 1, positions, velocities, masses);
            QS(array, pivot + 1, hi, positions, velocities, masses);
        }
    }
    
    [BurstCompile]
    static int Partition(NativeArray<ulong> array, int lo, int hi, NativeArray<double3> positions, NativeArray<double3> velocities,  NativeArray<double> masses)
    {
        ulong pivot = array[hi];
        int i = lo - 1;
        
        for (int j = lo; j < hi; j++)
        {
            if (array[j] <= pivot)
            {
                i += 1;
                (array[i], array[j]) = (array[j], array[i]);
                (positions[i], positions[j]) = (positions[j], positions[i]);
                (masses[i], masses[j]) = (masses[j], masses[i]);
                (velocities[i], velocities[j]) = (velocities[j], velocities[i]);
            }
        }
        
        (array[i + 1], array[hi]) = (array[hi], array[i + 1]);
        (positions[i + 1], positions[hi]) = (positions[hi], positions[i + 1]);
        (masses[i + 1], masses[hi]) = (masses[hi], masses[i + 1]);
        (velocities[i + 1], velocities[hi]) = (velocities[hi], velocities[i + 1]);
        return i + 1;
    }

    static int Radix()
    {
        throw new NotImplementedException();
    }
}
