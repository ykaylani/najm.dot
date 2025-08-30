using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GenerationType
{
    Spherical, 
    Cubic,
    Plummer
}

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(BodyFrontend)), RequireComponent(typeof(Propagator))]
public class SceneGenerator : MonoBehaviour
{
    InterfacedBodyInstance[] bodyInstances;
    BodyFrontend bodyFrontend;
    Propagator propagator;
    public GenerationType generationType;
    public double masses;
    public int NCount;

    private double totalMass;
    private double gravitationalConstant = 6.67e-11;
    
    void Awake()
    {
        propagator = GetComponent<Propagator>();
        bodyInstances = new InterfacedBodyInstance[NCount];
        gravitationalConstant /= propagator.simulationSettings.x * propagator.simulationSettings.x * propagator.simulationSettings.x;
        gravitationalConstant *= propagator.simulationTimestepMultiplier * propagator.simulationTimestepMultiplier;
        totalMass = NCount * masses;

        if (generationType == GenerationType.Cubic)
        {
            Cubic();
        }
        else if (generationType == GenerationType.Plummer)
        {
            Plummer();
        }
        else if (generationType == GenerationType.Spherical)
        {
            Spherical();
        }
    }

    void Spherical()
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        for (int i = 0; i < NCount; i++)
        {
            float radius = 500f;
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            float randomMagnitude = Mathf.Pow(Random.value, 1f/3f) * radius;
            Vector3 randomPosition = randomDirection * randomMagnitude;

            bodyInstances[i].mass = masses;
            bodyInstances[i].position = new double3(randomPosition.x, randomPosition.y, randomPosition.z);
            bodyInstances[i].primaryBody = -1;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }

    void Cubic()
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        for (int i = 0; i < NCount; i++)
        {
            bodyInstances[i].mass = masses;
            bodyInstances[i].position = new double3(Random.Range(-500, 500),  Random.Range(-500, 500), Random.Range(-500, 500));
            bodyInstances[i].primaryBody = -1;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }
    
    void Plummer()
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        double scaleRadius = 200.0;
    
        for (int i = 0; i < NCount; i++)
        {
            double X = Random.value;
            double radius = scaleRadius / Math.Sqrt(Math.Pow(X, -2.0/3.0) - 1.0);
            
            double theta = Math.Acos(2.0 * Random.value - 1.0);
            double phi = 2.0 * Math.PI * Random.value;
    
            double x = radius * Math.Sin(theta) * Math.Cos(phi);
            double y = radius * Math.Sin(theta) * Math.Sin(phi);
            double z = radius * Math.Cos(theta);
    
            bodyInstances[i].position = new double3(x, y, z);
            bodyInstances[i].mass = masses;
            bodyInstances[i].primaryBody = -1;
        }

        for (int i = 0; i < NCount; i++)
        {
            double3 pos = bodyInstances[i].position;
            double distanceFromCenter = math.length(pos);
            
            double massInside = totalMass * Math.Pow(distanceFromCenter, 3) / Math.Pow(Math.Pow(distanceFromCenter, 2) + Math.Pow(scaleRadius, 2), 3.0/2.0);
            
            double orbitalSpeed = Math.Sqrt(gravitationalConstant * massInside / distanceFromCenter);
            
            double3 radialDirection = math.normalize(pos);
            double3 randomPerp = math.normalize(new double3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));
            
            randomPerp = math.normalize(randomPerp - math.dot(randomPerp, radialDirection) * radialDirection);
            
            double3 velocityDirection = math.cross(radialDirection, randomPerp);
            bodyInstances[i].velocity = orbitalSpeed * velocityDirection;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }
}
