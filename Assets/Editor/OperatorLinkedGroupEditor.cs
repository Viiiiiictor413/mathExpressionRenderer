#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer (typeof(OperatorLinkedGroup))]
public class OperatorLinkedGroupEditor : PropertyDrawer {
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
		EditorGUIUtility.labelWidth = width / 5;
		int lineAmount = 0;

		Rect propertyRect = new Rect(position.x, position.y, width, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("type"));
		lineAmount++;
		
		bool isExplicitString = (property.FindPropertyRelative("type").enumValueIndex == 3);
		if (property.FindPropertyRelative("type").enumValueIndex == 0 || isExplicitString) {
			propertyRect = new Rect(position.x + (isExplicitString ? width * 0.25f + 4 : 0f), position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), (isExplicitString ? width * 0.75f - 4 : width), EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("stringVal"), new GUIContent((isExplicitString ? (property.FindPropertyRelative("isImage").boolValue ? "Image File Name" : "String") : "Closing String")));
			
			if (isExplicitString) {
				propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width * 0.25f, EditorGUIUtility.singleLineHeight);
				EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("isImage"));
			}
			lineAmount++;
		}
		
		SerializedProperty sizeFoldout = property.FindPropertyRelative("sizeFoldout");
		sizeFoldout.boolValue = EditorGUI.Foldout(new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight), sizeFoldout.boolValue, "Size", true);
		lineAmount++;
		if (sizeFoldout.boolValue) { 
			EditorGUI.indentLevel++;
		
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("sizePropToContent"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("sizeAbsolute"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("sizePropToBox"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("sizeMin"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("sizeMax"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("scaleTo"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("scaleMult"));
			lineAmount++;
			
			EditorGUI.indentLevel--;
		}
		
		SerializedProperty offsetFoldout = property.FindPropertyRelative("offsetFoldout");
		offsetFoldout.boolValue = EditorGUI.Foldout(new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight), offsetFoldout.boolValue, "Offset", true);
		lineAmount++;
		if (offsetFoldout.boolValue) {
			EditorGUI.indentLevel++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width * 0.5f, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("originX"));
			propertyRect = new Rect(position.x + width * 0.5f, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width * 0.5f, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("originY"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("offsetAbsolute"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("offsetIfText"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("offsetPropToSize"));
			lineAmount++;
			
			propertyRect = new Rect(position.x, position.y + lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative("offsetPropToBox"));
			lineAmount++;
			
			EditorGUI.indentLevel--;
		}
       
		// Set indent back to what it was
		EditorGUI.indentLevel = indent;
       
        EditorGUI.EndProperty();
    }
	
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		int lineAmount = 3;
        if (property.FindPropertyRelative("type").enumValueIndex == 0 || property.FindPropertyRelative("type").enumValueIndex == 3) lineAmount++;
		if (property.FindPropertyRelative("sizeFoldout").boolValue) lineAmount += 7;
		if (property.FindPropertyRelative("offsetFoldout").boolValue) lineAmount += 5;
		return (lineAmount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
    }
}
#endif