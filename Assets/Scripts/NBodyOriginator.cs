using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public struct OctreeNode
{
    public DVector3 worldPosition;
    public double size;
    
    public DVector3 centerOfMass;
    public double totalMass;
    
    public List<OctreeNode> octreeChildren;
    public List<NBody> containedBodies;
}

public class NBodyOriginator : MonoBehaviour    
{
    
    [Header("General Simulation Settings")]
    
    [Tooltip("Simulation's Distance Scaling. The default is 1 billion, meaning that every 1 unity meter will amount to 1 billion simulation meters.")]public double distMultiplier = 1e9;
    [Tooltip("Simulation's Bounds in Unity (Meters).")]public double simulationBounds = 500;
    [Tooltip("How fast the simulation's step is. Default value is default 'Fixed Timestep' for FixedUpdate in Project Settings > Time.")]public double simulationTimestep = 0.02;
    
    [Header("Barnes-Hut")]
    
    [Tooltip("The s/d criterion for barnes-hut to determine if it should use the approximation of an octant or compute each body. Lower Values make it more accurate and higher values make it more performant.")]
    [Range(0, 1)]public double openingAngleCriterion = 0.5;
    
    private const double gravitationalConstant = 6.67e-11;
    private List<OctreeNode> nodes = new List<OctreeNode>();
    private List<NBody> bodies;
    private OctreeNode octreeOriginator;

    void Start()
    {
        bodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        octreeOriginator.containedBodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        
        octreeOriginator.worldPosition = DVector3.zero;
        octreeOriginator.size = simulationBounds;
        octreeOriginator.octreeChildren = new List<OctreeNode>();
    }

    void FixedUpdate()
    {
        nodes.Clear();
        nodes.Add(octreeOriginator);
        
        foreach (NBody body in bodies)
        {
            body.predictedPosition = body.currentPosition + body.currentVelocity * simulationTimestep + (body.currentAcceleration * (simulationTimestep * simulationTimestep)) * 0.5;
        }
        
        BuildOctree(octreeOriginator, 0);

        foreach (NBody body in bodies)
        {
            DVector3 resultantForce = CalculateSubtreeForce(body, octreeOriginator, openingAngleCriterion);
            DVector3 newAcceleration = resultantForce / body.mass;
            DVector3 newVelocity = body.currentVelocity + (body.currentAcceleration + newAcceleration) * 0.5 * simulationTimestep;

            body.currentAcceleration = newAcceleration;
            body.currentVelocity = newVelocity;
            body.currentPosition = body.predictedPosition;
        }
    }

    //creates octree for barnes-hut algorithm
    void BuildOctree(OctreeNode node, int recursionDepth)
    {
        if (node.containedBodies.Count > 2 && recursionDepth < 15)
        {
            recursionDepth++;
            
            OctreeNode xyz = new OctreeNode(); //+++
            xyz.worldPosition = new DVector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            xyz.size = node.size / 2;
            xyz.octreeChildren = new List<OctreeNode>();
            xyz.containedBodies = new List<NBody>();
            
            OctreeNode xNegYz = new OctreeNode(); //+-+
            xNegYz.worldPosition = new DVector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            xNegYz.size = node.size / 2;
            xNegYz.octreeChildren = new List<OctreeNode>();
            xNegYz.containedBodies = new List<NBody>();
            
            OctreeNode negXyz = new OctreeNode();//-++
            negXyz.worldPosition = new DVector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXyz.size = node.size / 2;
            negXyz.octreeChildren = new List<OctreeNode>();
            negXyz.containedBodies = new List<NBody>();
            
            OctreeNode negXNegYz = new OctreeNode();//--+
            negXNegYz.worldPosition = new DVector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXNegYz.size = node.size / 2;
            negXNegYz.octreeChildren = new List<OctreeNode>();
            negXNegYz.containedBodies = new List<NBody>();
            
            OctreeNode xyNegZ = new OctreeNode();//++-
            xyNegZ.worldPosition = new DVector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            xyNegZ.size = node.size / 2;
            xyNegZ.octreeChildren = new List<OctreeNode>();
            xyNegZ.containedBodies = new List<NBody>();
            
            OctreeNode xNegYNegZ = new OctreeNode();//+--
            xNegYNegZ.worldPosition = new DVector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z - node.size / 4f);
            xNegYNegZ.size = node.size / 2;
            xNegYNegZ.octreeChildren = new List<OctreeNode>();
            xNegYNegZ.containedBodies = new List<NBody>();
            
            OctreeNode negXyNegZ = new OctreeNode();//-+-
            negXyNegZ.worldPosition = new DVector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            negXyNegZ.size = node.size / 2;
            negXyNegZ.octreeChildren = new List<OctreeNode>();
            negXyNegZ.containedBodies = new List<NBody>();
            
            OctreeNode negXNegYNegZ = new OctreeNode();//---
            negXNegYNegZ.worldPosition = new DVector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z - node.size / 4f);
            negXNegYNegZ.size = node.size / 2;
            negXNegYNegZ.octreeChildren = new List<OctreeNode>();
            negXNegYNegZ.containedBodies = new List<NBody>();
            
            node.octreeChildren.Add(xyz);
            node.octreeChildren.Add(xNegYz);
            node.octreeChildren.Add(negXyz);
            node.octreeChildren.Add(negXNegYz);
            node.octreeChildren.Add(xyNegZ);
            node.octreeChildren.Add(xNegYNegZ);
            node.octreeChildren.Add(negXyNegZ);
            node.octreeChildren.Add(negXNegYNegZ);
            
            nodes.Add(xyz);
            nodes.Add(xNegYz);
            nodes.Add(negXyz);
            nodes.Add(negXNegYz);
            nodes.Add(xyNegZ);
            nodes.Add(xNegYNegZ);
            nodes.Add(negXyNegZ);
            nodes.Add(negXNegYNegZ);

            for (int j = node.containedBodies.Count - 1; j >= 0; j--)
            {
                NBody evaluatedBody = node.containedBodies[j];
                DVector3 evaluatedBodyPosition = evaluatedBody.gameObject.transform.position;

                DVector3 normalizedDirection = DVector3.zero;

                if (evaluatedBodyPosition.x > node.worldPosition.x)
                {
                    normalizedDirection.x += 1;
                }
                else
                {
                    normalizedDirection.x -= 1;
                }

                if (evaluatedBodyPosition.y > node.worldPosition.y)
                {
                    normalizedDirection.y += 1;
                }
                else
                {
                    normalizedDirection.y -= 1;
                }

                if (evaluatedBodyPosition.z > node.worldPosition.z)
                {
                    normalizedDirection.z += 1;
                }
                else
                {
                    normalizedDirection.z -= 1;
                }


                if (normalizedDirection == new DVector3(1, 1, 1))
                {
                    xyz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(1, -1, 1))
                {
                    xNegYz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(-1, 1, 1))
                {
                    negXyz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(-1, -1, 1))
                {
                    negXNegYz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(1, 1, -1))
                {
                    xyNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(1, -1, -1))
                {
                    xNegYNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(-1, 1, -1))
                {
                    negXyNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new DVector3(-1, -1, -1))
                {
                    negXNegYNegZ.containedBodies.Add(evaluatedBody);
                }
                
                node.containedBodies.RemoveAt(j);
            }
            
            if (xyz.containedBodies.Count > 0)
            {
                BuildOctree(xyz, recursionDepth);
            }
                
            if (xNegYz.containedBodies.Count > 0)
            {
                BuildOctree(xNegYz, recursionDepth);
            }
                
            if (negXyz.containedBodies.Count > 0)
            {
                BuildOctree(negXyz, recursionDepth);
            }
                
            if (negXNegYz.containedBodies.Count > 0)
            {
                BuildOctree(negXNegYz, recursionDepth);
            }
                
            if (xyNegZ.containedBodies.Count > 0)
            {
                BuildOctree(xyNegZ, recursionDepth);
            }
                
            if (xNegYNegZ.containedBodies.Count > 0)
            {
                BuildOctree(xNegYNegZ, recursionDepth);
            }
                
            if (negXyNegZ.containedBodies.Count > 0)
            {
                BuildOctree(negXyNegZ, recursionDepth);
            }
                
            if (negXNegYNegZ.containedBodies.Count > 0)
            {
                BuildOctree(negXNegYNegZ, recursionDepth);
            }
            
            node.centerOfMass = DVector3.zero;
            node.totalMass = 0;
            
            for (int i = 0; i < node.octreeChildren.Count; i++)
            {
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
            node.octreeChildren = new List<OctreeNode>();
            node.octreeChildren.Clear();
            
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

    public DVector3 CalculateSubtreeForce(NBody queryBody, OctreeNode startNode, double openingAngle)
    {
        DVector3 resultant = DVector3.zero;
        
        if (startNode.octreeChildren.Count > 0)
        {
            if (startNode.size / (startNode.centerOfMass - queryBody.predictedPosition).magnitude < openingAngle)
            {
                
                double resultantMagnitude = gravitationalConstant * (startNode.totalMass * queryBody.mass / System.Math.Pow((startNode.centerOfMass - queryBody.predictedPosition).magnitude * distMultiplier, 2));
                
                DVector3 resultantDirection = (startNode.centerOfMass - queryBody.predictedPosition).normalized;
                
                resultant += resultantDirection * resultantMagnitude;
            }
            else
            {
                foreach (OctreeNode child in startNode.octreeChildren)
                {
                    resultant += CalculateSubtreeForce(queryBody, child, openingAngle);
                }
            }
        }
        else
        {
            foreach (NBody otherBody in startNode.containedBodies)
            {
                if (otherBody == queryBody) continue;
                
                double resultantMagnitude = gravitationalConstant * (otherBody.mass * queryBody.mass / System.Math.Pow((otherBody.predictedPosition - queryBody.predictedPosition).magnitude * distMultiplier, 2));
                
                DVector3 resultantDirection = (otherBody.predictedPosition - queryBody.predictedPosition).normalized;
                
                resultant += resultantDirection * resultantMagnitude;
            }
        }

        return resultant;
    }
    
}
