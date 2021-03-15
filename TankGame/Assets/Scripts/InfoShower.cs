using System;
using System.Runtime.CompilerServices;
using System.Timers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;

public class InfoShower : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    //// FPS
    [SerializeField] private bool _showFPS = true;
    private int _count = 0;
    private int _frameRateSum = 0;

    //// Timer
    [SerializeField] private bool _showTimer = true;
    
    private void Update()
    {
        ResetText();
        
        if (_showFPS) FPSStats();

        if (_showTimer) TimerStats();
    }

    private void ResetText()
    {
        _text.text = null;
    }

    private void TimerStats()
    {
        _text.text += $"Time: {Time.realtimeSinceStartup}\n";
    }

    private void FPSStats()
    {
        _count++;
        _frameRateSum += (int) (1f / Time.deltaTime);
        _text.text += $"FPS:{(int)(1f/Time.deltaTime)}\nMEAN:{_frameRateSum / _count}\nDELTA:{Time.deltaTime}\nFIXED:{Time.fixedDeltaTime}\n";
    }

    private void OnValidate()
    {
        if (_text == null)
        {
            _text = this.GetComponent<TextMeshProUGUI>();
        }
    }
}