using Unity.Mathematics;
using Unity.Collections;
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

public static class StructureConverter
{
    public static BodyStore SoABodies(InterfacedBodyInstance[] AoSbodies) //m00 is semimajor axis, m01 is eccentricity, m02 is longitude of ascending node, m03 is inclination, m10 is periapsis, m11 is true anomaly, m12 is orbitingAround, and m13 is calculating semimajor axis toggle.
    {
        BodyStore store = new BodyStore();
        
        if (store.positions.IsCreated) {store.positions.Dispose(); Debug.LogWarning("You are trying to call SoABodies while the original array is not disposed! For efficient GC, Do not do this.");}
        if (store.masses.IsCreated) {store.masses.Dispose(); Debug.LogWarning("You are trying to call SoABodies while original the array is not disposed! For efficient GC, Do not do this.");}
        if (store.velocities.IsCreated) {store.velocities.Dispose(); Debug.LogWarning("You are trying to call SoABodies while the original array is not disposed! For efficient GC, Do not do this.");}
        if (store.encodings.IsCreated) {store.encodings.Dispose(); Debug.LogWarning("You are trying to call SoABodies while the original array is not disposed! For efficient GC, Do not do this.");}

        store.positions = new NativeArray<Precision3>(AoSbodies.Length, Allocator.Persistent);
        store.masses = new NativeArray<Precision>(AoSbodies.Length, Allocator.Persistent);
        store.velocities = new NativeArray<Precision3>(AoSbodies.Length, Allocator.Persistent);
        
        store.keplerianParams = new NativeArray<Precision4x2>(AoSbodies.Length, Allocator.Persistent);
        store.encodings = new NativeArray<ulong>(AoSbodies.Length, Allocator.Persistent);

        for (int i = 0; i < AoSbodies.Length; i++)
        {
            store.positions[i] = (Precision3)AoSbodies[i].position;
            store.masses[i] = (Precision)AoSbodies[i].mass;
            store.velocities[i] = (Precision3)AoSbodies[i].velocity;
            double4x2 kp = new double4x2();
            kp[1][2] = AoSbodies[i].primaryBody;
            kp[0][1] = AoSbodies[i].eccentricity;
            kp[1][0] = AoSbodies[i].argumentOfPeriapsis;
            kp[0][2] = AoSbodies[i].longitudeOfTheAscendingNode;
            kp[0][3] = AoSbodies[i].inclination;
            kp[1][3] = 1; // calculation of semimajor axis assumed
            kp[1][1] = 0; // true anomaly assumed to be 0, going to add calculation soon
            store.keplerianParams[i] = (Precision4x2)kp;
        }
        
        return store;
    }

    public static InterfacedBodyInstance[] AoSBodies(NativeArray<Precision3> positions, NativeArray<Precision> masses, NativeArray<Precision4x2> keplerianParams)
    {
        InterfacedBodyInstance[] array = new InterfacedBodyInstance[positions.Length];

        for (int i = 0; i < array.Length; i++)
        {
            array[i].position = positions[i];
            array[i].mass = masses[i];
            array[i].primaryBody = (int)keplerianParams[i][1][2];
            array[i].eccentricity = keplerianParams[i][0][1];
            array[i].argumentOfPeriapsis = keplerianParams[i][1][0];
            array[i].inclination = keplerianParams[i][0][3];
            array[i].longitudeOfTheAscendingNode = keplerianParams[i][0][2];
            array[i].velocity = double3.zero;
        }
        
        return array;
    }
}
