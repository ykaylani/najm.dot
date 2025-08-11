using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class Octant
{
    public DVector3 worldPosition;
    public double size;
    
    public DVector3 centerOfMass;
    public double totalMass;
    
    public Octant[] octreeChildren;
    public List<Body> containedBodies;
}

public class Propagator : MonoBehaviour
{
    [Tooltip("x is simulation scale, y is simulation timestep, z is bounds, w is padding")]
    public double4 simulationSettings = new double4(1e9, 0.02, 200, 10);
    
    [Tooltip("The s/d criterion for barnes-hut to determine if it should use the approximation of an octant or compute each body. Lower Values make it more accurate and higher values make it more performant.")]
    [Range(0, 1)]public double openingAngleCriterion = 0.5;
    
    [Tooltip("If simulation bounds will always try to contain all bodies (This can be problematic if a body is launched due to gravitational singularity, so I recommend keeping this off.)")]public bool adaptiveSimulationBounds;
    
    private const double gravitationalConstant = 6.67e-11;
    private List<Body> bodies;
    private Octant octreeOriginator = new Octant();
    
    void Start()
    {
        bodies = FindObjectsByType<Body>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        octreeOriginator.containedBodies = new List<Body>(bodies);
        octreeOriginator.octreeChildren = null;
        octreeOriginator.worldPosition = DVector3.zero;
        octreeOriginator.size = simulationSettings.z + simulationSettings.w;

        foreach (Body body in bodies)
        {
            if (body.calculateSemimajorAxis && body.centralBody != null)
            {
                DVector3 relativePosition = body.currentPosition - body.centralBody.currentPosition;
                body.semimajorAxis = relativePosition.magnitude * simulationSettings.x;
            }

            double4 x = CalculateInitialVelocity(body);
            body.initialVelocity = new DVector3(x.x, x.y, x.z);
            body.currentVelocity = body.initialVelocity;

        }
    }

    void FixedUpdate()
    {
        DVector3 farthestBody = PredictPositions(bodies);
        SimulationBoundsUpdate(farthestBody);
        
        octreeOriginator = new Octant();
        octreeOriginator.worldPosition = DVector3.zero;
        octreeOriginator.size = simulationSettings.z + simulationSettings.w;
        octreeOriginator.octreeChildren = null;
        octreeOriginator.containedBodies = new List<Body>(bodies);
        
        BuildOctree(octreeOriginator, 0);
        ExecuteForceCalculations(bodies);
    }

    double4 CalculateInitialVelocity(Body body)
    {
        if (body.keplerianOrbits)
        {
            if (!body.centralBody)
            {
                Debug.LogWarning($"{body} has No Central Body Assigned to Orbit around! If it is the central body, please turn off 'Keplerian Orbits'.");
                double4 ivr = double4.zero;
                ivr.x = body.initialVelocity.x;
                ivr.y = body.initialVelocity.y;
                ivr.z = body.initialVelocity.z;
                return ivr;
            }

            double4 transformation = new double4(0, 0, 0, (gravitationalConstant * body.centralBody.mass) / simulationSettings.x); 
            
            double factor = transformation.w / System.Math.Sqrt(transformation.w * body.semimajorAxis * (1 - System.Math.Pow(body.eccentricity, 2)));
            double taSin = System.Math.Sin(body.trueAnomaly);
            double taCos = System.Math.Cos(body.trueAnomaly);
            double periapsisSin = System.Math.Sin(body.argumentOfPeriapsis);
            double periapsisCos = System.Math.Cos(body.argumentOfPeriapsis);
            double inclinationSin = System.Math.Sin(body.inclination);
            double inclinationCos = System.Math.Cos(body.inclination);
            double ascendingNodeSin = System.Math.Sin(body.ascendingNodeLongitude);
            double ascendingNodeCos = System.Math.Cos(body.ascendingNodeLongitude);
            
            //perifocal and periapsis rotations
            transformation.x = -factor * taSin * periapsisCos - factor * (body.eccentricity + taCos) * periapsisSin;
            transformation.y = -factor * taSin * periapsisSin + factor * (body.eccentricity + taCos) * periapsisCos;
            transformation.z = 0;
            
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
            
            return transformation;
        }

        double4 iv = double4.zero;
        iv.x = body.initialVelocity.x;
        iv.y = body.initialVelocity.y;
        iv.z = body.initialVelocity.z;
        return iv;
    }

    DVector3 PredictPositions(List<Body> lBodies)
    {
        DVector3 farthestBody = DVector3.zero;
        
        foreach (Body body in lBodies)
        {
            body.predictedPosition = body.currentPosition + body.currentVelocity * simulationSettings.y + (body.currentAcceleration * (simulationSettings.y * simulationSettings.y)) * 0.5;

            if (adaptiveSimulationBounds)
            {
                if (body.predictedPosition.magnitude > farthestBody.magnitude)
                {
                    farthestBody = body.predictedPosition;
                }
            }
        }
        
        return farthestBody;
    }

    void SimulationBoundsUpdate(DVector3 farthestBody)
    {
        if (adaptiveSimulationBounds)
        {
            simulationSettings.z = farthestBody.magnitude;
            octreeOriginator.size = simulationSettings.z + simulationSettings.w;
        }

    }

    void BuildOctree(Octant node, int recursionDepth)
    {
        if (node.containedBodies.Count > 2 && recursionDepth < 15)
        {
            if (node.octreeChildren == null)
            {
                node.octreeChildren = new Octant[8];
            }
            
            recursionDepth++;
            
            for (int j = node.containedBodies.Count - 1; j >= 0; j--)
            {
                Body body = node.containedBodies[j];
                
                int3 signage = int3.zero;
                
                short index = 0;
                if (body.predictedPosition.x > node.worldPosition.x) {index |= 1; signage.x += 1; } else {signage.x -= 1;}
                if (body.predictedPosition.y > node.worldPosition.y) {index |= 2; signage.y += 1; } else {signage.y -= 1;}
                if (body.predictedPosition.z > node.worldPosition.z) {index |= 4; signage.z += 1; } else {signage.z -= 1;}

                if (node.octreeChildren[index] == null)
                {
                    
                    Octant newOctant = new Octant
                    {
                        worldPosition = new DVector3(node.worldPosition.x + node.size / 4f * signage.x, node.worldPosition.y + node.size / 4f * signage.y, node.worldPosition.z + node.size / 4f * signage.z),
                        size = node.size / 2,
                        octreeChildren = new Octant[8],
                        containedBodies = new List<Body>()
                    };
                    
                    node.octreeChildren[index] = newOctant;
                    newOctant.containedBodies.Add(body);
                    node.containedBodies.RemoveAt(j);
                }
                else
                {
                    node.octreeChildren[index].containedBodies.Add(body);
                    node.containedBodies.RemoveAt(j);
                }
            }

            foreach (Octant octant in node.octreeChildren)
            {
                if (octant == null) continue;
                if (octant.containedBodies.Count > 0)
                {
                    BuildOctree(octant, recursionDepth);
                }
            }
            
            node.centerOfMass = DVector3.zero;
            node.totalMass = 0;
            
            for (int i = 0; i < node.octreeChildren.Length; i++)
            {
                if (node.octreeChildren[i] == null) continue;
                node.centerOfMass += node.octreeChildren[i].centerOfMass * node.octreeChildren[i].totalMass;
                node.totalMass += node.octreeChildren[i].totalMass;
            }
            
            if (node.totalMass > 0)
            {
                node.centerOfMass /= node.totalMass;
            }
            
        }
        else
        {
            node.octreeChildren = new Octant[8];
            node.centerOfMass = DVector3.zero;
            node.totalMass = 0;
            
            for (int i = 0; i < node.containedBodies.Count; i++)
            {
                node.centerOfMass += node.containedBodies[i].predictedPosition * node.containedBodies[i].mass;
                node.totalMass += node.containedBodies[i].mass;
            }

            if (node.totalMass > 0)
            {
                node.centerOfMass /= node.totalMass;
            }
        }
    }

    public DVector3 CalculateSubtreeForce(Body queryBody, Octant startNode, double openingAngle)
    {
        DVector3 resultant = DVector3.zero;
        bool found = false;

        foreach (Octant octant in startNode.octreeChildren)
        {
            if (octant != null)
            {
                found = true;
                break;
            }
        }
        
        if (found)
        {
            if (startNode.size / (startNode.centerOfMass - queryBody.predictedPosition).magnitude < openingAngle)
            {
                double resultantMagnitude = gravitationalConstant * (startNode.totalMass * queryBody.mass / System.Math.Pow((startNode.centerOfMass - queryBody.predictedPosition).magnitude * simulationSettings.x, 2));
                DVector3 resultantDirection = (startNode.centerOfMass - queryBody.predictedPosition).normalized;
                resultant += resultantDirection * resultantMagnitude;
            }
            else
            {
                foreach (Octant child in startNode.octreeChildren)
                {
                    if (child == null) continue;
                    resultant += CalculateSubtreeForce(queryBody, child, openingAngle);
                }
            }
        }
        else
        {
            foreach (Body otherBody in startNode.containedBodies)
            {
                if (otherBody == queryBody) continue;
                
                double resultantMagnitude = gravitationalConstant * (otherBody.mass * queryBody.mass / System.Math.Pow((otherBody.predictedPosition - queryBody.predictedPosition).magnitude * simulationSettings.x, 2));
                DVector3 resultantDirection = (otherBody.predictedPosition - queryBody.predictedPosition).normalized;
                resultant += resultantDirection * resultantMagnitude;
            }
        }

        return resultant;
    }
    
    void ExecuteForceCalculations(List<Body> nBodies)
    {
        foreach (Body body in nBodies)
        {
            DVector3 resultantForce = CalculateSubtreeForce(body, octreeOriginator, openingAngleCriterion);
            DVector3 newAcceleration = resultantForce / body.mass;
            DVector3 newVelocity = body.currentVelocity + (body.currentAcceleration + newAcceleration) * 0.5 * simulationSettings.y;

            body.currentAcceleration = newAcceleration;
            body.currentVelocity = newVelocity;
            body.currentPosition = body.predictedPosition;
        }
    }
    
}
