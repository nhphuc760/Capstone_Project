using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Test : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI time;

    DateTime start;
    void Start()
    {
        start = DateTime.UtcNow;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var TimeSpan = DateTime.UtcNow - start;
            time.text = TimeSpan.ToString();
            Debug.Log($"Span.Minute: {TimeSpan.Minutes}");
            Debug.Log($"Span.TotalMinute: {TimeSpan.TotalMinutes}");
            Debug.Log($"Span.Seconds: {TimeSpan.Seconds}");
            Debug.Log($"Span.TotalSecond: {TimeSpan.TotalSeconds}");
        }
    }  


}
