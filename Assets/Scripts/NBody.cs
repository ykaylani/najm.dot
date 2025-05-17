using UnityEngine;

// Every unity distance unit (m) is multiplied by 5 billion for viewable distances.

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
    private ObjectData objectData = new ObjectData();
    
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
        
        float simulationTimestep = Time.fixedDeltaTime * 30;
        
        AccumulateForce(transform.position);
        Integrate(simulationTimestep);
    }

    private void AccumulateForce(Vector3 objectPosition)
    {
        leadForce = Vector3.zero;
        for (int i = 0; i < originator.bodies.Count; i++)
        {
            NBody otherBody = originator.bodies[i];

            if (otherBody == this) continue;

            float distance = Vector3.Distance(objectPosition, otherBody.gameObject.transform.position) * distMultiplier;

            if (distance == 0) continue;

            float pullMagnitude = (originator.gravitationalConstant * mass * otherBody.mass) / (distance * distance + 1f);
            Vector3 pullDirection = (otherBody.transform.position - objectPosition).normalized;

            Vector3 total = pullDirection * pullMagnitude;
            leadForce += total;
        }
    }


    private ObjectData PredictPosition(Vector3 initialVelocity, Vector3 initialAcceleration, float timestep)
    {
        futurePosition += initialVelocity * timestep + 0.5f * initialAcceleration * (timestep * timestep);
        
        AccumulateForce(futurePosition);
        
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
