using UnityEditor;

[CustomEditor(typeof(NBodyOriginator))]
[CanEditMultipleObjects]
public class OriginatorEditor : Editor
{
    private SerializedProperty distMultiplier;
    private SerializedProperty simTimestep;
    
    private SerializedProperty toggleAdaptiveSimulationBounds;
    private SerializedProperty simulationBoundsValue;

    private void OnEnable()
    {
        distMultiplier = serializedObject.FindProperty("distMultiplier");
        simTimestep = serializedObject.FindProperty("simulationTimestep");
        
        toggleAdaptiveSimulationBounds = serializedObject.FindProperty("adaptiveSimulationBounds");
        simulationBoundsValue = serializedObject.FindProperty("simulationBounds");
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        if (!toggleAdaptiveSimulationBounds.boolValue)
        {
            EditorGUILayout.PropertyField(simulationBoundsValue);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
