using UnityEngine;
using UnityEngine.Serialization;

public class NBody : MonoBehaviour
{
    [HideInInspector]public DVector3 currentAcceleration;
    [HideInInspector]public DVector3 currentVelocity;
    [HideInInspector]public DVector3 currentPosition;
    
    [HideInInspector]public DVector3 predictedPosition;

    [Tooltip("Body's Mass in kilograms.")] public double mass = 5.972e24; // Earth mass default
    [Tooltip("Initial Velocity of Body")] public DVector3 initialVelocity;
    
    private void Start()
    {
        currentPosition = gameObject.transform.position;
        currentVelocity = initialVelocity;
        currentAcceleration = DVector3.zero;
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = currentPosition;
    }
    
}
