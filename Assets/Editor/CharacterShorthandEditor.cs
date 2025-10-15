#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer (typeof(CharacterShorthand))]
public class CharacterShorthandEditor : PropertyDrawer {
	// Draw the property inside the given rect
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		
		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), GUIContent.none);
		
		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
			
		// Calculate rects & draw fields
		float width = position.width;
		float height = position.height;
		EditorGUIUtility.labelWidth = width / 15;

		Rect propertyRect = new Rect(position.x - 10, position.y, width * 0.5f - 10, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("key"));
		
		propertyRect = new Rect(position.x - 10 + width * 0.5f, position.y, width * 0.5f - 10, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("val"));
		
		// Set indent back to what it was
		EditorGUI.indentLevel = indent;
       
	    // GetPropertyHeight(property, label);
        EditorGUI.EndProperty();
    }
	
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		int lineAmount = 1;
		return (lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
    }
}
#endif