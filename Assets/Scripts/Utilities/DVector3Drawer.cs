using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DVector3))]
public class DVector3Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        float fieldWidth = position.width / 3f;
        float spacing = 2f;

        Rect xRect = new Rect(position.x, position.y, fieldWidth - spacing, position.height);
        Rect yRect = new Rect(position.x + fieldWidth, position.y, fieldWidth - spacing, position.height);
        Rect zRect = new Rect(position.x + (fieldWidth * 2), position.y, fieldWidth, position.height);
        
        SerializedProperty propX = property.FindPropertyRelative("x");
        SerializedProperty propY = property.FindPropertyRelative("y");
        SerializedProperty propZ = property.FindPropertyRelative("z");
        
        EditorGUIUtility.labelWidth = 14f;
        EditorGUI.PropertyField(xRect, propX, new GUIContent("X"));
        EditorGUI.PropertyField(yRect, propY, new GUIContent("Y"));
        EditorGUI.PropertyField(zRect, propZ, new GUIContent("Z"));
        EditorGUIUtility.labelWidth = 0f;
        
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}