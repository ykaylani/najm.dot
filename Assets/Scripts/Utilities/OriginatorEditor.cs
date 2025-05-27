using UnityEditor;

[CustomEditor(typeof(NBodyOriginator))]
[CanEditMultipleObjects]
public class OriginatorEditor : Editor
{

    private SerializedProperty distMultiplier;
    private SerializedProperty simulationTimestep;

    private SerializedProperty openingAngleCriterion;
    
    private SerializedProperty adaptiveSimulationBounds;
    private SerializedProperty boundsPadding;
    private SerializedProperty simulationBounds;

    private SerializedProperty orbitTrailMaterial;
    private SerializedProperty orbitWidth;
    private SerializedProperty visualizationTimestep;

    private bool generalSettings;
    private bool barnesHutSettings;
    private bool boundsSettings;
    private bool visualizationSettings;

    private void OnEnable()
    {
        distMultiplier = serializedObject.FindProperty("distMultiplier");
        simulationTimestep = serializedObject.FindProperty("simulationTimestep");
        
        openingAngleCriterion = serializedObject.FindProperty("openingAngleCriterion");
        
        adaptiveSimulationBounds = serializedObject.FindProperty("adaptiveSimulationBounds");
        boundsPadding = serializedObject.FindProperty("boundsPadding");
        simulationBounds = serializedObject.FindProperty("simulationBounds");
        
        orbitTrailMaterial = serializedObject.FindProperty("orbitTrailMaterial");
        orbitWidth = serializedObject.FindProperty("orbitWidth");
        visualizationTimestep = serializedObject.FindProperty("visualizationTimestep");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        generalSettings = EditorGUILayout.BeginFoldoutHeaderGroup(generalSettings, "General");
        if (generalSettings)
        {
            EditorGUILayout.PropertyField(distMultiplier);
            EditorGUILayout.PropertyField(simulationTimestep);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        barnesHutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(barnesHutSettings, "Barnes-Hut");
        if (barnesHutSettings)
        {
            EditorGUILayout.PropertyField(openingAngleCriterion);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        boundsSettings = EditorGUILayout.BeginFoldoutHeaderGroup(boundsSettings, "Bounds");
        if (boundsSettings)
        {
            EditorGUILayout.PropertyField(adaptiveSimulationBounds);
            
            if (!adaptiveSimulationBounds.boolValue)
            {
                EditorGUILayout.PropertyField(simulationBounds);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        visualizationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(visualizationSettings, "Visualization");
        if (visualizationSettings)
        {
            EditorGUILayout.PropertyField(orbitTrailMaterial);
            EditorGUILayout.PropertyField(orbitWidth);
            EditorGUILayout.PropertyField(visualizationTimestep);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
