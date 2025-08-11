using UnityEditor;

[CustomEditor(typeof(Body))]
[CanEditMultipleObjects]
public class NBodyEditor : Editor
{
    private SerializedProperty mass;
    
    private SerializedProperty toggleKeplerianOrbits;
    private SerializedProperty initialVelocity;
    
    private SerializedProperty centralBody;
    private SerializedProperty eccentricity;
    private SerializedProperty semimajorAxis;
    private SerializedProperty calculateSemimajorAxis;
    private SerializedProperty trueAnomaly;
    private SerializedProperty argumentOfPeriapsis;
    private SerializedProperty ascendingNodeLongitude;
    private SerializedProperty inclination;

    private bool generalSettings;
    private bool initialVelocitySettings;
    
    private void OnEnable()
    {
        mass = serializedObject.FindProperty("mass");
        
        toggleKeplerianOrbits = serializedObject.FindProperty("keplerianOrbits");
        initialVelocity = serializedObject.FindProperty("initialVelocity");
        
        centralBody = serializedObject.FindProperty("centralBody");
        eccentricity = serializedObject.FindProperty("eccentricity");
        semimajorAxis = serializedObject.FindProperty("semimajorAxis");
        calculateSemimajorAxis = serializedObject.FindProperty("calculateSemimajorAxis");
        trueAnomaly = serializedObject.FindProperty("trueAnomaly");
        argumentOfPeriapsis = serializedObject.FindProperty("argumentOfPeriapsis");
        ascendingNodeLongitude = serializedObject.FindProperty("ascendingNodeLongitude");
        inclination = serializedObject.FindProperty("inclination");
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
                EditorGUILayout.PropertyField(calculateSemimajorAxis);

                if (!calculateSemimajorAxis.boolValue)
                {
                    EditorGUILayout.PropertyField(semimajorAxis);
                }
                
                EditorGUILayout.PropertyField(trueAnomaly);
                EditorGUILayout.PropertyField(argumentOfPeriapsis);
                EditorGUILayout.PropertyField(ascendingNodeLongitude);
                EditorGUILayout.PropertyField(inclination);
            }
        }
        
        EditorGUILayout.EndFoldoutHeaderGroup();
        serializedObject.ApplyModifiedProperties();
    }
}
