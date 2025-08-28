using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

public class OctantStore
{
    public NativeArray<double3> positions;
    public NativeArray<double3> coms;
    public NativeArray<double> masses;
    public NativeArray<double> sizes;

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
    public NativeArray<double3> positions;
    public NativeArray<double3> velocities;
    public NativeArray<double> masses;

    public NativeArray<ulong> encodings;
    
    public NativeArray<double4x2> keplerianParams; //m00 is semimajor axis, m01 is eccentricity, m02 is longitude of ascending node, m03 is inclination, m10 is periapsis, m11 is true anomaly, m12 is orbitingAround, and m13 is calculating semimajor axis toggle.
}

[DefaultExecutionOrder(500)]
[RequireComponent(typeof(BodyFrontend))]
public class Propagator : MonoBehaviour
{
    [Tooltip("x is simulation scale, y is simulation timestep, z is bounds, w is padding")]
    public double4 simulationSettings = new double4(1e9, 0.02, 200, 10);
    public double simulationTimestepMultiplier = 1;
    
    [Tooltip("The s/d criterion for barnes-hut to determine if it should use the approximation of an octant or compute each body. Lower Values make it more accurate and higher values make it more performant.")]
    [Range(0, 1)]public float openingAngleCriterion = 0.5f;
    
    private  double gravitationalConstant = 6.67e-11;
    private const short maxOctants = 4096;
    
    public BodyStore bodies = new BodyStore();
    private OctantStore octants = new OctantStore();
    
    private NativeArray<double3> bodyForces;
    
    private NativeArray<ulong> tempBodyEncodings;
    private NativeArray<double3> tempBodyPositions;
    private NativeArray<double3> tempBodyVelocities;
    private NativeArray<double> tempBodyMasses;

    private NativeArray<int> globalHistogram;
    private NativeArray<int> histograms;
    private NativeArray<int> prefixSums;

    private CalculateVelocities velocityJob;
    private ResetOctantsJob resetOctants;
    private BodyEncoding encoderJob;
    private RadixClearHistograms radixClearHistogramsJob;
    private RadixLocalHistograms radixLocalHistogramsJob;
    private RadixGlobalHistogram radixGlobalHistogramJob;
    private RadixScatter radixScatterJob;
    private RadixReassign radixReassignJob;
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
        
        octants.positions = new NativeArray<double3>(maxOctants, Allocator.Persistent);
        octants.coms = new NativeArray<double3>(maxOctants, Allocator.Persistent);
        octants.masses = new NativeArray<double>(maxOctants, Allocator.Persistent);
        octants.sizes = new NativeArray<double>(maxOctants, Allocator.Persistent);
        
        octants.bodyIndices = new NativeArray<int>(maxOctants, Allocator.Persistent);
        for(int i = 0; i < octants.bodyIndices.Length; i++) octants.bodyIndices[i] = -1;

        octants.bodyCounts = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildCount = new NativeArray<byte>(maxOctants, Allocator.Persistent);
        octants.depths = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildren = new NativeArray<int>(maxOctants * 8, Allocator.Persistent);

        bodies.encodings = new NativeArray<ulong>(bodies.positions.Length, Allocator.Persistent);
        bodyForces = new NativeArray<double3>(bodies.positions.Length, Allocator.Persistent);
        
        globalHistogram = new NativeArray<int>(256, Allocator.Persistent);
        histograms = new NativeArray<int>(256 * 16, Allocator.Persistent);
        prefixSums = new NativeArray<int>(256, Allocator.Persistent);
        
        tempBodyEncodings = new NativeArray<ulong>(bodies.encodings.Length, Allocator.Persistent);
        tempBodyPositions = new NativeArray<double3>(bodies.positions.Length, Allocator.Persistent);
        tempBodyMasses = new NativeArray<double>(bodies.masses.Length, Allocator.Persistent);
        tempBodyVelocities = new NativeArray<double3>(bodies.velocities.Length, Allocator.Persistent);
        
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

        encoderJob = new BodyEncoding
        {
            maxBounds = new double3(simulationSettings.z + simulationSettings.w),
            encodings = bodies.encodings,
            positions = bodies.positions,
        };

        radixClearHistogramsJob = new RadixClearHistograms
        {
            globalHistogram = globalHistogram,
            histograms = histograms,
        };
        
        radixGlobalHistogramJob = new RadixGlobalHistogram
        {
            localHistograms = histograms,
            prefixSums = prefixSums,
            globalHistogram = globalHistogram,
            segments = 16
        };

        radixReassignJob = new RadixReassign
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
            
        };

        initialVelocityJobHandle = new JobHandle();
        initialVelocityJobHandle = velocityJob.Schedule(bodies.keplerianParams.Length, 16, initialVelocityJobHandle);
    }

    void FixedUpdate()
    {
        if (!initialVelocityJobHandle.IsCompleted) initialVelocityJobHandle.Complete();
        
        for (int i = 0; i < bodies.positions.Length; i++)
        {
            double3 force = bodyForces[i];
            bodies.velocities[i] += Time.fixedDeltaTime * (force / bodies.masses[i]);
            bodies.positions[i] += Time.fixedDeltaTime * bodies.velocities[i];
            bodyForces[i] = double3.zero;
        }
        
        octants.poolMarker[0] = 1;


        dependency = resetOctants.Schedule(octants.positions.Length, 32, dependency);
        dependency = encoderJob.Schedule(bodies.encodings.Length, 32, dependency);

        for (int i = 0; i < 8; i++)
        {
            dependency = radixClearHistogramsJob.Schedule(dependency);
            
            radixLocalHistogramsJob = new RadixLocalHistograms
            {
                encodings = bodies.encodings,
                localHistograms = histograms,
                segments = 16,
                pass = i
            };
            dependency = radixLocalHistogramsJob.Schedule(16, 1, dependency);
            dependency = radixGlobalHistogramJob.Schedule(dependency);

            radixScatterJob = new RadixScatter
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
        dependency = forceJob.Schedule(bodies.positions.Length, 16, dependency);
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
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double> octantMasses;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double3> octantComs;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double3> octantPositions;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<double> octantSizes;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<int> octantBodyIndices;
    
    [ReadOnly, NativeDisableParallelForRestriction] public double4 simulationSettings;
    [ReadOnly, NativeDisableParallelForRestriction] public int bodyCount;
    
    public void Execute(int octant)
    {
        octantBodyCounts[octant] = 0;
        octantTreeChildCount[octant] = 0;
        octantDepths[octant] = 0;
        octantMasses[octant] = 0;
        octantComs[octant] = double3.zero;
        octantPositions[octant] = double3.zero;
        octantSizes[octant] = -1;
        octantBodyIndices[octant] = -1;

        if (octant == 0)
        {
            octantPositions[octant] = double3.zero;
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
    [ReadOnly]public NativeArray<double4x2> keplerianParams;
    
    [WriteOnly] public NativeArray<double3> bodyVelocities;
    [ReadOnly] public NativeArray<double3> bodyPostitions;
    [ReadOnly] public NativeArray<double> bodyMasses;

    [ReadOnly] public double gravitationalConstant;

    public void Execute(int body)
    {
        if (!(keplerianParams[body][1][2] >= 0)) {bodyVelocities[body] = double3.zero; return;}
        double4x2 bodyKeplerianParams = keplerianParams[body];
        
        if (bodyKeplerianParams[1][3] > 0 && !(bodyKeplerianParams[1][2] < 0))
        {
            double3 relativePosition = bodyPostitions[body] - bodyPostitions[(int)bodyKeplerianParams[1][2]];
                
            double4x2 kp = bodyKeplerianParams;
            kp[0][0] = math.length(relativePosition);
            bodyKeplerianParams = kp;
        }
        
        double4 transformation = new double4(0, 0, 0, gravitationalConstant * bodyMasses[(int)bodyKeplerianParams[1][2]]);

        double eccentricity = bodyKeplerianParams[0][1];
        
        double factor = transformation.w / math.sqrt(transformation.w * bodyKeplerianParams[0][0] * (1 - eccentricity * eccentricity));

        double trueAnomaly = bodyKeplerianParams[1][1];
        double periapsis = bodyKeplerianParams[1][0];
        double inclination = bodyKeplerianParams[0][3];
        double ascendingNode = bodyKeplerianParams[0][2];
        
        double taSin = math.sin(trueAnomaly);
        double taCos = math.cos(trueAnomaly);
        double periapsisSin = math.sin(periapsis);
        double periapsisCos = math.cos(periapsis);
        double inclinationSin = math.sin(inclination);
        double inclinationCos = math.cos(inclination);
        double ascendingNodeSin = math.sin(ascendingNode);
        double ascendingNodeCos = math.cos(ascendingNode);
        
        //perifocal and periapsis rotations
        transformation.x = -factor * taSin * periapsisCos - factor * (eccentricity + taCos) * periapsisSin;
        transformation.y = -factor * taSin * periapsisSin + factor * (eccentricity + taCos) * periapsisCos;
        
        //transformation.x carries over to ascending node rotation (this is inclination)
        double tempy = transformation.y;
        double tempz = transformation.z;
        transformation.y = tempy * inclinationCos - tempz * inclinationSin;
        transformation.z = tempy * inclinationSin + tempz * inclinationCos;
        
        //ascending node rotation (z is not modified)
        double tempx = transformation.x;
        double tempy2 = transformation.y;
        transformation.x = tempx * ascendingNodeCos - tempy2 * ascendingNodeSin;
        transformation.y = tempx * ascendingNodeSin + tempy2 * ascendingNodeCos;
        
        double3 final = new double3(transformation.x, transformation.z, transformation.y);
        bodyVelocities[body] = final;
    }
}

[BurstCompile]
public struct OctreeBuildRoot : IJob
{
    public NativeArray<int> octantDepths;
    public NativeArray<int> octantBodyCounts;
    public NativeArray<int> octantBodyIndices;
    
    public NativeArray<double> octantSizes;
    public NativeArray<double3> octantPositions;
    
    public NativeArray<int> octantTreeChildren;
    public NativeArray<byte> octantTreeChildCount;

    [ReadOnly] public NativeArray<ulong> bodyEncodings;
    
    [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 octantPoolStartMarker;
    
    
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
            
            if (depth >= maxDepth || octantBodyCounts[currentOctant] < 2) continue;

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
        
        double parentSize = octantSizes[parent];
        
        octantSizes[childOctant] = parentSize / 2;
        double3 childOffset = new double3((child & 1) != 0 ? 0.25 : -0.25, (child & 2) != 0 ? 0.25 : -0.25, (child & 4) != 0 ? 0.25 : -0.25) * parentSize;
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

    [NativeDisableParallelForRestriction] public NativeArray<double> octantSizes;
    [NativeDisableParallelForRestriction] public NativeArray<double3> octantPositions;

    [NativeDisableParallelForRestriction] public NativeArray<int> octantTreeChildren;
    [NativeDisableParallelForRestriction] public NativeArray<byte> octantTreeChildCount;

    [ReadOnly, NativeDisableParallelForRestriction]
    public NativeArray<ulong> bodyEncodings;

    [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 octantPoolStartMarker;

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

            if (depth >= maxDepth || octantBodyCounts[currentOctant] < 2) continue;

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

        double parentSize = octantSizes[parent];

        octantSizes[childOctant] = parentSize / 2;
        double3 childOffset = new double3((child & 1) != 0 ? 0.25 : -0.25, (child & 2) != 0 ? 0.25 : -0.25, (child & 4) != 0 ? 0.25 : -0.25) * parentSize;
        octantPositions[childOctant] = octantPositions[parent] + childOffset;

        octantTreeChildren[treeIndex] = childOctant;
        octantTreeChildCount[parent]++;

        if (end - start + 1 > 1 && depth + 1 < maxDepth) processQueue.Add(childOctant);
    }
}

[BurstCompile]
public struct comJob : IJob
{
    [NativeDisableParallelForRestriction] public NativeArray<double3> bodyPositions;
    [NativeDisableParallelForRestriction] public NativeArray<double> bodyMasses;

    [NativeDisableParallelForRestriction] public NativeArray<double3> octantCOMs;
    [NativeDisableParallelForRestriction] public NativeArray<double> octantMasses;
    [NativeDisableParallelForRestriction] public NativeArray<double> octantSizes;
    
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyIndices;
    [NativeDisableParallelForRestriction] public NativeArray<int> octantBodyCounts;
    
    [NativeDisableParallelForRestriction] public NativeArray<int> octantTreeChildren;
    [NativeDisableParallelForRestriction] public NativeArray<byte> octantTreeChildCount;

    public void Execute()
    {
        for (int octant = 4096 - 1; octant >= 0; octant--)
        {
            if (octantSizes[octant] <= 0) continue;

            int childCount = octantBodyCounts[octant];
            int childStart = octantBodyIndices[octant];

            octantCOMs[octant] = double3.zero;
            octantMasses[octant] = 0;

            double totalMass = 0;
            double3 totalCOM = double3.zero;

            if (octantTreeChildCount[octant] > 0)
            {
                int baseChild = octant * 8;
                for (int k = 0; k < 8; k++)
                {
                    int child = octantTreeChildren[baseChild + k];
                    if (child < 0) continue;

                    double mass = octantMasses[child];
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
    [ReadOnly] public NativeArray<double3> bodyPositions;
    [ReadOnly] public NativeArray<double> bodyMasses;
    
    [ReadOnly] public NativeArray<double3> octantCOMs;
    [ReadOnly] public NativeArray<double> octantMasses;
    [ReadOnly] public NativeArray<double> octantSizes;

    [ReadOnly] public NativeArray<int> octantBodyCounts;
    [ReadOnly] public NativeArray<int> octantBodyIndices;
    
    [ReadOnly] public NativeArray<int> octantTreeChildren;
    [ReadOnly] public NativeArray<byte> octantTreeChildCount;
    
    [ReadOnly] public double gravitationalConstant;
    [ReadOnly] public double openingAngle;
    
    [WriteOnly] public NativeArray<double3> bodyForces;
    
    public void Execute(int body)
    {
        double3 force = Force(body, 0, openingAngle); 
        bodyForces[body] = force;
    }
    
    private double3 Force(int body, int startOctant, double openingAngle)
    {
        FixedList512Bytes<int> stack = new FixedList512Bytes<int>();
        stack.Add(startOctant);
        
        double3 netForce = double3.zero;
        
        while (stack.Length > 0)
        {
            int currentOctant = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            double3 distance = (octantCOMs[currentOctant] - bodyPositions[body]);
            double distanceSquared = distance.x * distance.x + distance.y * distance.y + distance.z * distance.z;
            if (distanceSquared == 0) continue;

            if (octantMasses[currentOctant] == 0) continue;
            
            int startIndex = octantBodyIndices[currentOctant];
            int endIndex = startIndex + octantBodyCounts[currentOctant] - 1;
            bool containsBody = (body >= startIndex && body <= endIndex);
        
            if (containsBody && octantBodyCounts[currentOctant] == 1) continue;
            
            bool approximate = octantSizes[currentOctant] * octantSizes[currentOctant] / distanceSquared < openingAngle * openingAngle;
            
            if (approximate && !containsBody)
            {
                netForce += (gravitationalConstant * (bodyMasses[body] * octantMasses[currentOctant]) / distanceSquared) * (distance / math.sqrt(distanceSquared));
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
                        
                        double3 bodyDistance = (bodyPositions[selectedBody] - bodyPositions[body]);
                        double bodyDistanceSquared = bodyDistance.x * bodyDistance.x + bodyDistance.y * bodyDistance.y + bodyDistance.z * bodyDistance.z;
                        if (bodyDistanceSquared == 0) continue;
                        
                        netForce += (gravitationalConstant * (bodyMasses[body] * bodyMasses[selectedBody]) / bodyDistanceSquared) * (bodyDistance / math.sqrt(bodyDistanceSquared));
                    }
                }
            }
        }
        
        return netForce;
    }
}

