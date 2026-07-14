using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ObjectDataValidator : EditorWindow
{
    private Vector2 scroll;

    private readonly List<string> errors = new();
    private readonly List<string> warnings = new();

    [MenuItem("Tools/ObjectData Validator")]
    public static void Open()
    {
        GetWindow<ObjectDataValidator>("Object Validator");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        if (GUILayout.Button("Scan Project"))
        {
            Scan();
        }

        GUILayout.Space(10);

        GUILayout.Label($"Errors : {errors.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (string error in errors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        GUILayout.Space(10);

        GUILayout.Label($"Warnings : {warnings.Count}", EditorStyles.boldLabel);

        foreach (string warning in warnings)
        {
            EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        errors.Clear();
        warnings.Clear();

        string[] guids = AssetDatabase.FindAssets("t:ObjectData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            ObjectData data =
                AssetDatabase.LoadAssetAtPath<ObjectData>(path);

            Validate(data, path);
        }

        Debug.Log("ObjectData Validation Finished");
    }

    private void Validate(ObjectData data, string path)
    {
        if (string.IsNullOrWhiteSpace(data.objectName))
            errors.Add($"{path} : Object Name is empty");

        if (data.icon == null)
            warnings.Add($"{path} : Missing Icon");

        if (data.objectPrefab == null)
            errors.Add($"{path} : Missing Prefab");

        if (data.objectPoints < 0)
            errors.Add($"{path} : Negative Points");

        if (data.weight <= 0)
            warnings.Add($"{path} : Weight <= 0");

        if (data.objectType == ObjectType.ItemEffect &&
            data.effect == null)
        {
            errors.Add($"{path} : ItemEffect has no ObjectEffectData");
        }

        if (data.objectType != ObjectType.ItemEffect &&
            data.effect != null)
        {
            warnings.Add($"{path} : Non ItemEffect should not have ObjectEffectData");
        }
    }
}