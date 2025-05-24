using UnityEditor;

[CustomEditor(typeof(NBody))]
[CanEditMultipleObjects]
public class NBodyEditor : Editor
{
    private SerializedProperty toggleKeplerianOrbits;
    private SerializedProperty initialVelocity;
    
    private SerializedProperty centralBody;
    private SerializedProperty eccentricity;
    private SerializedProperty semimajorAxis;
    private SerializedProperty trueAnomaly;
    private SerializedProperty argumentOfPeriapsis;
    private SerializedProperty ascendingNodeLongitude;
    private SerializedProperty inclination;
    
    private void OnEnable()
    {
        toggleKeplerianOrbits = serializedObject.FindProperty("keplerianOrbits");
        initialVelocity = serializedObject.FindProperty("initialVelocity");
        
        centralBody = serializedObject.FindProperty("centralBody");
        eccentricity = serializedObject.FindProperty("eccentricity");
        semimajorAxis = serializedObject.FindProperty("semimajorAxis");
        trueAnomaly = serializedObject.FindProperty("trueAnomaly");
        argumentOfPeriapsis = serializedObject.FindProperty("argumentOfPeriapsis");
        ascendingNodeLongitude = serializedObject.FindProperty("ascendingNodeLongitude");
        inclination = serializedObject.FindProperty("inclination");
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        if (!toggleKeplerianOrbits.boolValue)
        {
            EditorGUILayout.PropertyField(initialVelocity);
        }
        else
        {
            EditorGUILayout.PropertyField(centralBody);
            EditorGUILayout.PropertyField(eccentricity);
            EditorGUILayout.PropertyField(semimajorAxis);
            EditorGUILayout.PropertyField(trueAnomaly);
            EditorGUILayout.PropertyField(argumentOfPeriapsis);
            EditorGUILayout.PropertyField(ascendingNodeLongitude);
            EditorGUILayout.PropertyField(inclination);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
