using System.Collections.Generic;
using TriInspector;
using UnityEngine;

public class SerilizationTest : MonoBehaviour
{
    public float MyFloat = 32f;
    [SerializeField]
    private float _myPrivateFloat = 12;  
    [Button]
    public void AddToFloat()
    {
        _myPrivateFloat += 1;
        Debug.Log(_myPrivateFloat);
    }
}
