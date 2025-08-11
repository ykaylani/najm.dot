using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class Body : MonoBehaviour
{
    [HideInInspector]public DVector3 currentAcceleration;
    [HideInInspector]public DVector3 currentVelocity;
    [HideInInspector]public DVector3 currentPosition;
    
    [HideInInspector]public DVector3 predictedPosition;
    
    [HideInInspector]public LineRenderer lineRenderer;
    [HideInInspector]public List<Vector3> orbitPoints;
    
    [Tooltip("Body's Mass in kilograms.")] public double mass = 5.972e24; // Earth mass default
    
    [Tooltip("Whether or not the initial velocity of the object will be calculated using the keplerian properties")]public bool keplerianOrbits = true;
    [Tooltip("The body that this body will orbit around")][HideInInspector]public Body centralBody;
    [Tooltip("Determines how elliptical the orbit is (0 is circular, 1 is very elongated)")][HideInInspector]public double eccentricity = 0.1;
    [Tooltip("The orbit's longest radius in real-world units (simulation units * distMultiplier)")][HideInInspector]public double semimajorAxis = 1.5e11;
    public bool calculateSemimajorAxis = true;
    [Tooltip("The angle (in radians) between the periapsis and the current body position")][HideInInspector]public double trueAnomaly = 0;
    [Tooltip("Rotation of the orbit relative to it's plane")][HideInInspector]public double argumentOfPeriapsis = 0;
    [HideInInspector]public double ascendingNodeLongitude = 0;
    [Tooltip("Difference between the world plane and the orbital plane")][HideInInspector]public double inclination = 0;
    
    [Tooltip("Initial Velocity of the body")][HideInInspector]public DVector3 initialVelocity;
    
    [Tooltip("Whether or not orbit trails will be shown. These are currently not recommended for purely performance-based applications as they use Unity's built-in LineRenderer that can cause CPU overhead issues when scaled.")] public bool orbitTrails = true;
    [Tooltip("The length (in timesteps) of the orbit trail.")] public int orbitTrailLength = 30;
    [Tooltip("The material of this body's orbit. If not set, it will be overriden by the originator's orbit trail material.")]public Material orbitTrailMaterial;
    
    private void Awake()
    {
        currentAcceleration = DVector3.zero;
        currentPosition = transform.position;
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = currentPosition;
    }
    
}
