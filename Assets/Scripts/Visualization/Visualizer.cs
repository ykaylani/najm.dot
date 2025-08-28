using UnityEngine;

[RequireComponent(typeof(Propagator))]
public class Visualizer : MonoBehaviour
{
    public GameObject bodyPrefab;
    private Propagator propagator;
    private GameObject[] bodies;

    void Start()
    {
        propagator = GetComponent<Propagator>();
        InitializeBodies();
    }

    void FixedUpdate()
    {
        UpdateBodyPositions();
    }

    void InitializeBodies()
    {
        if (propagator.bodies.positions.IsCreated)
        {
            int numBodies = propagator.bodies.positions.Length;
            bodies = new GameObject[numBodies];
            
            for (int i = 0; i < numBodies; i++)
            {
                bodies[i] = Instantiate(bodyPrefab, transform);
                bodies[i].transform.localScale = Vector3.one * 3;
                bodies[i].SetActive(true);
                
                // Set initial position
                Vector3 position = new Vector3(
                    (float)propagator.bodies.positions[i].x,
                    (float)propagator.bodies.positions[i].y,
                    (float)propagator.bodies.positions[i].z
                );
                bodies[i].transform.localPosition = position;
            }
        }
    }

    void UpdateBodyPositions()
    {
        if (bodies == null || !propagator.bodies.positions.IsCreated) return;

        for (int i = 0; i < bodies.Length; i++)
        {
            Vector3 position = new Vector3((float)propagator.bodies.positions[i].x, (float)propagator.bodies.positions[i].y, (float)propagator.bodies.positions[i].z);
            bodies[i].transform.localPosition = position;
            
        }
    }
}