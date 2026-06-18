using UnityEngine;

public static class NumberExtensions
{
   public static bool IsBetween(this float value, float min, float max)
   {
       return value >= min && value <= max;
    }
    public static bool IsBetween(this int value, int min, int max)
    {
        return value >= min && value <= max;
    }
    public static int ToInt(this float value)
    {
        return Mathf.RoundToInt(value);
    }
}
