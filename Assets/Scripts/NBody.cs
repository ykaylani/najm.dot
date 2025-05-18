using UnityEngine;

// Every unity distance unit (m) is multiplied by 2.5 billion for viewable distances.

public class NBody : MonoBehaviour
{
    public NBodyOriginator originator;
    
    private float distMultiplier;

    [HideInInspector]public Vector3 currentAcceleration;
    [HideInInspector]public Vector3 currentVelocity;
    [HideInInspector]public Vector3 currentPosition;
    
    [HideInInspector]public Vector3 predictedPosition;
    
    public float mass = 5.972e24f; // Earth
    
    private void Start()
    {
        originator = GameObject.FindGameObjectWithTag("Originator").GetComponent<NBodyOriginator>();
        distMultiplier = originator.distMultiplier;
        
        currentPosition = gameObject.transform.position;
        currentVelocity = Vector3.zero;
        currentAcceleration = Vector3.zero;
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = currentPosition;
    }
    
}
