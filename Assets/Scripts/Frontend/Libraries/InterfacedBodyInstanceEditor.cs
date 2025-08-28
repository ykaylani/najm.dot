using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public struct InterfacedBodyInstance
{
    public double3 position;
    public double mass;

    public bool keplerianOrbit;
    public int primaryBody;
    
    public double eccentricity;
    public double inclination;
    public double argumentOfPeriapsis;
    public double longitudeOfTheAscendingNode;
}

[CustomPropertyDrawer(typeof(InterfacedBodyInstance))]
public class InterfacedBodyInstanceEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
        
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }
        
        EditorGUI.indentLevel++;
        
        SerializedProperty positionProperty = property.FindPropertyRelative("position");
        SerializedProperty massProperty = property.FindPropertyRelative("mass");
        SerializedProperty keplerianOrbitProperty = property.FindPropertyRelative("keplerianOrbit");
        SerializedProperty primaryBodyProperty = property.FindPropertyRelative("primaryBody");
        SerializedProperty eccentricityProperty = property.FindPropertyRelative("eccentricity");
        SerializedProperty inclinationProperty = property.FindPropertyRelative("inclination");
        SerializedProperty periapsisProperty = property.FindPropertyRelative("argumentOfPeriapsis");
        SerializedProperty ascendingNodeProperty = property.FindPropertyRelative("longitudeOfTheAscendingNode");
        
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(position, positionProperty);
        
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(position, massProperty);
        
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(position, keplerianOrbitProperty);
        
        if (keplerianOrbitProperty.boolValue)
        {
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, primaryBodyProperty);
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, inclinationProperty);
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, eccentricityProperty);
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, periapsisProperty);
            
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, ascendingNodeProperty);
        }
        else
        {
            primaryBodyProperty.intValue = -1;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;
        height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
        
        SerializedProperty keplerianOrbitProp = property.FindPropertyRelative("keplerianOrbit");
        if (keplerianOrbitProp.boolValue) { height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 5; }
        
        return height;
    }
}