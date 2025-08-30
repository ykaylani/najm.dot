using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Propagator))]
public class Visualizer : MonoBehaviour
{
    public Propagator propagator;
    public Mesh pointMesh;
    public Material material;
    public float pointScale = 5f;

    private NativeArray<Vector3> renderPositions;
    private Matrix4x4[] matrices;
    private bool needsUpdate;

    void Start()
    {
        renderPositions = new NativeArray<Vector3>(propagator.bodies.positions.Length, Allocator.Persistent);
        matrices = new Matrix4x4[propagator.bodies.positions.Length];

        for (int i = 0; i < propagator.bodies.positions.Length; i++) {renderPositions[i] = (float3)propagator.bodies.positions[i];}
    }

    void FixedUpdate()
    {
        CopyPositionsJob job = new CopyPositionsJob { source = propagator.bodies.positions, destination = renderPositions };
        job.Schedule(propagator.bodies.positions.Length, 64).Complete();
    }

    void LateUpdate()
    {
        
        for (int i = 0; i < propagator.bodies.positions.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(renderPositions[i], Quaternion.identity, Vector3.one * pointScale);
        }
        
        Graphics.DrawMeshInstanced(pointMesh, 0, material, matrices, propagator.bodies.positions.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
    }

    void OnDestroy()
    {
        renderPositions.Dispose();
    }
    
}

struct CopyPositionsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<double3> source;
    [WriteOnly] public NativeArray<Vector3> destination;

    public void Execute(int index)
    {
        destination[index] = (float3)source[index];
    }
}