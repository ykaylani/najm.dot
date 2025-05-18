using UnityEngine;

// Every unity distance unit (m) is multiplied by 2.5 billion for viewable distances.

public class NBody : MonoBehaviour
{
    public NBodyOriginator originator;
    
    private double distMultiplier;

    [HideInInspector]public DVector3 currentAcceleration;
    [HideInInspector]public DVector3 currentVelocity;
    [HideInInspector]public DVector3 currentPosition;
    
    [HideInInspector]public DVector3 predictedPosition;
    
    public double mass = 5.972e24f; // Earth
    
    public DVector3 impulse;
    
    private void Start()
    {
        
        originator = GameObject.FindGameObjectWithTag("Originator").GetComponent<NBodyOriginator>();
        
        currentPosition = gameObject.transform.position;
        currentVelocity = impulse;
        currentAcceleration = DVector3.zero;
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = currentPosition;
    }
    
}
