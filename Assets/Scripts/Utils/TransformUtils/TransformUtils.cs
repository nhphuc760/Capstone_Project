using UnityEngine;

public static class TransformUtils
{   
    public static void ResetLocal(this Transform t)
    {
        t.transform.localPosition = Vector3.zero;
        t.transform.localRotation = Quaternion.identity;
        t.transform.localScale = Vector3.one;
    }
    public static T GetOrAdd<T>(this Transform t) where T : Component
    {
        var component = t.GetComponent<T>();
        if(component != null)
        {
            return component;
        }
        return t.gameObject.AddComponent<T>();
    }
}
