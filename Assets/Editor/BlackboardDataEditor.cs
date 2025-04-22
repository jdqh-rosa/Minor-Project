using System;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomEditor(typeof(BlackboardData))]
public class BlackboardDataEditor : Editor
{
    ReorderableList entryList;


    private void OnEnable() {
        entryList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(BlackboardData.Entries)), true, true, true,
            true)
        {
            drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.3f, EditorGUIUtility.singleLineHeight),
                    "Key");
                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.3f + 10, rect.y, rect.width * 0.3f,
                        EditorGUIUtility.singleLineHeight), "Type");
                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width * 0.6f + 5, rect.y, rect.width * 0.4f,
                        EditorGUIUtility.singleLineHeight), "Value");
            }
        };

        entryList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var _element = entryList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;
            var _keyName = _element.FindPropertyRelative(nameof(BlackboardEntryData.KeyName));
            var _valueType = _element.FindPropertyRelative(nameof(BlackboardEntryData.ValueType));
            var value = _element.FindPropertyRelative(nameof(BlackboardEntryData.Value));

            var _keyNameRect = new Rect(rect.x, rect.y, rect.width * 0.3f, EditorGUIUtility.singleLineHeight);
            var _valueTypeRect = new Rect(rect.x + rect.width * 0.3f, rect.y, rect.width * 0.3f,
                EditorGUIUtility.singleLineHeight);
            var _valueRect = new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(_keyNameRect, _keyName, GUIContent.none);
            EditorGUI.PropertyField(_valueTypeRect, _valueType, GUIContent.none);

            switch ((AnyValue.ValueType)_valueType.enumValueIndex) {
                case AnyValue.ValueType.Int:
                    var _intValue = value.FindPropertyRelative(nameof(AnyValue.IntValue));
                    EditorGUI.PropertyField(_valueRect, _intValue, GUIContent.none);
                    break;
                case AnyValue.ValueType.Float:
                    var _floatValue = value.FindPropertyRelative(nameof(AnyValue.FloatValue));
                    EditorGUI.PropertyField(_valueRect, _floatValue, GUIContent.none);
                    break;
                case AnyValue.ValueType.Bool:
                    var _boolValue = value.FindPropertyRelative(nameof(AnyValue.BoolValue));
                    EditorGUI.PropertyField(_valueRect, _boolValue, GUIContent.none);
                    break;
                case AnyValue.ValueType.String:
                    var _stringValue = value.FindPropertyRelative(nameof(AnyValue.StringValue));
                    EditorGUI.PropertyField(_valueRect, _stringValue, GUIContent.none);
                    break;
                case AnyValue.ValueType.Vector3:
                    var _vector3Value = value.FindPropertyRelative(nameof(AnyValue.Vector3Value));
                    EditorGUI.PropertyField(_valueRect, _vector3Value, GUIContent.none);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        entryList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}


