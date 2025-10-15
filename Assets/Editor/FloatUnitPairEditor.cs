#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer (typeof(FloatUnitPair))]
public class FloatUnitPairEditor : PropertyDrawer {
	// Draw the property inside the given rect
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		
		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		
		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
			
		// Calculate rects & draw fields
		float width = position.width;
		float height = position.height;
		EditorGUIUtility.labelWidth = width / 17f;

		bool effective = (property.FindPropertyRelative("val").floatValue != 0f);
       
		Rect propertyRect = new Rect(position.x, position.y, (effective ? width * 0.6f - 3 : width), EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("val"), GUIContent.none);
	
		if (effective) {
			propertyRect = new Rect(position.x + width * 0.6f, position.y, width * 0.4f, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("unit"), GUIContent.none);
		}
		
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

[CustomPropertyDrawer (typeof(Vector2FloatUnitPair))]
public class Vector2FloatUnitPairEditor : PropertyDrawer {
	// Draw the property inside the given rect
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		
		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		
		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
			
		// Calculate rects & draw fields
		float width = position.width * 0.8f;
		float height = position.height;
		EditorGUIUtility.labelWidth = 13f;
		
		bool effective = (property.FindPropertyRelative("x").FindPropertyRelative("val").floatValue != 0f);
		Color originalColor = GUI.color;
        if (!effective) GUI.color = new Color(1f, 1f, 1f, 0.5f);
		Rect propertyRect = new Rect(position.x, position.y, width * 0.5f, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("x"), new GUIContent("X"));
		GUI.color = originalColor;
		
		effective = (property.FindPropertyRelative("y").FindPropertyRelative("val").floatValue != 0f);
        if (!effective) GUI.color = new Color(1f, 1f, 1f, 0.5f);
		propertyRect = new Rect(position.x + width * 0.5f + 4, position.y, width * 0.5f, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("y"), new GUIContent("Y"));
		GUI.color = originalColor;
		
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

[CustomPropertyDrawer (typeof(Vector2))]
public class Vector2Editor : PropertyDrawer {
	// Draw the property inside the given rect
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		
		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		
		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
			
		// Calculate rects & draw fields
		float width = position.width * 0.8f;
		float height = position.height;
		EditorGUIUtility.labelWidth = 13f;
		
		bool effective = (property.FindPropertyRelative("x").floatValue != 0f);
		Color originalColor = GUI.color;
        if (!effective) GUI.color = new Color(1f, 1f, 1f, 0.5f);
		Rect propertyRect = new Rect(position.x, position.y, width * 0.5f, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("x"), new GUIContent("X"));
		GUI.color = originalColor;
		
		effective = (property.FindPropertyRelative("y").floatValue != 0f);
        if (!effective) GUI.color = new Color(1f, 1f, 1f, 0.5f);
		propertyRect = new Rect(position.x + width * 0.5f + 4, position.y, width * 0.5f, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("y"), new GUIContent("Y"));
		GUI.color = originalColor;
		
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