using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

public static class Sorter
{
    [BurstCompile]
    public struct RadixClearHistograms : IJob
    {
        public NativeArray<int> globalHistogram;
        public NativeArray<int> histograms;

        public void Execute()
        {
            for (int i = 0; i < globalHistogram.Length; i++)
            {
                globalHistogram[i] = 0;
            }

            for (int i = 0; i < histograms.Length; i++)
            {
                histograms[i] = 0;
            }
        }
    }


    [BurstCompile]
    public struct RadixLocalHistograms : IJobParallelFor
    {
        [ReadOnly]public NativeArray<ulong> encodings;
        [NativeDisableParallelForRestriction] public NativeArray<int> localHistograms;

        [ReadOnly] public int segments;
        [ReadOnly] public int pass;
        
        public void Execute(int index) 
        {
            int segmentSize = encodings.Length / segments;
            int startIndex = index * segmentSize;
            int endIndex = (index == segments - 1) ? encodings.Length : startIndex + segmentSize;
            
            int histogramStartIndex = index * 256;
            
            for (int i = startIndex; i < endIndex; i++)
            {
                int digit = (int)((encodings[i] >> (pass * 8)) & 0xFF);
                localHistograms[histogramStartIndex + digit] += 1;
            }
        }
        
    }

    [BurstCompile]
    public struct RadixGlobalHistogram : IJob
    {
        [ReadOnly]public NativeArray<int> localHistograms;
        [WriteOnly] public NativeArray<int> prefixSums;
        public NativeArray<int> globalHistogram;
        
        [ReadOnly]public int segments;
        private int prefixSumAggregate;
        
        public void Execute()
        {
            int segmentSize = localHistograms.Length / segments;

            for (int i = 0; i < segments; i++)
            {
                int startIndex = segmentSize * i;
                int endIndex = startIndex + segmentSize;

                for (int j = startIndex; j < endIndex; j++)
                {
                    globalHistogram[j - startIndex] += localHistograms[j];
                }
            }
            
            prefixSumAggregate = 0;

            for (int i = 0; i < globalHistogram.Length; i++)
            {
                prefixSums[i] = prefixSumAggregate;
                prefixSumAggregate += globalHistogram[i];
            }
        }
    }

    [BurstCompile]
    public struct RadixScatter : IJob
    {
        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<ulong> encodings;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<ulong> tempEncodings;
        [NativeDisableParallelForRestriction] public NativeArray<int> prefixSums;
        [ReadOnly] public int pass;

        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double3> positions;
        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double3> velocities;
        [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double> masses;

        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double3> tempPositions;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double3> tempVelocities;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double> tempMasses;

        public void Execute()
        {
            int len = encodings.Length;
            
            for (int i = 0; i < len; i++)
            {
                int digit = (int)((encodings[i] >> (pass * 8)) & 0xFF);
                int position = prefixSums[digit];
                prefixSums[digit] = position + 1;
                tempEncodings[position] = encodings[i];
                tempPositions[position] = positions[i];
                tempVelocities[position] = velocities[i];
                tempMasses[position] = masses[i];
            }
        }
    }

    [BurstCompile]
    public unsafe struct RadixReassign : IJob
    {
        [WriteOnly] public NativeArray<ulong> encodings;
        [WriteOnly] public NativeArray<double3> positions;
        [WriteOnly] public NativeArray<double3> velocities;
        [WriteOnly] public NativeArray<double> masses;
        
        public NativeArray<ulong> tempEncodings;
        public NativeArray<double3> tempPositions;
        public NativeArray<double3> tempVelocities;
        public NativeArray<double> tempMasses;

        public void Execute()
        {
            NativeArray<ulong>.Copy(tempEncodings, encodings);
            NativeArray<double3>.Copy(tempPositions, positions);
            NativeArray<double3>.Copy(tempVelocities, velocities);
            NativeArray<double>.Copy(tempMasses, masses);
            
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(tempEncodings);
            long size = tempEncodings.Length * UnsafeUtility.SizeOf<ulong>();
            UnsafeUtility.MemClear(ptr, size);
            
            ptr = NativeArrayUnsafeUtility.GetUnsafePtr(tempPositions);
            size = tempPositions.Length * UnsafeUtility.SizeOf<double3>();
            UnsafeUtility.MemClear(ptr, size);
            
            ptr = NativeArrayUnsafeUtility.GetUnsafePtr(tempVelocities);
            size = tempVelocities.Length * UnsafeUtility.SizeOf<double3>();
            UnsafeUtility.MemClear(ptr, size);
            
            ptr = NativeArrayUnsafeUtility.GetUnsafePtr(tempMasses);
            size = tempMasses.Length * UnsafeUtility.SizeOf<double>();
            UnsafeUtility.MemClear(ptr, size);
        }
    }
}

