using UnityEditor;
using UnityEngine;

[DefaultExecutionOrder(0)]
[RequireComponent(typeof(Propagator))]
public class BodyFrontend : MonoBehaviour
{
    public Propagator propagator;
    public InterfacedBodyInstance[] bodies;

    void OnEnable()
    {
        bodies = StructureConverter.AoSBodies(propagator.bodies.positions, propagator.bodies.masses, propagator.bodies.keplerianParams);
    }

    void Awake()
    {
        propagator.bodies = StructureConverter.SoABodies(bodies);
    }

}

[CustomEditor(typeof(BodyFrontend))]
public class BodyContainer : Editor
{
    public override void OnInspectorGUI()
    {
        SerializedProperty bodies = serializedObject.FindProperty("bodies");
        SerializedProperty propagator = serializedObject.FindProperty("propagator");
        EditorGUILayout.PropertyField(propagator);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(bodies, true);
        serializedObject.ApplyModifiedProperties();
    }
}