using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple Real-Time Chart Manager using Unity UI
/// Displays motor telemetry data in VR-friendly format
/// No external dependencies required
/// </summary>
public class SimpleChartManager : MonoBehaviour
{
    [Header("═══════════ Motor Reference ═══════════")]
    public MotorController motorController;
    
    [Header("═══════════ Chart Panels ═══════════")]
    public RectTransform rpmChartPanel;
    public RectTransform temperatureChartPanel;
    public RectTransform currentChartPanel;
    public RectTransform powerChartPanel;
    
    [Header("═══════════ Text Displays ═══════════")]
    public TextMeshProUGUI rpmValueText;
    public TextMeshProUGUI temperatureValueText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI powerValueText;
    public TextMeshProUGUI vibrationValueText;
    
    [Header("═══════════ Chart Settings ═══════════")]
    [Range(10, 100)]
    public int maxDataPoints = 50;
    
    [Range(0.1f, 2f)]
    public float updateInterval = 0.5f;
    
    public Color rpmColor = Color.cyan;
    public Color tempColor = Color.red;
    public Color currentColor = Color.yellow;
    public Color powerColor = Color.green;
    
    [Header("═══════════ Chart Lines ═══════════")]
    public LineRenderer rpmLine;
    public LineRenderer temperatureLine;
    public LineRenderer currentLine;
    public LineRenderer powerLine;
    
    // Data storage
    private Queue<float> rpmData = new Queue<float>();
    private Queue<float> tempData = new Queue<float>();
    private Queue<float> currentData = new Queue<float>();
    private Queue<float> powerData = new Queue<float>();
    
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        if (motorController == null)
        {
            motorController = FindObjectOfType<MotorController>();
        }
        
        InitializeCharts();
        
        Debug.Log("Chart Manager initialized");
    }
    
    void InitializeCharts()
    {
        // Setup line renderers
        if (rpmLine != null) SetupLineRenderer(rpmLine, rpmColor);
        if (temperatureLine != null) SetupLineRenderer(temperatureLine, tempColor);
        if (currentLine != null) SetupLineRenderer(currentLine, currentColor);
        if (powerLine != null) SetupLineRenderer(powerLine, powerColor);
    }
    
    void SetupLineRenderer(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.02f;
        line.endWidth = 0.02f;
        line.positionCount = 0;
        line.useWorldSpace = false;
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateData();
            UpdateCharts();
            UpdateTextDisplays();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateData()
    {
        if (motorController == null) return;
        
        // Add new data points
        AddDataPoint(rpmData, motorController.GetRPM());
        AddDataPoint(tempData, motorController.GetTemperature());
        AddDataPoint(currentData, motorController.GetCurrent());
        AddDataPoint(powerData, motorController.GetPower());
    }
    
    void AddDataPoint(Queue<float> queue, float value)
    {
        queue.Enqueue(value);
        
        while (queue.Count > maxDataPoints)
        {
            queue.Dequeue();
        }
    }
    
    void UpdateCharts()
    {
        UpdateLineChart(rpmLine, rpmData, motorController.maxRPM);
        UpdateLineChart(temperatureLine, tempData, 100f); // Max 100°C
        UpdateLineChart(currentLine, currentData, 1000f); // Max 1000mA
        UpdateLineChart(powerLine, powerData, 10000f); // Max 10W
    }
    
    void UpdateLineChart(LineRenderer line, Queue<float> data, float maxValue)
    {
        if (line == null || data.Count == 0) return;
        
        line.positionCount = data.Count;
        
        int index = 0;
        float xStep = 1f / Mathf.Max(1, data.Count - 1);
        
        foreach (float value in data)
        {
            float x = index * xStep;
            float y = Mathf.Clamp01(value / maxValue);
            
            line.SetPosition(index, new Vector3(x, y, 0));
            index++;
        }
    }
    
    void UpdateTextDisplays()
    {
        if (motorController == null) return;
        
        if (rpmValueText != null)
        {
            rpmValueText.text = $"<size=60><b>{motorController.rpm:F1}</b></size>\n<size=30>RPM</size>";
        }
        
        if (temperatureValueText != null)
        {
            temperatureValueText.text = $"<size=60><b>{motorController.temperature:F1}</b></size>\n<size=30>°C</size>";
        }
        
        if (currentValueText != null)
        {
            currentValueText.text = $"<size=60><b>{motorController.current:F0}</b></size>\n<size=30>mA</size>";
        }
        
        if (powerValueText != null)
        {
            powerValueText.text = $"<size=60><b>{motorController.power:F0}</b></size>\n<size=30>mW</size>";
        }
        
        if (vibrationValueText != null)
        {
            vibrationValueText.text = $"<size=60><b>{motorController.vibration:F2}</b></size>\n<size=30>Level</size>";
        }
    }
    
    public void ClearAllCharts()
    {
        rpmData.Clear();
        tempData.Clear();
        currentData.Clear();
        powerData.Clear();
        
        if (rpmLine != null) rpmLine.positionCount = 0;
        if (temperatureLine != null) temperatureLine.positionCount = 0;
        if (currentLine != null) currentLine.positionCount = 0;
        if (powerLine != null) powerLine.positionCount = 0;
        
        Debug.Log("All charts cleared");
    }
}