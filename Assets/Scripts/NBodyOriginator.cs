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
    public float distMultiplier = 2.5e9f; // 2.5 billion times bigger

    public float simulationBounds = 100f;
    
    private List<NBody> bodies;

    //barnes-hut
    [HideInInspector] public List<OctreeNode> nodes;
    private OctreeNode octreeOriginator;

    public float openingAngleCriterion = 0.3f;

    void Start()
    {
        bodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        
        octreeOriginator.worldPosition = Vector3.zero;
        octreeOriginator.size = simulationBounds;
        octreeOriginator.containedBodies = FindObjectsByType<NBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
    }

    void FixedUpdate()
    {
        nodes.Clear();
        nodes.Add(octreeOriginator);
        BuildOctree(octreeOriginator);

        foreach (NBody body in bodies)
        {
            body.leadForce = ForceAccumulation(body, octreeOriginator, openingAngleCriterion);
        }
    }

    //creates octree for barnes-hut algorithm
    void BuildOctree(OctreeNode node)
    {
        if (node.containedBodies.Count > 3)
        {
            OctreeNode xyz = new OctreeNode();
            xyz.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            xyz.size = node.size / 2;
            
            OctreeNode xNegYz = new OctreeNode();
            xNegYz.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            xNegYz.size = node.size / 2;
            
            OctreeNode negXyz = new OctreeNode();
            negXyz.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXyz.size = node.size / 2;
            
            OctreeNode negXNegYz = new OctreeNode();
            negXNegYz.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            negXNegYz.size = node.size / 2;
            
            OctreeNode xyNegZ = new OctreeNode();
            xyNegZ.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            xyNegZ.size = node.size / 2;
            
            OctreeNode xNegYNegZ = new OctreeNode();
            xNegYNegZ.worldPosition = new Vector3(node.worldPosition.x + node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z + node.size / 4f);
            xNegYNegZ.size = node.size / 2;
            
            OctreeNode negXyNegZ = new OctreeNode();
            negXyNegZ.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y + node.size / 4f, node.worldPosition.z - node.size / 4f);
            negXyNegZ.size = node.size / 2;
            
            OctreeNode negXNegYNegZ = new OctreeNode();
            negXNegYNegZ.worldPosition = new Vector3(node.worldPosition.x - node.size / 4f, node.worldPosition.y - node.size / 4f, node.worldPosition.z - node.size / 4f);
            negXNegYNegZ.size = node.size / 2;
            
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
                else if (normalizedDirection == Vector3.zero)
                {
                    negXyz.containedBodies.Add(evaluatedBody);
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
        }
        else
        {
            node.octreeChildren = null;
            
            for (int i = 0; i < node.containedBodies.Count; i++)
            {
                node.centerOfMass += node.containedBodies[i].gameObject.transform.position * node.containedBodies[i].mass;
                node.totalMass += node.containedBodies[i].mass;
            }

            if (node.totalMass > 0)
            {
                node.centerOfMass /= node.totalMass;
            }
        }
    }

    public Vector3 ForceAccumulation(Vector3 bodyPosition, float bodyMass, OctreeNode rootNode, float openingAngleCriterion)
    {
        for (int i = 0; i < originator.nodes.Count; i++)
        {
            OctreeNode node = originator.nodes[i];

            Vector3 otherPosition = Vector3.zero;
            float otherMass;

            if (node.octreeChildren == null)
            {
                foreach (NBody otherBody in node.containedBodies)
                {
                    otherPosition = otherBody.transform.position;
                    otherMass =  otherBody.mass;
                    
                    float distance = Vector3.Distance(objectPosition, otherPosition) * distMultiplier;

                    float pullMagnitude = (originator.gravitationalConstant * mass * otherMass) / (distance * distance + 1f);
                    Vector3 pullDirection = (otherPosition - objectPosition).normalized;

                    Vector3 total = pullDirection * pullMagnitude;
                    leadForce += total;
                }
            }
            else
            {
                if (node.size / Vector3.Distance(objectPosition, node.worldPosition) < originator.openingAngleCriterion)
                {
                    otherPosition = node.centerOfMass;
                    otherMass = node.totalMass;
                    
                    float distance = Vector3.Distance(objectPosition, otherPosition) * distMultiplier;

                    float pullMagnitude = (originator.gravitationalConstant * mass * otherMass) / (distance * distance + 1f);
                    Vector3 pullDirection = (otherPosition - objectPosition).normalized;

                    Vector3 total = pullDirection * pullMagnitude;
                    leadForce += total;
                }
                else
                {
                    foreach (OctreeNode containedNode in node.octreeChildren)
                    {
                        AccumulateForce(transform.position, originator.nodes.IndexOf(containedNode));
                    }
                }
            }
        }
    }
    
}
