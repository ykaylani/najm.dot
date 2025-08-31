using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Scraper))]
public class EnergyTracker : MonoBehaviour
{
    private Scraper scraper;
    private Propagator propagator;
    
    private NativeArray<double> totalEnergy;
    private NativeArray<long> kEnergy;
    private NativeArray<long> pEnergy;
    
    private Potential potentialJob;
    private Kinetic kineticJob;
    private JobHandle dependency;

    void Start()
    {
        scraper = GetComponent<Scraper>();
        propagator = GetComponent<Propagator>();
        
        totalEnergy = new NativeArray<double>(scraper.maxRecord, Allocator.Persistent);
        kEnergy = new NativeArray<long>(1, Allocator.Persistent);
        pEnergy = new NativeArray<long>(1, Allocator.Persistent);
        kEnergy[0] = DoubleToLong(0);
        pEnergy[0] = DoubleToLong(0);
    }

    public void Compute(int currentStep, NativeArray<double3> positions, NativeArray<double3> velocities, NativeArray<double> masses)
    {
        kEnergy[0] = DoubleToLong(0);
        pEnergy[0] = DoubleToLong(0);

        potentialJob = new Potential
        {
            masses = masses,
            positions = positions,
            potentialEnergy = pEnergy,
            gravitationalConstant = propagator.gravitationalConstant,
        };

        kineticJob = new Kinetic
        {
            masses = masses,
            velocities = velocities,
            kineticEnergy = kEnergy
        };
        
        dependency = potentialJob.Schedule(positions.Length, 512, dependency);
        dependency = kineticJob.Schedule(positions.Length, 512, dependency);
        dependency.Complete();

        if (currentStep > totalEnergy.Length) Time.timeScale = 0;
        totalEnergy[currentStep] = LongToDouble(kEnergy[0]) + LongToDouble(pEnergy[0]);
    }

    public void Export()
    {
        string content = string.Join("\n", totalEnergy.ToArray());
        string filePath = Path.Combine(Application.persistentDataPath, "Energy.txt");
        File.WriteAllText(filePath, content);
        Debug.Log($"File saved to: {filePath}");
    }

    void OnDestroy()
    {
        Export();
        
        totalEnergy.Dispose();
        kEnergy.Dispose();
        pEnergy.Dispose();
    }
    
    private long DoubleToLong(double value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Double = value;
        return u.Long;
    }

    private double LongToDouble(long value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Long = value;
        return u.Double;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct DoubleLongUnion
    {
        [FieldOffset(0)] public double Double;
        [FieldOffset(0)] public long Long;
    }
}

[BurstCompile]
public struct Potential : IJobParallelFor
{
    [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double> masses;
    [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double3> positions;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<long> potentialEnergy;
    public double gravitationalConstant;
    
    public void Execute(int index)
    {
        double contribution = 0;
        for (int j = index + 1; j < masses.Length; j++)
        {
            double3 distance = positions[j] - positions[index];
            contribution += -gravitationalConstant * masses[j] * masses[index] / math.sqrt(distance.x * distance.x + distance.y * distance.y + distance.z * distance.z);
        }

        unsafe
        {
            long* ptr = (long*)potentialEnergy.GetUnsafePtr();
            long oldLong, newLong;
            
            do { oldLong = *ptr; double currentValue = LongToDouble(oldLong); currentValue += contribution; newLong = DoubleToLong(currentValue); } 
            while (Interlocked.CompareExchange(ref *ptr, newLong, oldLong) != oldLong);
        }
    }
    
    private double LongToDouble(long value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Long = value;
        return u.Double;
    }

    private long DoubleToLong(double value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Double = value;
        return u.Long;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct DoubleLongUnion
    {
        [FieldOffset(0)] public double Double;
        [FieldOffset(0)] public long Long;
    }
}

[BurstCompile]
public struct Kinetic : IJobParallelFor
{
    [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double> masses;
    [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<double3> velocities;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<long> kineticEnergy;

    public void Execute(int index)
    {
        double speedSquared = math.dot(velocities[index], velocities[index]);
        double contribution = 0.5 * masses[index] * speedSquared;
        
        unsafe
        {
            long* ptr = (long*)kineticEnergy.GetUnsafePtr();
            long oldLong, newLong;
            
            do { oldLong = *ptr; double currentValue = LongToDouble(oldLong); currentValue += contribution; newLong = DoubleToLong(currentValue); } 
            while (Interlocked.CompareExchange(ref *ptr, newLong, oldLong) != oldLong);
        }
    }
    
    private double LongToDouble(long value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Long = value;
        return u.Double;
    }

    private long DoubleToLong(double value)
    {
        DoubleLongUnion u = new DoubleLongUnion();
        u.Double = value;
        return u.Long;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct DoubleLongUnion
    {
        [FieldOffset(0)] public double Double;
        [FieldOffset(0)] public long Long;
    }
}