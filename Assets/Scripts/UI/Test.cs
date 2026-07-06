using System;
using TMPro;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] TMP_InputField test;
    private void Awake()
    {
        test.onEndEdit.AddListener(OnEndEditInput);
        test.onSelect.AddListener(OnSelectInput);
    }

    private void OnSelectInput(string arg0)
    {
        Debug.Log("Select input: " + arg0);
    }

    private void OnEndEditInput(string arg0)
    {
        Debug.Log("End Edit called: " + arg0);
    }
}
