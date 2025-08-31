using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

/*#if !USE_FLOAT
    using Precision = System.Single;
    using Precision3 = Unity.Mathematics.float3;
    using Precision4 = Unity.Mathematics.float4;
    using Precision4x2 = Unity.Mathematics.float4x2;
#else
    using Precision = System.Double;
    using Precision3 = Unity.Mathematics.double3;
    using Precision4 = Unity.Mathematics.double4;
    using Precision4x2 = Unity.Mathematics.double4x2;
#endif*/
    
using Precision = System.Double;
using Precision3 = Unity.Mathematics.double3;
using Precision4 = Unity.Mathematics.double4;
using Precision4x2 = Unity.Mathematics.double4x2;

public class OctantStore
{
    public NativeArray<Precision3> positions;
    public NativeArray<Precision3> coms;
    public NativeArray<Precision> masses;
    public NativeArray<Precision> sizes;

    public NativeArray<int> bodyIndices;
    public NativeArray<int> bodyCounts;

    public NativeArray<byte> treeChildCount;
    public NativeArray<int> treeChildren;
    public NativeArray<int> depths;

    public NativeArray<int> poolMarker;
    public UnsafeAtomicCounter32 poolStartMarker;
}

public class BodyStore
{
    public NativeArray<Precision3> positions;
    public NativeArray<Precision3> velocities;
    public NativeArray<Precision> masses;

    public NativeArray<ulong> encodings;
    
    public NativeArray<Precision4x2> keplerianParams; //m00 is semimajor axis, m01 is eccentricity, m02 is longitude of ascending node, m03 is inclination, m10 is periapsis, m11 is true anomaly, m12 is orbitingAround, and m13 is calculating semimajor axis toggle.
}

[DefaultExecutionOrder(500)]
[RequireComponent(typeof(BodyFrontend))]
public class Propagator : MonoBehaviour
{
    [Tooltip("x is simulation scale, y is simulation timestep, z is bounds, w is padding")]
    public Precision4 simulationSettings = new Precision4((Precision)1e9, (Precision)0.02, (Precision)200, (Precision)10);
    public Precision simulationTimestepMultiplier = 1;
    
    [Tooltip("The s/d criterion for barnes-hut to determine if it should use the approximation of an octant or compute each body. Lower Values make it more accurate and higher values make it more performant.")]
    [Range(0, 1)]public Precision openingAngleCriterion = 0.5f;
    
    [HideInInspector] public Precision gravitationalConstant = (Precision)6.67e-11;
    public int maxOctants = 16384;
    public int splittingThreshold = 16;
    public int softeningLengthSquared = 5000;
    
    public BodyStore bodies = new BodyStore();
    private OctantStore octants = new OctantStore();
    
    private NativeArray<Precision3> bodyForces;
    
    private NativeArray<ulong> tempBodyEncodings;
    private NativeArray<Precision3> tempBodyPositions;
    private NativeArray<Precision3> tempBodyVelocities;
    private NativeArray<Precision> tempBodyMasses;

    private NativeArray<int> globalHistogram;
    private NativeArray<int> histograms;
    private NativeArray<int> prefixSums;

    private CalculateVelocities velocityJob;
    private ResetOctantsJob resetOctants;
    private Encoder.BodyEncoding encoderJob;
    private Sorter.RadixClearHistograms radixClearHistogramsJob;
    private Sorter.RadixLocalHistograms radixLocalHistogramsJob;
    private Sorter.RadixGlobalHistogram radixGlobalHistogramJob;
    private Sorter.RadixScatter radixScatterJob;
    private Sorter.RadixReassign radixReassignJob;
    private OctreeBuildRoot octreeBuildRootJob;
    private OctreeBuildSubtrees octreeBuildSubtreesJob;
    private comJob comsJob;
    private ForceJob forceJob;
    
    private JobHandle initialVelocityJobHandle;
    private JobHandle dependency;
    
    void Start()
    {
        gravitationalConstant /= simulationSettings.x * simulationSettings.x * simulationSettings.x;
        gravitationalConstant *= simulationTimestepMultiplier * simulationTimestepMultiplier;
        
        octants.positions = new NativeArray<Precision3>(maxOctants, Allocator.Persistent);
        octants.coms = new NativeArray<Precision3>(maxOctants, Allocator.Persistent);
        octants.masses = new NativeArray<Precision>(maxOctants, Allocator.Persistent);
        octants.sizes = new NativeArray<Precision>(maxOctants, Allocator.Persistent);
        
        octants.bodyIndices = new NativeArray<int>(maxOctants, Allocator.Persistent);
        for(int i = 0; i < octants.bodyIndices.Length; i++) octants.bodyIndices[i] = -1;

        octants.bodyCounts = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildCount = new NativeArray<byte>(maxOctants, Allocator.Persistent);
        octants.depths = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildren = new NativeArray<int>(maxOctants * 8, Allocator.Persistent);

        bodies.encodings = new NativeArray<ulong>(bodies.positions.Length, Allocator.Persistent);
        bodyForces = new NativeArray<Precision3>(bodies.positions.Length, Allocator.Persistent);
        
        globalHistogram = new NativeArray<int>(256, Allocator.Persistent);
        histograms = new NativeArray<int>(256 * 16, Allocator.Persistent);
        prefixSums = new NativeArray<int>(256, Allocator.Persistent);
        
        tempBodyEncodings = new NativeArray<ulong>(bodies.encodings.Length, Allocator.Persistent);
        tempBodyPositions = new NativeArray<Precision3>(bodies.positions.Length, Allocator.Persistent);
        tempBodyMasses = new NativeArray<Precision>(bodies.masses.Length, Allocator.Persistent);
        tempBodyVelocities = new NativeArray<Precision3>(bodies.velocities.Length, Allocator.Persistent);
        
        octants.poolMarker = new NativeArray<int>(1, Allocator.Persistent);
        octants.poolMarker[0] = 1;
        unsafe { octants.poolStartMarker = new UnsafeAtomicCounter32((int*)octants.poolMarker.GetUnsafePtr()); }
        
        octants.bodyCounts[0] = bodies.positions.Length;

        velocityJob = new CalculateVelocities
        {
            keplerianParams = bodies.keplerianParams,
            bodyVelocities = bodies.velocities,
            bodyPostitions = bodies.positions,
            bodyMasses = bodies.masses,
            gravitationalConstant = gravitationalConstant
        };

        resetOctants = new ResetOctantsJob
        {
            octantBodyCounts = octants.bodyCounts,
            octantTreeChildCount = octants.treeChildCount,
            octantTreeChildren = octants.treeChildren,
            octantDepths = octants.depths,
            octantMasses = octants.masses,
            octantComs = octants.coms,
            octantPositions = octants.positions,
            octantSizes = octants.sizes,
            octantBodyIndices = octants.bodyIndices,
            simulationSettings = simulationSettings,
            bodyCount = bodies.positions.Length,
        };

        encoderJob = new Encoder.BodyEncoding
        {
            maxBounds = new Precision3(simulationSettings.z + simulationSettings.w),
            encodings = bodies.encodings,
            positions = bodies.positions,
        };

        radixClearHistogramsJob = new Sorter.RadixClearHistograms
        {
            globalHistogram = globalHistogram,
            histograms = histograms,
        };
        
        radixGlobalHistogramJob = new Sorter.RadixGlobalHistogram
        {
            localHistograms = histograms,
            prefixSums = prefixSums,
            globalHistogram = globalHistogram,
            segments = 16
        };

        radixReassignJob = new Sorter.RadixReassign
        {
            encodings = bodies.encodings,
            positions = bodies.positions,
            velocities = bodies.velocities,
            masses = bodies.masses,
            tempEncodings = tempBodyEncodings,
            tempPositions = tempBodyPositions,
            tempVelocities = tempBodyVelocities,
            tempMasses = tempBodyMasses,
        };

        octreeBuildRootJob = new OctreeBuildRoot
        {
            bodyEncodings = bodies.encodings,
            octantBodyCounts = octants.bodyCounts,
            octantTreeChildCount = octants.treeChildCount,
            octantTreeChildren = octants.treeChildren,
            octantDepths = octants.depths,
            octantBodyIndices = octants.bodyIndices,
            octantPoolStartMarker = octants.poolStartMarker,
            octantPositions = octants.positions,
            octantSizes = octants.sizes,
            splittingThreshold = splittingThreshold
        };

        octreeBuildSubtreesJob = new OctreeBuildSubtrees
        {
            bodyEncodings = bodies.encodings,
            octantBodyCounts = octants.bodyCounts,
            octantTreeChildCount = octants.treeChildCount,
            octantTreeChildren = octants.treeChildren,
            octantDepths = octants.depths,
            octantBodyIndices = octants.bodyIndices,
            octantPoolStartMarker = octants.poolStartMarker,
            octantPositions = octants.positions,
            octantSizes = octants.sizes,
            splittingThreshold = splittingThreshold,
        };

        comsJob = new comJob
        {
            bodyPositions = bodies.positions,
            bodyMasses = bodies.masses,
            octantCOMs = octants.coms,
            octantMasses = octants.masses,
            octantSizes = octants.sizes,
            octantBodyIndices = octants.bodyIndices,
            octantBodyCounts = octants.bodyCounts,
            octantTreeChildren = octants.treeChildren,
            octantTreeChildCount = octants.treeChildCount,
            octantLimit = maxOctants
        };

        forceJob = new ForceJob
        {
            octantBodyIndices = octants.bodyIndices,
            octantMasses = octants.masses,
            octantSizes = octants.sizes,
            octantCOMs = octants.coms,
            octantBodyCounts = octants.bodyCounts,
            octantTreeChildCount = octants.treeChildCount,
            octantTreeChildren = octants.treeChildren,
            bodyForces = bodyForces,
            bodyMasses = bodies.masses,
            bodyPositions = bodies.positions,
            gravitationalConstant = gravitationalConstant,
            openingAngle = openingAngleCriterion,
            softening = softeningLengthSquared
            
        };

        initialVelocityJobHandle = new JobHandle();
        initialVelocityJobHandle = velocityJob.Schedule(bodies.keplerianParams.Length, 16, initialVelocityJobHandle);
        initialVelocityJobHandle.Complete();
    }

    void FixedUpdate()
    {
        if (!initialVelocityJobHandle.IsCompleted) initialVelocityJobHandle.Complete();
        
        for (int i = 0; i < bodies.positions.Length; i++)
        {
            Precision3 force = bodyForces[i];
            bodies.velocities[i] += Time.fixedDeltaTime * (force / bodies.masses[i]);
            bodies.positions[i] += Time.fixedDeltaTime * bodies.velocities[i];
            bodyForces[i] = Precision3.zero;
        }
        
        octants.poolMarker[0] = 1;
        
        dependency = resetOctants.Schedule(octants.positions.Length, 512, dependency);
        dependency = encoderJob.Schedule(bodies.encodings.Length, 512, dependency);

        for (int i = 0; i < 8; i++)
        {
            dependency = radixClearHistogramsJob.Schedule(dependency);
            
            radixLocalHistogramsJob = new Sorter.RadixLocalHistograms
            {
                encodings = bodies.encodings,
                localHistograms = histograms,
                segments = 16,
                pass = i
            };
            dependency = radixLocalHistogramsJob.Schedule(16, 1, dependency);
            dependency = radixGlobalHistogramJob.Schedule(dependency);

            radixScatterJob = new Sorter.RadixScatter
            {
                encodings = bodies.encodings,
                tempEncodings = tempBodyEncodings,
                prefixSums = prefixSums,
                pass = i,
                positions = bodies.positions,
                velocities = bodies.velocities,
                masses = bodies.masses,
                tempPositions = tempBodyPositions,
                tempVelocities = tempBodyVelocities,
                tempMasses = tempBodyMasses,
            };
            dependency = radixScatterJob.Schedule(dependency);
            dependency = radixReassignJob.Schedule(dependency);
        }
        
        dependency = octreeBuildRootJob.Schedule(dependency);
        dependency = octreeBuildSubtreesJob.Schedule(8, 1, dependency);
        dependency = comsJob.Schedule(dependency);
        dependency = forceJob.Schedule(bodies.positions.Length, 512, dependency);
        dependency.Complete();
    }
    
    void OnDestroy()
    {
        octants.positions.Dispose();
        octants.coms.Dispose();
        octants.sizes.Dispose();
        
        octants.bodyIndices.Dispose();
        octants.bodyCounts.Dispose();
        octants.treeChildCount.Dispose();
        
        bodies.positions.Dispose();
        bodies.velocities.Dispose();
        bodies.masses.Dispose();
        
        bodies.encodings.Dispose();
        
        globalHistogram.Dispose();
        histograms.Dispose();
        prefixSums.Dispose();
        
        tempBodyEncodings.Dispose();
        tempBodyPositions.Dispose();
        tempBodyVelocities.Dispose();
        tempBodyMasses.Dispose();
        
        if (bodies.positions.IsCreated) bodies.positions.Dispose();
        if (bodies.velocities.IsCreated) bodies.velocities.Dispose();
        if (bodies.masses.IsCreated) bodies.masses.Dispose();
        if (bodies.encodings.IsCreated) bodies.encodings.Dispose();

        bodyForces.Dispose();
    }
}

[BurstCompile]
public struct ResetOctantsJob : IJobParallelFor
{
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<int> octantBodyCounts;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<byte> octantTreeChildCount;
    [NativeDisableParallelForRestriction] public NativeArray<int> octantTreeChildren;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<int> octantDepths;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Precision> octantMasses;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Precision3> octantComs;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Precision3> octantPositions;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Precision> octantSizes;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<int> octantBodyIndices;
    
    [ReadOnly, NativeDisableParallelForRestriction] public Precision4 simulationSettings;
    [ReadOnly, NativeDisableParallelForRestriction] public int bodyCount;
    
    public void Execute(int octant)
    {
        octantBodyCounts[octant] = 0;
        octantTreeChildCount[octant] = 0;
        octantDepths[octant] = 0;
        octantMasses[octant] = 0;
        octantComs[octant] = Precision3.zero;
        octantPositions[octant] = Precision3.zero;
        octantSizes[octant] = -1;
        octantBodyIndices[octant] = -1;

        if (octant == 0)
        {
            octantPositions[octant] = Precision3.zero;
            octantSizes[octant] = simulationSettings.z + simulationSettings.w;
            octantDepths[octant] = 0;
            octantBodyCounts[octant] = bodyCount;
            octantBodyIndices[octant] = 0;
            
            for (int i = 0; i < octantTreeChildren.Length; i++) 
            {
                octantTreeChildren[i] = -1;
            }
        }
    }
}

[BurstCompile]
public struct CalculateVelocities : IJobParallelFor
{
    [ReadOnly]public NativeArray<Precision4x2> keplerianParams;
    
    [WriteOnly] public NativeArray<Precision3> bodyVelocities;
    [ReadOnly] public NativeArray<Precision3> bodyPostitions;
    [ReadOnly] public NativeArray<Precision> bodyMasses;

    [ReadOnly] public Precision gravitationalConstant;

    public void Execute(int body)
    {
        if (!(keplerianParams[body][1][2] >= 0)) {return;}
        Precision4x2 bodyKeplerianParams = keplerianParams[body];
        
        if (bodyKeplerianParams[1][3] > 0 && !(bodyKeplerianParams[1][2] < 0))
        {
            Precision3 relativePosition = bodyPostitions[body] - bodyPostitions[(int)bodyKeplerianParams[1][2]];
                
            Precision4x2 kp = bodyKeplerianParams;
            kp[0][0] = math.length(relativePosition);
            bodyKeplerianParams = kp;
        }
        
        Precision4 transformation = new Precision4(0, 0, 0, gravitationalConstant * bodyMasses[(int)bodyKeplerianParams[1][2]]);

        Precision eccentricity = bodyKeplerianParams[0][1];
        
        Precision factor = transformation.w / math.sqrt(transformation.w * bodyKeplerianParams[0][0] * (1 - eccentricity * eccentricity));

        Precision trueAnomaly = bodyKeplerianParams[1][1];
        Precision periapsis = bodyKeplerianParams[1][0];
        Precision inclination = bodyKeplerianParams[0][3];
        Precision ascendingNode = bodyKeplerianParams[0][2];
        
        Precision taSin = math.sin(trueAnomaly);
        Precision taCos = math.cos(trueAnomaly);
        Precision periapsisSin = math.sin(periapsis);
        Precision periapsisCos = math.cos(periapsis);
        Precision inclinationSin = math.sin(inclination);
        Precision inclinationCos = math.cos(inclination);
        Precision ascendingNodeSin = math.sin(ascendingNode);
        Precision ascendingNodeCos = math.cos(ascendingNode);
        
        //perifocal and periapsis rotations
        transformation.x = -factor * taSin * periapsisCos - factor * (eccentricity + taCos) * periapsisSin;
        transformation.y = -factor * taSin * periapsisSin + factor * (eccentricity + taCos) * periapsisCos;
        
        //transformation.x carries over to ascending node rotation (this is inclination)
        Precision tempy = transformation.y;
        Precision tempz = transformation.z;
        transformation.y = tempy * inclinationCos - tempz * inclinationSin;
        transformation.z = tempy * inclinationSin + tempz * inclinationCos;
        
        //ascending node rotation (z is not modified)
        Precision tempx = transformation.x;
        Precision tempy2 = transformation.y;
        transformation.x = tempx * ascendingNodeCos - tempy2 * ascendingNodeSin;
        transformation.y = tempx * ascendingNodeSin + tempy2 * ascendingNodeCos;
        
        Precision3 final = new Precision3(transformation.x, transformation.z, transformation.y);
        bodyVelocities[body] = final;
    }
}

[BurstCompile]
public struct OctreeBuildRoot : IJob
{
    public NativeArray<int> octantDepths;
    public NativeArray<int> octantBodyCounts;
    public NativeArray<int> octantBodyIndices;
    
    public NativeArray<Precision> octantSizes;
    public NativeArray<Precision3> octantPositions;
    
    public NativeArray<int> octantTreeChildren;
    public NativeArray<byte> octantTreeChildCount;

    [ReadOnly] public NativeArray<ulong> bodyEncodings;
    
    [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 octantPoolStartMarker;
    public int splittingThreshold;
    
    public void Execute()
    {
        Build(0, 1);
    }

    private void Build(int root, int maxDepth)
    {
        FixedList4096Bytes<int> octantProcessingQueue = new FixedList4096Bytes<int>();
        octantProcessingQueue.Add(root);

        while (octantProcessingQueue.Length > 0)
        {
            int currentOctant = octantProcessingQueue[octantProcessingQueue.Length - 1];
            octantProcessingQueue.RemoveAt(octantProcessingQueue.Length - 1);

            int depth = octantDepths[currentOctant];
            
            if (depth >= maxDepth || octantBodyCounts[currentOctant] < splittingThreshold) continue;

            int firstBody = octantBodyIndices[currentOctant];
            int lastBody = firstBody + octantBodyCounts[currentOctant] - 1;
            
            if (firstBody < 0 || lastBody >= bodyEncodings.Length || firstBody > lastBody) continue;

            int currentStart = firstBody;
            int currentChild = Encoder.ChildIndex(bodyEncodings[currentStart], depth + 1);

            for (int i = firstBody + 1; i <= lastBody; i++)
            {
                int child = Encoder.ChildIndex(bodyEncodings[i], depth + 1);
                if (child != currentChild)
                {
                    if (i - 1 >= currentStart) InitializeOctant(currentOctant, currentStart, i - 1, currentChild, depth, ref octantProcessingQueue, maxDepth);
                    currentStart = i;
                    currentChild = child;
                }
            }
            if (lastBody >= currentStart) InitializeOctant(currentOctant, currentStart, lastBody, currentChild, depth, ref octantProcessingQueue, maxDepth);
        }
    }
    
    void InitializeOctant(int parent, int start, int end, int child, int depth, ref FixedList4096Bytes<int> processQueue, int maxDepth)
    {
        if (start > end) return;
        int childOctant = octantPoolStartMarker.Add(1);

        octantBodyIndices[childOctant] = start;
        octantBodyCounts[childOctant] = end - start + 1;
        octantDepths[childOctant] = depth + 1;
        
        Precision parentSize = octantSizes[parent];
        
        octantSizes[childOctant] = parentSize / 2;
        Precision3 childOffset = new Precision3((Precision)((child & 1) != 0 ? 0.25 : -0.25), (Precision)((child & 2) != 0 ? 0.25 : -0.25), (Precision)((child & 4) != 0 ? 0.25 : -0.25)) * parentSize;
        octantPositions[childOctant] = octantPositions[parent] + childOffset;

        octantTreeChildren[parent * 8 + child] = childOctant;
        octantTreeChildCount[parent]++;
        
        if (end - start + 1 > 1 && depth + 1 < maxDepth) processQueue.Add(childOctant);
    }
}

[BurstCompile]
public struct OctreeBuildSubtrees : IJobParallelFor
{
    [NativeDisableParallelForRestriction] public NativeArray<int> octantDepths;
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyCounts;
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyIndices;

    [NativeDisableParallelForRestriction] public NativeArray<Precision> octantSizes;
    [NativeDisableParallelForRestriction] public NativeArray<Precision3> octantPositions;

    [NativeDisableParallelForRestriction] public NativeArray<int> octantTreeChildren;
    [NativeDisableParallelForRestriction] public NativeArray<byte> octantTreeChildCount;

    [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<ulong> bodyEncodings;

    [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 octantPoolStartMarker;
    [NativeDisableParallelForRestriction] public int splittingThreshold;
    

    public void Execute(int index)
    {
        int childRoot = octantTreeChildren[index];
        if (childRoot < 0) return;
        Build(childRoot, 21);
    }

    private void Build(int root, int maxDepth)
    {
        FixedList4096Bytes<int> octantProcessingQueue = new FixedList4096Bytes<int>();
        octantProcessingQueue.Add(root);

        while (octantProcessingQueue.Length > 0)
        {
            int currentOctant = octantProcessingQueue[octantProcessingQueue.Length - 1];
            octantProcessingQueue.RemoveAt(octantProcessingQueue.Length - 1);
            
            int depth = octantDepths[currentOctant];

            if (depth >= maxDepth || octantBodyCounts[currentOctant] < splittingThreshold) continue;

            int firstBody = octantBodyIndices[currentOctant];
            int lastBody = firstBody + octantBodyCounts[currentOctant] - 1;
            
            if (firstBody < 0 || lastBody >= bodyEncodings.Length || firstBody > lastBody) continue;

            int currentStart = firstBody;
            int currentChild = Encoder.ChildIndex(bodyEncodings[currentStart], depth + 1);

            for (int i = firstBody + 1; i <= lastBody; i++)
            {

                int child = Encoder.ChildIndex(bodyEncodings[i], depth + 1);
                if (child != currentChild)
                {
                    if (i - 1 >= currentStart) InitializeOctant(currentOctant, currentStart, i - 1, currentChild, depth, ref octantProcessingQueue, maxDepth);
                    currentStart = i;
                    currentChild = child;
                }
            }

            if (lastBody >= currentStart) InitializeOctant(currentOctant, currentStart, lastBody, currentChild, depth, ref octantProcessingQueue, maxDepth);
        }
    }

    void InitializeOctant(int parent, int start, int end, int child, int depth, ref FixedList4096Bytes<int> processQueue, int maxDepth)
    {
        if (start > end) return;
        
        int treeIndex = parent * 8 + child;
        int childOctant = octantPoolStartMarker.Add(1);

        octantBodyIndices[childOctant] = start;
        octantBodyCounts[childOctant] = end - start + 1;
        octantDepths[childOctant] = depth + 1;

        Precision parentSize = octantSizes[parent];

        octantSizes[childOctant] = parentSize / 2;
        Precision3 childOffset = new Precision3((Precision)((child & 1) != 0 ? 0.25 : -0.25), (Precision)((child & 2) != 0 ? 0.25 : -0.25), (Precision)((child & 4) != 0 ? 0.25 : -0.25)) * parentSize;
        octantPositions[childOctant] = octantPositions[parent] + childOffset;

        octantTreeChildren[treeIndex] = childOctant;
        octantTreeChildCount[parent]++;

        if (end - start + 1 > 1 && depth + 1 < maxDepth) processQueue.Add(childOctant);
    }
}

[BurstCompile]
public struct comJob : IJob
{
    [NativeDisableParallelForRestriction] public NativeArray<Precision3> bodyPositions;
    [NativeDisableParallelForRestriction] public NativeArray<Precision> bodyMasses;

    [NativeDisableParallelForRestriction] public NativeArray<Precision3> octantCOMs;
    [NativeDisableParallelForRestriction] public NativeArray<Precision> octantMasses;
    [NativeDisableParallelForRestriction] public NativeArray<Precision> octantSizes;
    
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyIndices;
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyCounts;
    
    [NativeDisableParallelForRestriction] public NativeArray<int> octantTreeChildren;
    [NativeDisableParallelForRestriction] public NativeArray<byte> octantTreeChildCount;

    [NativeDisableParallelForRestriction] public int octantLimit;

    public void Execute()
    {
        for (int octant = octantLimit - 1; octant >= 0; octant--)
        {
            if (octantSizes[octant] <= 0) continue;

            int childCount = octantBodyCounts[octant];
            int childStart = octantBodyIndices[octant];

            octantCOMs[octant] = Precision3.zero;
            octantMasses[octant] = 0;

            Precision totalMass = 0;
            Precision3 totalCOM = Precision3.zero;

            if (octantTreeChildCount[octant] > 0)
            {
                int baseChild = octant * 8;
                for (int k = 0; k < 8; k++)
                {
                    int child = octantTreeChildren[baseChild + k];
                    if (child < 0) continue;

                    Precision mass = octantMasses[child];
                    if (mass <= 0.0) continue;

                    totalMass += mass;
                    totalCOM += octantCOMs[child] * mass;
                }
            }
            else
            {
                for (int j = 0; j < childCount; j++)
                {
                    int currentChild = childStart + j;
                    totalMass += bodyMasses[currentChild];
                    totalCOM += bodyPositions[currentChild] * bodyMasses[currentChild];
                }
            }

            if (totalMass > 0) totalCOM /= totalMass;

            octantCOMs[octant] = totalCOM;
            octantMasses[octant] = totalMass;
        }
    }
}



[BurstCompile]
public struct ForceJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Precision3> bodyPositions;
    [ReadOnly] public NativeArray<Precision> bodyMasses;
    
    [ReadOnly] public NativeArray<Precision3> octantCOMs;
    [ReadOnly] public NativeArray<Precision> octantMasses;
    [ReadOnly] public NativeArray<Precision> octantSizes;

    [ReadOnly] public NativeArray<int> octantBodyCounts;
    [ReadOnly] public NativeArray<int> octantBodyIndices;
    
    [ReadOnly] public NativeArray<int> octantTreeChildren;
    [ReadOnly] public NativeArray<byte> octantTreeChildCount;
    
    [ReadOnly] public Precision gravitationalConstant;
    [ReadOnly] public Precision openingAngle;
    
    [WriteOnly] public NativeArray<Precision3> bodyForces;
    [ReadOnly] public int softening;
    
    public void Execute(int body)
    {
        Precision3 force = Force(body, 0, openingAngle); 
        bodyForces[body] = force;
    }
    
    private Precision3 Force(int body, int startOctant, Precision openingAngle)
    {
        FixedList4096Bytes<int> stack = new FixedList4096Bytes<int>();
        stack.Add(startOctant);
        
        Precision3 netForce = Precision3.zero;
        
        while (stack.Length > 0)
        {
            int currentOctant = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            Precision3 distance = (octantCOMs[currentOctant] - bodyPositions[body]);
            Precision distanceSquared = math.lengthsq(distance) + softening;
            
            if (distanceSquared == 1e-10f) continue;
            if (octantMasses[currentOctant] == 0) continue;
            
            int startIndex = octantBodyIndices[currentOctant];
            int endIndex = startIndex + octantBodyCounts[currentOctant] - 1;
            bool containsBody = (body >= startIndex && body <= endIndex);
        
            if (containsBody && octantBodyCounts[currentOctant] == 1) continue;
            
            bool approximate = octantSizes[currentOctant] * octantSizes[currentOctant] / distanceSquared < openingAngle * openingAngle;
            
            if (approximate && !containsBody)
            {
                Precision rsqrtDistance = math.rsqrt(distanceSquared);
                Precision force = gravitationalConstant * bodyMasses[body] * octantMasses[currentOctant] * (rsqrtDistance * rsqrtDistance * rsqrtDistance);
                Precision3 direction = distance;
                if (math.isnan(force) || math.isinf(force)) Debug.LogError($"Invalid forceMagnitude: {force}");
                netForce += force * direction;
            }
            else
            {
                if (octantTreeChildCount[currentOctant] > 0)
                {
                    int firstChild = currentOctant * 8;

                    for (int i = 0; i < 8; i++)
                    {
                        if (octantTreeChildren[firstChild + i] < 0) continue;
                        if (octantMasses[octantTreeChildren[firstChild + i]] <= 0) continue;
                        stack.Add(octantTreeChildren[firstChild + i]);
                    }
                }
                else
                {
                    int childStart = octantBodyIndices[currentOctant];
                    
                    for (int i = 0; i < octantBodyCounts[currentOctant]; i++)
                    {
                        int selectedBody = childStart + i;
                        if(selectedBody == body) continue;
                        
                        Precision3 bodyDistance = (bodyPositions[selectedBody] - bodyPositions[body]);
                        Precision bodyDistanceSquared = math.lengthsq(bodyDistance) + softening;
                        Precision rsqrtDistance = math.rsqrt(bodyDistanceSquared);
                        if (bodyDistanceSquared == 1e-10f) continue;
                        
                        Precision force = gravitationalConstant * bodyMasses[body] * bodyMasses[selectedBody] * (rsqrtDistance * rsqrtDistance * rsqrtDistance);
                        Precision3 direction = distance;
                        if (math.isnan(force) || math.isinf(force))
                            Debug.LogError($"Invalid forceMagnitude: {force}");
                        netForce += force * direction;
                    }
                }
            }
        }
        
        return netForce;
    }
}

