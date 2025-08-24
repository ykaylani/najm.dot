using UnityEditor;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

[RequireComponent(typeof(Propagator))]
public class BodyFrontend : MonoBehaviour
{
    public double[] masses; 
    public double3[] positions;
    public double3[] velocities;
    public double4x2[] keplerianParams;
    public Propagator propagator;

    void OnEnable()
    {
        if (masses != null && masses.Length > 0)
        {
            propagator.bodies.masses = new NativeArray<double>(masses.Length, Allocator.Persistent);
            for (int i = 0; i < masses.Length; i++) propagator.bodies.masses[i] = masses[i];
        }

        if (positions != null && positions.Length > 0)
        {
            propagator.bodies.positions = new NativeArray<double3>(positions.Length, Allocator.Persistent);
            for (int i = 0; i < positions.Length; i++) propagator.bodies.positions[i] = positions[i];
        }
        
        if (velocities != null && velocities.Length > 0)
        {
            propagator.bodies.velocities = new NativeArray<double3>(velocities.Length, Allocator.Persistent);
            for (int i = 0; i < velocities.Length; i++) propagator.bodies.velocities[i] = velocities[i];
        }
        
        if (keplerianParams != null && keplerianParams.Length > 0)
        {
            propagator.bodies.keplerianParams = new NativeArray<double4x2>(keplerianParams.Length, Allocator.Persistent);
            for (int i = 0; i < keplerianParams.Length; i++) propagator.bodies.keplerianParams[i] = keplerianParams[i];
        }
    }

    void OnDisable()
    {
        if (propagator.bodies.masses.IsCreated)
        {
            masses = propagator.bodies.masses.ToArray();
            positions = propagator.bodies.positions.ToArray();
            velocities = propagator.bodies.velocities.ToArray();
        }
    }

}

[CustomEditor(typeof(BodyFrontend))]
public class BodyContainer : Editor
{
    public override void OnInspectorGUI()
    {
        SerializedProperty masses = serializedObject.FindProperty("masses");
        SerializedProperty positions = serializedObject.FindProperty("positions");
        SerializedProperty velocities = serializedObject.FindProperty("velocities");
        SerializedProperty keplerianParams = serializedObject.FindProperty("keplerianParams");
        SerializedProperty propagator = serializedObject.FindProperty("propagator");
        EditorGUILayout.PropertyField(propagator);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(masses, true);
        EditorGUILayout.PropertyField(positions, true);
        EditorGUILayout.PropertyField(velocities, true);
        EditorGUILayout.PropertyField(keplerianParams, true);
        serializedObject.ApplyModifiedProperties();
    }
}