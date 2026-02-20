using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ImageController))]
public class ImageControllerEditor : Editor
{
    private SerializedProperty _isCustom;
    private SerializedProperty _customSprites;
    private SerializedProperty _currentCustomKey;

    private void OnEnable()
    {
        _isCustom = serializedObject.FindProperty("isCustom");
        _customSprites = serializedObject.FindProperty("customSprites");
        _currentCustomKey = serializedObject.FindProperty("currentCustomKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var exclude = _isCustom.boolValue ? new[] { "m_Script", "currentCustomKey" } : new[] { "m_Script" };
        DrawPropertiesExcluding(serializedObject, exclude);

        if (_isCustom.boolValue)
        {
            if (_customSprites != null && _customSprites.isArray)
            {
                var keys = GetCustomKeys();
                if (keys.Length > 0)
                {
                    EditorGUILayout.Space(2);
                    int currentIndex = System.Array.IndexOf(keys, _currentCustomKey.stringValue);
                    if (currentIndex < 0) currentIndex = 0;
                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUILayout.Popup("Current Custom Key", currentIndex, keys);
                    if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < keys.Length)
                        _currentCustomKey.stringValue = keys[newIndex];
                }
                else
                    EditorGUILayout.PropertyField(_currentCustomKey, new GUIContent("Current Custom Key"));
            }
            else
                EditorGUILayout.PropertyField(_currentCustomKey, new GUIContent("Current Custom Key"));
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string[] GetCustomKeys()
    {
        int count = _customSprites.arraySize;
        var list = new System.Collections.Generic.List<string>();
        for (int i = 0; i < count; i++)
        {
            var entry = _customSprites.GetArrayElementAtIndex(i);
            var keyProp = entry.FindPropertyRelative("key");
            if (keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue))
                list.Add(keyProp.stringValue);
        }
        return list.ToArray();
    }
}
