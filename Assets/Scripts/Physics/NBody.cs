using UnityEngine;
using System.Collections.Generic;

public class NBody : MonoBehaviour
{
    [HideInInspector]public DVector3 currentAcceleration;
    [HideInInspector]public DVector3 currentVelocity;
    [HideInInspector]public DVector3 currentPosition;
    
    [HideInInspector]public DVector3 predictedPosition;
    
    [HideInInspector]public LineRenderer lineRenderer;
    [HideInInspector]public List<Vector3> orbitPoints;
    
    [Tooltip("Body's Mass in kilograms.")] public double mass = 5.972e24; // Earth mass default
    
    [Tooltip("Whether or not the initial velocity of the object will be calculated using the keplerian properties")]public bool keplerianOrbits = true;
    [Tooltip("The body that this body will orbit around")][HideInInspector]public NBody centralBody;
    [Tooltip("Determines how elliptical the orbit is (0 is circular, 1 is very elongated)")][HideInInspector]public double eccentricity = 0.1;
    [Tooltip("The orbit's longest radius in real-world units (simulation units * distMultiplier)")][HideInInspector]public double semimajorAxis = 1.5e11;
    [Tooltip("The angle (in radians) between the periapsis and the current body position")][HideInInspector]public double trueAnomaly = 0;
    [Tooltip("Rotation of the orbit relative to it's plane")][HideInInspector]public double argumentOfPeriapsis = 0;
    [HideInInspector]public double ascendingNodeLongitude = 0;
    [Tooltip("Difference between the world plane and the orbital plane")][HideInInspector]public double inclination = 0;
    
    [Tooltip("Initial Velocity of the body")][HideInInspector]public DVector3 initialVelocity;
    
    [Tooltip("Whether or not orbit trails will be shown.")] public bool orbitTrails = true;
    [Tooltip("The length (in timesteps) of the orbit trail.")] public int orbitTrailLength = 30;
    
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
