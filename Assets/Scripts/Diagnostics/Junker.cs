using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Junker : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        var j = new JunkJob();
        var handle = j.Schedule(1000, 16);
        JobHandle.ScheduleBatchedJobs();
    }
}

[BurstCompile]
public struct JunkJob : IJobParallelFor
{
    public void Execute(int i)
    {
        double s = 0;
        for (int k = 0; k < 200000; k++) s += math.sin(k + i);
    }
}
