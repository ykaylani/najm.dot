using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
    
    public ushort poolStartMarker;
}

public class BodyStore
{
    public NativeArray<double3> positions;
    public NativeArray<double3> velocities;
    public NativeArray<double> masses;

    public NativeArray<ulong> encodings;
    
    public NativeArray<double4x2> keplerianParams; //m00 is semimajor axis, m01 is eccentricity, m02 is longitude of ascending node, m03 is inclination, m10 is periapsis, m11 is true anomaly, m12 is orbitingAround, and m13 is calculating semimajor axis toggle.
}

[RequireComponent(typeof(BodyFrontend))]
public class Propagator : MonoBehaviour
{
    [Tooltip("x is simulation scale, y is simulation timestep, z is bounds, w is padding")]
    public double4 simulationSettings = new double4(1e9, 0.02, 200, 10);
    public double simulationTime = 1;
    
    [Tooltip("The s/d criterion for barnes-hut to determine if it should use the approximation of an octant or compute each body. Lower Values make it more accurate and higher values make it more performant.")]
    [Range(0, 1)]public float openingAngleCriterion = 0.5f;
    
    private  double gravitationalConstant = 6.67e-11;
    private const short maxOctants = 4096;
    private const short maxRecursions = 4096;
    
    public BodyStore bodies = new BodyStore();
    private OctantStore octants = new OctantStore();

    private NativeQueue<int> octantProcessingQueue;

    private NativeArray<double3> bodyForces;
    
    void Start()
    {
        gravitationalConstant /= simulationSettings.x * simulationSettings.x * simulationSettings.x;
        gravitationalConstant *= simulationTime * simulationTime;
        
        octants.positions = new NativeArray<double3>(maxOctants, Allocator.Persistent);
        octants.coms = new NativeArray<double3>(maxOctants, Allocator.Persistent);
        octants.masses = new NativeArray<double>(maxOctants, Allocator.Persistent);
        octants.sizes = new NativeArray<double>(maxOctants, Allocator.Persistent);
        
        octants.bodyIndices = new NativeArray<int>(maxOctants, Allocator.Persistent);
        for(int i = 0; i < octants.bodyIndices.Length; i++) octants.bodyIndices[i] = i;

        octants.bodyCounts = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildCount = new NativeArray<byte>(maxOctants, Allocator.Persistent);
        octants.depths = new NativeArray<int>(maxOctants, Allocator.Persistent);
        octants.treeChildren = new NativeArray<int>(maxOctants * 8, Allocator.Persistent);

        octantProcessingQueue = new NativeQueue<int>(Allocator.Persistent);
        bodies.encodings = new NativeArray<ulong>(bodies.positions.Length, Allocator.Persistent);
        bodyForces = new NativeArray<double3>(bodies.positions.Length, Allocator.Persistent);
        
        octants.bodyCounts[0] = bodies.positions.Length;

        for (int i = 0; i < bodies.positions.Length; i++)
        {
            if (bodies.keplerianParams[i][1][3] > 0 && !(bodies.keplerianParams[i][1][2] < 0))
            {
                double3 relativePosition = bodies.positions[i] - bodies.positions[(int)bodies.keplerianParams[i][1][2]];
                
                double4x2 kp = bodies.keplerianParams[i];
                kp[0][0] = math.length(relativePosition);
                bodies.keplerianParams[i] = kp;
            }

            bodies.velocities[i] = CalculateInitialVelocity(i);
        }
        
        bodies.keplerianParams.Dispose();
    }

    JobHandle forceHandle;
    int scheduledFrames = -1;

    void FixedUpdate()
    {
        if (scheduledFrames != -1) scheduledFrames++;
        
        if (scheduledFrames > 0)
        {
            forceHandle.Complete();
            scheduledFrames = -1;
            
            for (int i = 0; i < bodies.positions.Length; i++)
            {
                double3 force = bodyForces[i];
                bodies.velocities[i] += Time.fixedDeltaTime * (force / bodies.masses[i]);
                bodies.positions[i] += Time.fixedDeltaTime * bodies.velocities[i];
            }

            for(int i = 0; i <= bodyForces.Length - 1; i++) bodyForces[i] = double3.zero;
            octants.poolStartMarker = 0;
        }
        
        for (int i = 0; i < octants.masses.Length; i++)
        {
            octants.bodyCounts[i] = 0;
            octants.treeChildCount[i] = 0;
            octants.depths[i] = 0;
            octants.masses[i] = 0;
            octants.coms[i] = double3.zero;
            octants.positions[i] = double3.zero;
            octants.sizes[i] = -1;
        }
        
        for (int i = 0; i < octants.treeChildren.Length; i++) 
        {
            octants.treeChildren[i] = -1;
        }
        
        octants.poolStartMarker = 0;
        
        octants.positions[0] = double3.zero;
        octants.sizes[0] = simulationSettings.z + simulationSettings.w;
        octants.depths[0] = 0;
        octants.bodyCounts[0] = bodies.positions.Length;
        
        BuildOctree(0);

        ForceJob forceJob = new ForceJob
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
        
        forceHandle = forceJob.Schedule(bodies.positions.Length, 64);
        scheduledFrames = 0;
    }
    
    void OnDestroy()
    {
        if (!forceHandle.IsCompleted) forceHandle.Complete();
        
        octants.positions.Dispose();
        octants.coms.Dispose();
        octants.sizes.Dispose();
        
        octants.bodyIndices.Dispose();
        octants.bodyCounts.Dispose();
        octants.treeChildCount.Dispose();
        
        bodies.positions.Dispose();
        bodies.velocities.Dispose();
        bodies.masses.Dispose();
        
        octantProcessingQueue.Dispose();
        bodies.encodings.Dispose();

        bodyForces.Dispose();
    }

    double3 CalculateInitialVelocity(int body)
    {
        if (bodies.keplerianParams[body][1][2] >= 0) 
        {
            double4x2 keplerianParams = bodies.keplerianParams[body];
            
            double4 transformation = new double4(0, 0, 0, gravitationalConstant * bodies.masses[(int)keplerianParams[1][2]]);

            double eccentricity = keplerianParams[0][1];
            
            double factor = transformation.w / math.sqrt(transformation.w * keplerianParams[0][0] * (1 - eccentricity * eccentricity));

            double trueAnomaly = keplerianParams[1][1];
            double periapsis = keplerianParams[1][0];
            double inclination = keplerianParams[0][3];
            double ascendingNode = keplerianParams[0][2];
            
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
            return final;
        }
        
        return bodies.velocities[body];
    }
    

    void BuildOctree(int root)
    {
        for (int i = 0; i < bodies.positions.Length; i++)
        {
            double simulation = (simulationSettings.z / 2) + (simulationSettings.w / 2);
            double3 maxBounds = new double3(simulation, simulation, simulation);
            
            double3 position = bodies.positions[i];
            bodies.encodings[i] = Encoder.Morton(position, -maxBounds, maxBounds);
        }
        
        Sorter.QS(bodies.encodings, 0, bodies.encodings.Length - 1, bodies.positions, bodies.velocities, bodies.masses);
        
        octantProcessingQueue.Enqueue(root);

        while (octantProcessingQueue.Count > 0)
        {
            int currentOctant = octantProcessingQueue.Dequeue();
            int depth = octants.depths[currentOctant];
            
            if (depth >= maxRecursions || octants.bodyCounts[currentOctant] <= 1) continue;

            int firstBody = octants.bodyIndices[currentOctant];
            int lastBody = firstBody + octants.bodyCounts[currentOctant] - 1;

            int currentStart = firstBody;
            int currentChild = Encoder.ChildIndex(bodies.encodings[currentStart], depth + 1);

            for (int i = firstBody + 1; i <= lastBody; i++)
            {
                int child = Encoder.ChildIndex(bodies.encodings[i], depth + 1);
                if (child != currentChild)
                {
                    if (i - 1 >= currentStart) InitializeOctant(currentOctant, currentStart, i - 1, currentChild, depth, octantProcessingQueue);
                    currentStart = i;
                    currentChild = child;
                }
            }
            if (lastBody >= currentStart) InitializeOctant(currentOctant, currentStart, lastBody, currentChild, depth, octantProcessingQueue);
        }

        octantProcessingQueue.Clear();

        for (int i = octants.poolStartMarker + 1; i < octants.positions.Length; i++)
        {
            octants.positions[i] = double3.zero;
            octants.coms[i] = double3.zero;
            octants.sizes[i] = -1;
            octants.masses[i] = 0;
        }

        for (int i = octants.poolStartMarker; i >= 0; i--)
        {
            if (octants.sizes[i] <= 0) continue;
            
            int childCount = octants.bodyCounts[i];
            int childStart = octants.bodyIndices[i];
            
            octants.coms[i] = double3.zero;
            octants.masses[i] = 0;

            double totalMass = 0;
            double3 totalCOM = double3.zero;

            for (int j = 0; j < childCount; j++)
            {
                int currentChild = childStart + j;
                totalMass += bodies.masses[currentChild];
                totalCOM += bodies.positions[currentChild] * bodies.masses[currentChild];
            }
            
            int baseChild = i * 8;
            
            for (int k = 0; k < 8; k++)
            {
                int child = octants.treeChildren[baseChild + k];
                if (child < 0) continue;
                
                double mass = octants.masses[child];
                if (mass <= 0.0) continue;
                
                totalMass += mass;
                totalCOM += octants.coms[child] * mass;
            }
            
            if (totalMass > 0) totalCOM /= totalMass;
            
            octants.coms[i] = totalCOM;
            octants.masses[i] = totalMass;
            
        }
    }
    
    void InitializeOctant(int parent, int start, int end, int child, int depth, NativeQueue<int> processQueue)
    {
        if (start > end) return;
        octants.poolStartMarker++;
        int childOctant = octants.poolStartMarker;

        octants.bodyIndices[childOctant] = start;
        octants.bodyCounts[childOctant] = end - start + 1;
        octants.depths[childOctant] = depth + 1;
        
        double parentSize = octants.sizes[parent];
        
        octants.sizes[childOctant] = parentSize / 2;
        double3 childOffset = new double3((child & 1) != 0 ? 0.25 : -0.25, (child & 2) != 0 ? 0.25 : -0.25, (child & 4) != 0 ? 0.25 : -0.25) * parentSize;
        octants.positions[childOctant] = octants.positions[parent] + childOffset;

        octants.treeChildren[parent * 8 + child] = childOctant;
        octants.treeChildCount[parent]++;
        
        if (end - start + 1 > 1 && depth + 1 < maxRecursions) processQueue.Enqueue(childOctant);    
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
    
    public NativeArray<double3> bodyForces;
    
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
            
            if (octantSizes[currentOctant] * octantSizes[currentOctant] / distanceSquared < openingAngle * openingAngle)
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
