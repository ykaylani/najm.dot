using UnityEngine;

// Every unity distance unit (m) is multiplied by 2.5 billion for viewable distances.

struct ObjectData
{
    public Vector3 acceleration;
    public Vector3 velocity;
}

public class NBody : MonoBehaviour
{
    public NBodyOriginator originator;
    
    private float distMultiplier;
    private Vector3 leadForce;
    
    private Vector3 futurePosition;
    private ObjectData objectData;
    
    public float mass = 5.972e24f; // Earth
    
    private void Start()
    {
        originator = GameObject.FindGameObjectWithTag("Originator").GetComponent<NBodyOriginator>();
        distMultiplier = originator.distMultiplier;

        futurePosition = transform.position;
        
    }

    private void FixedUpdate()
    {
        gameObject.transform.position = futurePosition;
        
        float simulationTimestep = Time.fixedDeltaTime * 30; // simulation speed
        
        leadForce = Vector3.zero;
        
        Integrate(simulationTimestep);
    }
    //Issues with updating forces
    // both PredictPosition and Integrate are for moving the body via Velocity Verlet integration
    private ObjectData PredictPosition(Vector3 initialVelocity, Vector3 initialAcceleration, float timestep)
    {
        futurePosition += initialVelocity * timestep + 0.5f * initialAcceleration * (timestep * timestep);
        
        ObjectData returnData = new ObjectData();

        returnData.acceleration = leadForce / mass;
        returnData.velocity = initialVelocity + 0.5f * (initialAcceleration + returnData.acceleration) * timestep;
        
        return returnData;
    }

    private void Integrate(float timestep)
    {
        ObjectData newData = PredictPosition(objectData.acceleration, objectData.velocity, timestep);
        objectData = newData;
        
    }
}
