#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using MessagePack.Resolvers;
using MessagePack.Unity;

public class MsgPackDataEditor : EditorWindow
{
    private List<string> _filePaths = new List<string>();
    private string _selectedFilePath;
    private string _jsonText = "";
    private Vector2 _jsonScrollPosition;
    private string _statusMessage = "";

    // LocalDataManager와 동일한 옵션 사용 (저장 포맷 호환)
    private static MessagePackSerializerOptions _msgPackOptions = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            UnityResolver.Instance,
            ContractlessStandardResolver.Instance,
            StandardResolver.Instance
        ));

    [MenuItem("Tools/MsgPack Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<MsgPackDataEditor>("MsgPack Data Editor");
    }

    private void OnEnable()
    {
        RefreshFileList();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Refresh File List", GUILayout.Height(24)))
        {
            RefreshFileList();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Select File", EditorStyles.boldLabel);

        if (_filePaths.Count == 0)
        {
            EditorGUILayout.HelpBox("No .mpk files found in GameData folder.", MessageType.Info);
        }
        else
        {
            int currentIndex = string.IsNullOrEmpty(_selectedFilePath)
                ? 0
                : Mathf.Clamp(_filePaths.IndexOf(_selectedFilePath), 0, _filePaths.Count - 1);
            int newIndex = EditorGUILayout.Popup(currentIndex, _filePaths.Select(p => Path.GetFileName(p)).ToArray());
            if (newIndex != currentIndex || string.IsNullOrEmpty(_selectedFilePath))
            {
                _selectedFilePath = _filePaths[newIndex];
                LoadAndDisplaySelectedFile();
            }
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(_jsonText))
        {
            EditorGUILayout.LabelField("JSON Content", EditorStyles.boldLabel);
            _jsonScrollPosition = EditorGUILayout.BeginScrollView(_jsonScrollPosition, GUILayout.Height(400));
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 11 };
            string newJsonText = EditorGUILayout.TextArea(_jsonText, textAreaStyle, GUILayout.ExpandHeight(true));
            if (newJsonText != _jsonText)
            {
                _jsonText = newJsonText;
                if (!string.IsNullOrEmpty(_selectedFilePath))
                    _statusMessage = "JSON content modified. Click 'Save Changes to MessagePack' to apply.";
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField($"Characters: {_jsonText.Length} | Lines: {_jsonText.Split('\n').Length}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_selectedFilePath) || string.IsNullOrEmpty(_jsonText));
        if (GUILayout.Button("Save Changes to MessagePack", GUILayout.Height(32)))
        {
            SaveJsonToMessagePack();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void RefreshFileList()
    {
        string previouslySelected = _selectedFilePath;
        _filePaths.Clear();
        _statusMessage = "";

        try
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "GameData");
            if (Directory.Exists(dataPath))
            {
                _filePaths = Directory.GetFiles(dataPath, "*.mpk", SearchOption.TopDirectoryOnly).ToList();
                _statusMessage = $"Found {_filePaths.Count} .mpk file(s).";

                if (!string.IsNullOrEmpty(previouslySelected) && _filePaths.Contains(previouslySelected))
                {
                    _selectedFilePath = previouslySelected;
                    LoadAndDisplaySelectedFile();
                }
                else
                {
                    _selectedFilePath = _filePaths.Count > 0 ? _filePaths[0] : null;
                    _jsonText = "";
                    if (!string.IsNullOrEmpty(_selectedFilePath))
                        LoadAndDisplaySelectedFile();
                }
            }
            else
            {
                _statusMessage = $"Data directory not found: {dataPath}";
                _selectedFilePath = null;
                _jsonText = "";
            }
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
            _selectedFilePath = null;
            _jsonText = "";
            Debug.LogError(_statusMessage);
        }

        Repaint();
    }

    private void LoadAndDisplaySelectedFile()
    {
        _jsonText = "";
        _statusMessage = "";
        if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            _statusMessage = "File not found or no file selected.";
            return;
        }

        try
        {
            byte[] fileBytes = File.ReadAllBytes(_selectedFilePath);
            _statusMessage = $"Read {fileBytes.Length} bytes from {Path.GetFileName(_selectedFilePath)}.";

            string json = MessagePackSerializer.ConvertToJson(fileBytes, _msgPackOptions);
            object parsed = JsonConvert.DeserializeObject(json);
            _jsonText = JsonConvert.SerializeObject(parsed, Formatting.Indented);
            _statusMessage += " Converted to JSON.";
        }
        catch (Exception ex)
        {
            _jsonText = $"Error loading: {ex.Message}";
            _statusMessage = $"Error: {ex.Message}";
            Debug.LogError(ex);
        }

        Repaint();
    }

    private void SaveJsonToMessagePack()
    {
        if (string.IsNullOrEmpty(_selectedFilePath) || string.IsNullOrEmpty(_jsonText))
        {
            _statusMessage = "No file selected or JSON data is empty.";
            return;
        }

        try
        {
            byte[] messagePackBytes = ConvertJsonToMessagePack(_jsonText, _selectedFilePath);
            File.WriteAllBytes(_selectedFilePath, messagePackBytes);
            _statusMessage = $"Saved to {Path.GetFileName(_selectedFilePath)} ({messagePackBytes.Length} bytes).";
            LoadAndDisplaySelectedFile();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error saving: {ex}";
            Debug.LogError(ex);
        }

        Repaint();
    }

    private byte[] ConvertJsonToMessagePack(string jsonText, string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string typeName = Path.GetFileNameWithoutExtension(fileName);

        Type type = FindTableType(typeName);
        if (type != null)
        {
            object obj = JsonConvert.DeserializeObject(jsonText, type, _jsonSettings);
            if (obj == null)
                throw new Exception($"JSON deserialization resulted in null for type {type.Name}.");
            return SerializeWithType(type, obj);
        }

        return MessagePackSerializer.ConvertFromJson(jsonText, _msgPackOptions);
    }

    private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { new StringEnumConverter() },
        NullValueHandling = NullValueHandling.Ignore
    };

    private static Type FindTableType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            try
            {
                Type type = asm.GetTypes().FirstOrDefault(t =>
                    t.Name == typeName && t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);
                if (type != null) return type;
            }
            catch (ReflectionTypeLoadException) { }
        }
        return null;
    }

    private static byte[] SerializeWithType(Type type, object obj)
    {
        // dynamic으로 런타임 타입에 맞는 Serialize<T> 호출 (리플렉션 불필요)
        return MessagePackSerializer.Serialize((dynamic)obj, _msgPackOptions);
    }
}
#endif
