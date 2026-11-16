using UnityEditor;
using UnityEngine;

public class ColorPaletteExplorer : EditorWindow
{
    private Color baseColor = Color.green;

    [MenuItem("Tools/Color Palette Explorer")]
    private static void ShowWindow()
    {
        GetWindow<ColorPaletteExplorer>("Color Palette");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Base Color", EditorStyles.boldLabel);

        baseColor = EditorGUILayout.ColorField(baseColor);

        GUILayout.Space(10);

        DrawVariant("Base", baseColor);

        GUILayout.Space(5);

        DrawVariant("Light", LerpWhite(baseColor, 0.25f));
        DrawVariant("Very Light", LerpWhite(baseColor, 0.5f));
        DrawVariant("Pastel", Desaturate(LerpWhite(baseColor, 0.4f), 0.5f));

        GUILayout.Space(10);

        DrawVariant("Dark", LerpBlack(baseColor, 0.25f));
        DrawVariant("Very Dark", LerpBlack(baseColor, 0.5f));

        GUILayout.Space(10);

        DrawVariant("Soft", Desaturate(baseColor, 0.3f));
        DrawVariant("Muted", Desaturate(baseColor, 0.6f));

        GUILayout.Space(10);

        DrawVariant("Vivid", Saturate(baseColor, 1.3f));
        DrawVariant("Ultra Vivid", Saturate(baseColor, 1.8f));
    }

    private void DrawVariant(string label, Color color)
    {
        Rect rect = GUILayoutUtility.GetRect(80, 40);

        EditorGUI.DrawRect(rect, color);

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = GetContrastColor(color);

        GUI.Label(
            rect,
            $"{label}\n#{ColorUtility.ToHtmlStringRGB(color)}",
            style);
    }

    private Color GetContrastColor(Color c)
    {
        float luminance =
            0.299f * c.r +
            0.587f * c.g +
            0.114f * c.b;

        return luminance > 0.5f
            ? Color.black
            : Color.white;
    }

    private Color LerpWhite(Color color, float amount)
    {
        return Color.Lerp(color, Color.white, amount);
    }

    private Color LerpBlack(Color color, float amount)
    {
        return Color.Lerp(color, Color.black, amount);
    }

    private Color Desaturate(Color color, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);

        s *= (1f - amount);

        return Color.HSVToRGB(h, s, v);
    }

    private Color Saturate(Color color, float multiplier)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);

        s = Mathf.Clamp01(s * multiplier);

        return Color.HSVToRGB(h, s, v);
    }
}