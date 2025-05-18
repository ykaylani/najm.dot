using UnityEngine;

public class NBody : MonoBehaviour
{
    [HideInInspector]public DVector3 currentAcceleration;
    [HideInInspector]public DVector3 currentVelocity;
    [HideInInspector]public DVector3 currentPosition;
    
    [HideInInspector]public DVector3 predictedPosition;
    
    public double mass = 5.972e24f; // Earth
    
    public DVector3 impulse;
    
    private void Start()
    {
        
        currentPosition = gameObject.transform.position;
        currentVelocity = impulse;
        currentAcceleration = DVector3.zero;
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = currentPosition;
    }
    
}
