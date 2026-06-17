using UnityEngine;

public static class StringUtils
{
    public static string ToColor(this string s, Color color)
    {
        string colorHTML = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{colorHTML}>{s}</color>";
    }
}
