using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GenerationType
{
    Spherical, 
    FLSphereRandomized,
    Cubic,
    Plummer, 
}

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(BodyFrontend)), RequireComponent(typeof(Propagator))]
public class SceneGenerator : MonoBehaviour
{
    InterfacedBodyInstance[] bodyInstances;
    BodyFrontend bodyFrontend;
    Propagator propagator;
    
    public GenerationType generationType;
    public int NCount;
    public float size;

    public double masses;
    public double centralBodyMass;
    public double orbitingBodyMass;

    private double totalMass;
    private double gravitationalConstant = 6.67e-11;
    
    void Awake()
    {
        if (!this.enabled) return;
        propagator = GetComponent<Propagator>();
        bodyInstances = new InterfacedBodyInstance[NCount];
        
        gravitationalConstant /= propagator.scale * propagator.scale * propagator.scale;
        gravitationalConstant *= propagator.speed * propagator.speed;
        totalMass = NCount * masses;

        switch (generationType)
        {
            case GenerationType.FLSphereRandomized:
                FSpherical(gravitationalConstant, size, centralBodyMass, orbitingBodyMass);
                break;
            case GenerationType.Cubic:
                Cubic(size);
                break;
            case GenerationType.Plummer:
                Plummer(size);
                break;
            case GenerationType.Spherical:
                Spherical(size);
                break;
        }
        
    }

    void Spherical(float radius)
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        for (int i = 0; i < NCount; i++)
        {
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

    void FSpherical(double G, double size, double cMass, double oMass)
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        bodyInstances[0].mass = cMass;
        bodyInstances[0].position = new double3(0, 0, 0);
        bodyInstances[0].primaryBody = -1;

        double phi = math.PI * (math.sqrt(5) - 1);
        
        for (int i = 1; i < NCount; i++)
        {
            bodyInstances[i].position.y = 1 - (i / (float)(NCount - 1)) * 2;
            double r = math.sqrt(1 - bodyInstances[i].position.y * bodyInstances[i].position.y);

            double theta = phi * i;

            bodyInstances[i].position.x = math.cos(theta) * r;
            bodyInstances[i].position.z = math.sin(theta) * r;

            bodyInstances[i].position.x += Random.Range(-1f, 1f);
            bodyInstances[i].position.y += Random.Range(-1f, 1f);
            bodyInstances[i].position.z += Random.Range(-1f, 1f);
            
            bodyInstances[i].mass = oMass;
            bodyInstances[i].primaryBody = -1;

            bodyInstances[i].position *= size;
            
            double3 relative = (bodyInstances[i].position - bodyInstances[0].position); 
            double3 up = new double3(Random.Range(-1f,1f), Random.Range(-1f,1f), Random.Range(-1f,1f));
            if (math.abs(math.dot(math.normalize(relative), math.normalize(up))) > 0.9999) up = new double3(1, 0, 0);
            
            double3 direction = math.normalize(math.cross(up, relative));
            double mu = G * (bodyInstances[0].mass + bodyInstances[i].mass);
            double magnitude = math.sqrt(mu / math.length(relative));
            double3 velocity = direction * magnitude;
            bodyInstances[i].velocity = velocity;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }

    void Cubic(float size)
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
        for (int i = 0; i < NCount - 1; i++)
        {
            bodyInstances[i].mass = masses;
            bodyInstances[i].position = new double3(Random.Range(-size, size),  Random.Range(-size, size), Random.Range(-size, size));
            bodyInstances[i].primaryBody = -1;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }
    
    void Plummer(double scaleRadius)
    {
        bodyInstances = new InterfacedBodyInstance[NCount];
    
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
    
            double r2 = distanceFromCenter * distanceFromCenter;
            double a2 = scaleRadius * scaleRadius;
            double velocityDispersion = math.sqrt((gravitationalConstant * totalMass) / (6 * math.sqrt(r2 + a2)));
    
            double speed = velocityDispersion * math.sqrt(-2.0 * math.log(Random.Range(1e-10f, 1f)));
    
            // Your existing angular code...
            double3 radialDirection = math.normalize(pos);
            double3 randomPerp = math.normalize(new double3(
                Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));
            randomPerp = math.normalize(randomPerp - math.dot(randomPerp, radialDirection) * radialDirection);
            double3 velocityDirection = math.cross(radialDirection, randomPerp);
    
            bodyInstances[i].velocity = speed * velocityDirection;
        }
        
        bodyFrontend = GetComponent<BodyFrontend>();
        bodyFrontend.bodies = bodyInstances;
    }
}
