using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Enhanced Motor Controller with ESP32 Digital Twin Integration
/// Replaces the original MotorController.cs
/// Compatible with existing GearRotate.cs
/// </summary>

[Serializable]
public class MotorStatus
{
    public bool running;
    public int speed;
    public float rpm;
    public float temperature;
    public float current;
    public float voltage;
    public float power;
    public float vibration;
    public long timestamp;
}

[Serializable]
public class MotorCommand
{
    public string command;
    public int speed;
}

public class MotorController : MonoBehaviour
{
    [Header("═══════════ ESP32 Configuration ═══════════")]
    [Tooltip("IP address of your ESP32 (check Serial Monitor after upload)")]
    public string esp32IP = "192.168.1.100"; // ← UPDATE THIS!
    
    [Tooltip("How many times per second to sync with ESP32")]
    [Range(1, 30)]
    public int updateRate = 10;
    
    [Tooltip("Enable to use physical ESP32, disable for simulation only")]
    public bool usePhysicalMotor = true;
    
    [Header("═══════════ Motor Parameters ═══════════")]
    [Tooltip("Maximum RPM of the motor")]
    public float maxRPM = 150;
    
    [Tooltip("Rotation axis for the motor visual")]
    public Vector3 rotationAxis = Vector3.forward;
    
    [Header("═══════════ Current Status (Read-Only) ═══════════")]
    [Tooltip("Current RPM - synced with physical motor if connected")]
    public float rpm = 0f;
    
    [Tooltip("Motor running state")]
    public bool motorOn = false;
    
    [Tooltip("Current motor speed (0-255)")]
    public int currentSpeed = 0;
    
    [Header("═══════════ Sensor Data (Read-Only) ═══════════")]
    public float temperature = 0f;
    public float current = 0f;
    public float voltage = 0f;
    public float power = 0f;
    public float vibration = 0f;
    
    [Header("═══════════ Connection Status ═══════════")]
    public bool isConnected = false;
    public string lastError = "";
    
    // Private variables
    private string baseURL;
    private MotorStatus physicalStatus = new MotorStatus();
    private bool isUpdating = false;
    private float simulatedRPM = 0f;
    
    void Start()
    {
        baseURL = $"http://{esp32IP}";
        
        if (usePhysicalMotor)
        {
            Debug.Log($"╔═══════════════════════════════════════════════════════════════╗");
            Debug.Log($"║  Motor Controller Started - Physical Mode                    ║");
            Debug.Log($"║  ESP32 IP: {esp32IP,-44} ║");
            Debug.Log($"╚═══════════════════════════════════════════════════════════════╝");
            
            // Start continuous status updates from ESP32
            StartCoroutine(ContinuousStatusUpdate());
        }
        else
        {
            Debug.Log("Motor Controller Started - Simulation Mode (No ESP32)");
        }
    }
    
    void Update()
    {
        // Handle keyboard input (works in both physical and simulation mode)
        HandleKeyboardInput();
        
        // Update motor rotation based on RPM
        if (motorOn)
        {
            float degreesPerSecond = rpm * 6f; // RPM to degrees/second
            transform.Rotate(rotationAxis * degreesPerSecond * Time.deltaTime);
        }
    }
    
    void HandleKeyboardInput()
    {
        // Space to toggle motor on/off
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (motorOn)
                StopMotor();
            else
                StartMotor();
        }
        
        // Up arrow to increase speed
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (usePhysicalMotor)
            {
                int newSpeed = Mathf.Min(currentSpeed + 2, 255);
                SetMotorSpeed(newSpeed);
            }
            else
            {
                // Simulation mode
                simulatedRPM += 50f * Time.deltaTime;
                simulatedRPM = Mathf.Clamp(simulatedRPM, 0, maxRPM);
                rpm = simulatedRPM;
            }
        }
        
        // Down arrow to decrease speed
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (usePhysicalMotor)
            {
                int newSpeed = Mathf.Max(currentSpeed - 2, 0);
                SetMotorSpeed(newSpeed);
            }
            else
            {
                // Simulation mode
                simulatedRPM -= 50f * Time.deltaTime;
                simulatedRPM = Mathf.Clamp(simulatedRPM, 0, maxRPM);
                rpm = simulatedRPM;
            }
        }
        
        // S key to stop
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMotor();
        }
    }
    
    IEnumerator ContinuousStatusUpdate()
    {
        while (usePhysicalMotor)
        {
            yield return StartCoroutine(GetMotorStatus());
            yield return new WaitForSeconds(1f / updateRate);
        }
    }
    
    IEnumerator GetMotorStatus()
    {
        string url = $"{baseURL}/status";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Set timeout
            request.timeout = 2;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    physicalStatus = JsonUtility.FromJson<MotorStatus>(request.downloadHandler.text);
                    
                    // Update local variables from physical motor
                    motorOn = physicalStatus.running;
                    rpm = physicalStatus.rpm;
                    currentSpeed = physicalStatus.speed;
                    temperature = physicalStatus.temperature;
                    current = physicalStatus.current;
                    voltage = physicalStatus.voltage;
                    power = physicalStatus.power;
                    vibration = physicalStatus.vibration;
                    
                    isConnected = true;
                    lastError = "";
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse motor status: {e.Message}");
                    isConnected = false;
                    lastError = "Parse error";
                }
            }
            else
            {
                isConnected = false;
                lastError = request.error;
                
                // Only log error occasionally to avoid spam
                if (Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning($"Cannot connect to ESP32 at {esp32IP}: {request.error}");
                }
            }
        }
    }
    
    public void SendCommand(string command, int speed = 0)
    {
        if (!usePhysicalMotor)
        {
            Debug.Log($"Simulation mode - ignoring command: {command}");
            return;
        }
        
        StartCoroutine(SendMotorCommand(command, speed));
    }
    
    IEnumerator SendMotorCommand(string command, int speed)
    {
        string url = $"{baseURL}/control";
        
        MotorCommand cmd = new MotorCommand
        {
            command = command,
            speed = speed
        };
        
        string jsonData = JsonUtility.ToJson(cmd);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 2;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Command '{command}' sent to ESP32");
            }
            else
            {
                Debug.LogError($"✗ Command failed: {request.error}");
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC API - Can be called from VR controllers or UI
    // ═══════════════════════════════════════════════════════════════════
    
    public void SetMotorSpeed(int speed)
    {
        speed = Mathf.Clamp(speed, 0, 255);
        
        if (usePhysicalMotor)
        {
            SendCommand("setSpeed", speed);
        }
        else
        {
            // Simulation mode
            currentSpeed = speed;
            simulatedRPM = (speed / 255f) * maxRPM;
            rpm = simulatedRPM;
        }
    }
    
    public void StartMotor()
    {
        if (usePhysicalMotor)
        {
            SendCommand("start", currentSpeed > 0 ? currentSpeed : 128);
        }
        else
        {
            // Simulation mode
            motorOn = true;
            if (simulatedRPM == 0)
            {
                simulatedRPM = maxRPM / 2;
                rpm = simulatedRPM;
            }
            Debug.Log("✓ Motor started (Simulation)");
        }
    }
    
    public void StopMotor()
    {
        if (usePhysicalMotor)
        {
            SendCommand("stop", 0);
        }
        else
        {
            // Simulation mode
            motorOn = false;
            simulatedRPM = 0;
            rpm = 0;
            Debug.Log("✓ Motor stopped (Simulation)");
        }
    }
    
    public void ToggleMotor()
    {
        if (motorOn)
            StopMotor();
        else
            StartMotor();
    }
    
    // Getters for external scripts (like chart manager)
    public float GetRPM() => rpm;
    public float GetTemperature() => temperature;
    public float GetCurrent() => current;
    public float GetVoltage() => voltage;
    public float GetPower() => power;
    public float GetVibration() => vibration;
    public bool IsRunning() => motorOn;
    public bool IsConnectedToESP32() => isConnected;
}