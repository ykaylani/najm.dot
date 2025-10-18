using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

[CustomEditor(typeof(SceneGenerator))]
public class SceneGenerationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SerializedProperty genType = serializedObject.FindProperty("generationType");
        
        SerializedProperty size = serializedObject.FindProperty("size");
        SerializedProperty count = serializedObject.FindProperty("NCount");
        
        SerializedProperty mass = serializedObject.FindProperty("masses");
        SerializedProperty centralMass = serializedObject.FindProperty("centralBodyMass");
        SerializedProperty orbitMass = serializedObject.FindProperty("orbitingBodyMass");

        EditorGUILayout.PropertyField(genType);
        EditorGUI.indentLevel++;

        switch (genType.enumValueIndex)
        {
            case 0:
                EditorGUILayout.PropertyField(count);
                EditorGUILayout.PropertyField(size, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(mass);
                break;
            case 1:
                EditorGUILayout.PropertyField(count);
                EditorGUILayout.PropertyField(size, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(centralMass);
                EditorGUILayout.PropertyField(orbitMass, new GUIContent("Orbiting Body Mass(es)"));
                break;
            case 2:
                EditorGUILayout.PropertyField(count);
                EditorGUILayout.PropertyField(size, new GUIContent("Size"));
                EditorGUILayout.PropertyField(mass);
                break;
            case 3:
                EditorGUILayout.PropertyField(count);
                EditorGUILayout.PropertyField(size, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(mass);
                break;
            
        }

        serializedObject.ApplyModifiedProperties();
    }
}
