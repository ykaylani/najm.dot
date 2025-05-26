using UnityEditor;

[CustomEditor(typeof(NBody))]
[CanEditMultipleObjects]
public class NBodyEditor : Editor
{
    private SerializedProperty mass;
    
    private SerializedProperty toggleKeplerianOrbits;
    private SerializedProperty initialVelocity;
    
    private SerializedProperty centralBody;
    private SerializedProperty eccentricity;
    private SerializedProperty semimajorAxis;
    private SerializedProperty trueAnomaly;
    private SerializedProperty argumentOfPeriapsis;
    private SerializedProperty ascendingNodeLongitude;
    private SerializedProperty inclination;

    private SerializedProperty orbitTrails;
    private SerializedProperty orbitTrailLength;

    private bool generalSettings;
    private bool initialVelocitySettings;
    private bool visualizationSettings;
    
    private void OnEnable()
    {
        mass = serializedObject.FindProperty("mass");
        
        toggleKeplerianOrbits = serializedObject.FindProperty("keplerianOrbits");
        initialVelocity = serializedObject.FindProperty("initialVelocity");
        
        centralBody = serializedObject.FindProperty("centralBody");
        eccentricity = serializedObject.FindProperty("eccentricity");
        semimajorAxis = serializedObject.FindProperty("semimajorAxis");
        trueAnomaly = serializedObject.FindProperty("trueAnomaly");
        argumentOfPeriapsis = serializedObject.FindProperty("argumentOfPeriapsis");
        ascendingNodeLongitude = serializedObject.FindProperty("ascendingNodeLongitude");
        inclination = serializedObject.FindProperty("inclination");
        
        orbitTrails = serializedObject.FindProperty("orbitTrails");
        orbitTrailLength = serializedObject.FindProperty("orbitTrailLength");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        generalSettings = EditorGUILayout.BeginFoldoutHeaderGroup(generalSettings, "General");
        
        if (generalSettings)
        {
            EditorGUILayout.PropertyField(mass);
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        initialVelocitySettings = EditorGUILayout.BeginFoldoutHeaderGroup(initialVelocitySettings, "Initial Velocity Settings");
        
        if (initialVelocitySettings)
        {
            if (!toggleKeplerianOrbits.boolValue)
            {
                EditorGUILayout.PropertyField(toggleKeplerianOrbits);
                EditorGUILayout.PropertyField(initialVelocity);
            }
            else
            {
                EditorGUILayout.PropertyField(toggleKeplerianOrbits);
                EditorGUILayout.PropertyField(centralBody);
                EditorGUILayout.PropertyField(eccentricity);
                EditorGUILayout.PropertyField(semimajorAxis);
                EditorGUILayout.PropertyField(trueAnomaly);
                EditorGUILayout.PropertyField(argumentOfPeriapsis);
                EditorGUILayout.PropertyField(ascendingNodeLongitude);
                EditorGUILayout.PropertyField(inclination);
            }
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        visualizationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(visualizationSettings, "Visualization");

        if (visualizationSettings)
        {
            EditorGUILayout.PropertyField(orbitTrails);

            if (orbitTrails.boolValue)
            {
                EditorGUILayout.PropertyField(orbitTrailLength);
            }
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        serializedObject.ApplyModifiedProperties();
    }
}
