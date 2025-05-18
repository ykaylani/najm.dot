using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public struct OctreeNode
{
    public Vector3 worldPosition;
    public float size;
    
    public Vector3 centerOfMass;
    public float totalMass;
    
    public List<OctreeNode> octreeChildren;
    public List<NBody> containedBodies;
}

public class NBodyOriginator : MonoBehaviour    
{
    public float gravitationalConstant = 6.67e-11f;
    public float distMultiplier = 100e10f; // 2.5 billion times bigger

    public float simulationBounds = 100f;
    
    private List<NBody> bodies;

    //barnes-hut
    [HideInInspector] public List<OctreeNode> nodes = new List<OctreeNode>();
    private OctreeNode octreeOriginator;
 
    public float openingAngleCriterion = 0.3f;
    public float simulationTimestep;

    void Start()
    {
        bodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        octreeOriginator.containedBodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        
        octreeOriginator.worldPosition = Vector3.zero;
        octreeOriginator.size = simulationBounds;
        octreeOriginator.octreeChildren = new List<OctreeNode>();
    }

    void FixedUpdate()
    {
        nodes.Clear();
        nodes.Add(octreeOriginator);
        
        foreach (NBody body in bodies)
        {
            body.predictedPosition = body.currentPosition + body.currentVelocity * simulationTimestep + 0.5f * (body.currentAcceleration * (simulationTimestep * simulationTimestep));
        }
        
        BuildOctree(octreeOriginator);

        foreach (NBody body in bodies)
        {
            Vector3 resultantForce = CalculateSubtreeForce(body, octreeOriginator, openingAngleCriterion);
            
            Vector3 newAcceleration = resultantForce / body.mass;
            Vector3 newVelocity = body.currentVelocity + 0.5f * (body.currentAcceleration + newAcceleration) * simulationTimestep;

            body.currentAcceleration = newAcceleration;
            body.currentVelocity = newVelocity;
            body.currentPosition = body.predictedPosition;
        }
    }

    //creates octree for barnes-hut algorithm
    void BuildOctree(OctreeNode node)
    {
        if (node.containedBodies.Count > 3)
        {
            OctreeNode xyz = new OctreeNode(); //+++
            xyz.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            xyz.size = node.size / 2;
            xyz.octreeChildren = new List<OctreeNode>();
            xyz.containedBodies = new List<NBody>();
            
            OctreeNode xNegYz = new OctreeNode(); //+-+
            xNegYz.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            xNegYz.size = node.size / 2;
            xNegYz.octreeChildren = new List<OctreeNode>();
            xNegYz.containedBodies = new List<NBody>();
            
            OctreeNode negXyz = new OctreeNode();//-++
            negXyz.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXyz.size = node.size / 2;
            negXyz.octreeChildren = new List<OctreeNode>();
            negXyz.containedBodies = new List<NBody>();
            
            OctreeNode negXNegYz = new OctreeNode();//--+
            negXNegYz.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXNegYz.size = node.size / 2;
            negXNegYz.octreeChildren = new List<OctreeNode>();
            negXNegYz.containedBodies = new List<NBody>();
            
            OctreeNode xyNegZ = new OctreeNode();//++-
            xyNegZ.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            xyNegZ.size = node.size / 2;
            xyNegZ.octreeChildren = new List<OctreeNode>();
            xyNegZ.containedBodies = new List<NBody>();
            
            OctreeNode xNegYNegZ = new OctreeNode();//+--
            xNegYNegZ.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z - node.size / 4f);
            xNegYNegZ.size = node.size / 2;
            xNegYNegZ.octreeChildren = new List<OctreeNode>();
            xNegYNegZ.containedBodies = new List<NBody>();
            
            OctreeNode negXyNegZ = new OctreeNode();//-+-
            negXyNegZ.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            negXyNegZ.size = node.size / 2;
            negXyNegZ.octreeChildren = new List<OctreeNode>();
            negXyNegZ.containedBodies = new List<NBody>();
            
            OctreeNode negXNegYNegZ = new OctreeNode();//---
            negXNegYNegZ.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z - node.size / 4f);
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
                Vector3 evaluatedBodyPosition = evaluatedBody.gameObject.transform.position;

                Vector3 normalizedDirection = Vector3.zero;

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


                if (normalizedDirection == new Vector3(1, 1, 1))
                {
                    xyz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(1, -1, 1))
                {
                    xNegYz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(-1, 1, 1))
                {
                    negXyz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(-1, -1, 1))
                {
                    negXNegYz.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(1, 1, -1))
                {
                    xyNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(1, -1, -1))
                {
                    xNegYNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(-1, 1, -1))
                {
                    negXyNegZ.containedBodies.Add(evaluatedBody);
                }
                else if (normalizedDirection == new Vector3(-1, -1, -1))
                {
                    negXNegYNegZ.containedBodies.Add(evaluatedBody);
                }
                
                node.containedBodies.RemoveAt(j);
            }
            
            if (xyz.containedBodies.Count > 0)
            {
                BuildOctree(xyz);
            }
                
            if (xNegYz.containedBodies.Count > 0)
            {
                BuildOctree(xNegYz);
            }
                
            if (negXyz.containedBodies.Count > 0)
            {
                BuildOctree(negXyz);
            }
                
            if (negXNegYz.containedBodies.Count > 0)
            {
                BuildOctree(negXNegYz);
            }
                
            if (xyNegZ.containedBodies.Count > 0)
            {
                BuildOctree(xyNegZ);
            }
                
            if (xNegYNegZ.containedBodies.Count > 0)
            {
                BuildOctree(xNegYNegZ);
            }
                
            if (negXyNegZ.containedBodies.Count > 0)
            {
                BuildOctree(negXyNegZ);
            }
                
            if (negXNegYNegZ.containedBodies.Count > 0)
            {
                BuildOctree(negXNegYNegZ);
            }
            
            node.centerOfMass = Vector3.zero;
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

    public Vector3 CalculateSubtreeForce(NBody queryBody, OctreeNode startNode, float openingAngle)
    {
        Vector3 resultant = Vector3.zero;
        
        if (startNode.octreeChildren.Count > 0)
        {
            if (startNode.size / (startNode.centerOfMass - queryBody.predictedPosition).magnitude < openingAngle)
            {
                float resultantMagnitude = gravitationalConstant * (startNode.totalMass * queryBody.mass / ((startNode.centerOfMass - queryBody.predictedPosition).sqrMagnitude * distMultiplier));
                Vector3 resultantDirection = (startNode.centerOfMass - queryBody.predictedPosition).normalized;
                
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
                
                float resultantMagnitude = gravitationalConstant * (otherBody.mass * queryBody.mass / ((otherBody.predictedPosition - queryBody.predictedPosition).sqrMagnitude * distMultiplier));
                Vector3 resultantDirection = (otherBody.predictedPosition - queryBody.predictedPosition).normalized;
                
                resultant += resultantDirection * resultantMagnitude;
            }
        }

        return resultant;
    }
    
}
