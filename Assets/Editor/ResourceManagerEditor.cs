using UnityEditor;
using UnityEngine;

public class ResourceManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ResourceManager generator = (ResourceManager)target;

        GUILayout.Space(50);
        if (GUILayout.Button("Generate Map", GUILayout.Height(30)))
        {
            generator.GenerateMap();
        }

        if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
        {
            generator.ClearMap();
        }
    }
}
