using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro; // Using TextMeshPro for better VR text

/// <summary>
/// VR Motor Controller for Meta Quest
/// Handles VR-specific interactions, UI, haptics, and audio
/// Compatible with Unity 6 XR Interaction Toolkit
/// </summary>
public class VRMotorController : MonoBehaviour
{
    [Header("═══════════ Motor Reference ═══════════")]
    [Tooltip("Reference to the MotorController script")]
    public MotorController motorController;
    
    [Header("═══════════ VR UI Canvas ═══════════")]
    [Tooltip("World space canvas for VR UI")]
    public Canvas vrCanvas;
    
    [Tooltip("XR Origin (VR camera rig)")]
    public Transform xrOrigin;
    
    [Tooltip("Distance from camera to place UI")]
    [Range(1f, 5f)]
    public float canvasDistance = 2.5f;
    
    [Tooltip("Height offset for UI placement")]
    [Range(-1f, 3f)]
    public float canvasHeight = 0f;
    
    [Tooltip("Should UI follow the player's gaze?")]
    public bool followGaze = true;
    
    [Header("═══════════ UI Elements ═══════════")]
    [Tooltip("Main status display text")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Speed control slider")]
    public Slider speedSlider;
    
    [Tooltip("Start button")]
    public Button startButton;
    
    [Tooltip("Stop button")]
    public Button stopButton;
    
    [Tooltip("Connection status indicator")]
    public TextMeshProUGUI connectionStatusText;
    
    [Header("═══════════ Visual Feedback ═══════════")]
    [Tooltip("Motor visual GameObject")]
    public GameObject motorVisual;
    
    [Tooltip("Material when motor is running")]
    public Material motorRunningMaterial;
    
    [Tooltip("Material when motor is stopped")]
    public Material motorStoppedMaterial;
    
    [Tooltip("Particle effect when motor is running")]
    public ParticleSystem motorParticles;
    
    [Header("═══════════ Audio Feedback ═══════════")]
    [Tooltip("Audio source for motor sound")]
    public AudioSource motorAudioSource;
    
    [Tooltip("Motor running sound clip")]
    public AudioClip motorRunningSound;
    
    [Range(0.1f, 2f)]
    public float minPitch = 0.5f;
    
    [Range(0.5f, 3f)]
    public float maxPitch = 2.0f;
    
    [Header("═══════════ Haptic Feedback ═══════════")]
    [Tooltip("Left hand controller")]
    public ActionBasedController leftController;
    
    [Tooltip("Right hand controller")]
    public ActionBasedController rightController;
    
    [Range(0f, 1f)]
    public float hapticIntensity = 0.5f;
    
    [Range(0.01f, 1f)]
    public float hapticDuration = 0.1f;
    
    [Header("═══════════ Advanced Settings ═══════════")]
    [Tooltip("Update UI every X seconds")]
    public float uiUpdateInterval = 0.1f;
    
    [Tooltip("Enable debug logging")]
    public bool debugMode = false;
    
    // Private variables
    private Renderer motorRenderer;
    private float lastUIUpdate = 0f;
    private Camera mainCamera;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeVR();
    }
    
    void InitializeVR()
    {
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  VR Motor Controller - Initializing...                    ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
        
        // Find motor controller if not assigned
        if (motorController == null)
        {
            motorController = FindObjectOfType<MotorController>();
            if (motorController == null)
            {
                Debug.LogError("✗ MotorController not found! Please assign it in the inspector.");
                return;
            }
        }
        
        // Find XR Origin if not assigned
        if (xrOrigin == null)
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
            }
        }
        
        // Get main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null && xrOrigin != null)
            {
                mainCamera = xrOrigin.GetComponentInChildren<Camera>();
            }
        }
        
        // Setup motor visual
        if (motorVisual != null)
        {
            motorRenderer = motorVisual.GetComponent<Renderer>();
        }
        else if (motorController != null)
        {
            motorVisual = motorController.gameObject;
            motorRenderer = motorVisual.GetComponent<Renderer>();
        }
        
        // Setup audio
        if (motorAudioSource == null && motorVisual != null)
        {
            motorAudioSource = motorVisual.GetComponent<AudioSource>();
            if (motorAudioSource == null)
            {
                motorAudioSource = motorVisual.AddComponent<AudioSource>();
            }
        }
        
        if (motorAudioSource != null)
        {
            motorAudioSource.loop = true;
            motorAudioSource.playOnAwake = false;
            motorAudioSource.spatialBlend = 1.0f; // 3D sound
            motorAudioSource.minDistance = 1f;
            motorAudioSource.maxDistance = 10f;
        }
        
        // Setup UI button listeners
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
            Debug.Log("✓ Start button configured");
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopButtonClick);
            Debug.Log("✓ Stop button configured");
        }
        
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(OnSpeedSliderChange);
            speedSlider.minValue = 0;
            speedSlider.maxValue = 255;
            Debug.Log("✓ Speed slider configured");
        }
        
        isInitialized = true;
        Debug.Log("╔════════════════════════════════════════════════════════════╗");
        Debug.Log("║  VR Motor Controller - Ready!                             ║");
        Debug.Log("╚════════════════════════════════════════════════════════════╝");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Update canvas position
        if (vrCanvas != null && mainCamera != null && followGaze)
        {
            UpdateCanvasPosition();
        }
        
        // Update visual feedback
        UpdateMotorVisuals();
        
        // Update audio feedback
        UpdateMotorAudio();
        
        // Update UI
        if (Time.time - lastUIUpdate >= uiUpdateInterval)
        {
            UpdateUI();
            lastUIUpdate = Time.time;
        }
        
        // Handle VR controller input
        HandleVRInput();
    }
    
    void UpdateCanvasPosition()
    {
        Vector3 targetPosition = mainCamera.transform.position + 
                                 mainCamera.transform.forward * canvasDistance;
        targetPosition.y = mainCamera.transform.position.y + canvasHeight;
        
        vrCanvas.transform.position = Vector3.Lerp(
            vrCanvas.transform.position, 
            targetPosition, 
            Time.deltaTime * 3f
        );
        
        Vector3 lookDirection = mainCamera.transform.position - vrCanvas.transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDirection);
            vrCanvas.transform.rotation = Quaternion.Slerp(
                vrCanvas.transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }
    
    void UpdateMotorVisuals()
    {
        if (motorRenderer == null || motorController == null) return;
        
        if (motorController.motorOn)
        {
            if (motorRunningMaterial != null)
                motorRenderer.material = motorRunningMaterial;
            
            float glowIntensity = motorController.rpm / motorController.maxRPM;
            if (motorRenderer.material.HasProperty("_EmissionColor"))
            {
                Color emissionColor = Color.cyan * Mathf.LinearToGammaSpace(glowIntensity * 2f);
                motorRenderer.material.SetColor("_EmissionColor", emissionColor);
            }
            
            if (motorParticles != null && !motorParticles.isPlaying)
                motorParticles.Play();
        }
        else
        {
            if (motorStoppedMaterial != null)
                motorRenderer.material = motorStoppedMaterial;
            
            if (motorParticles != null && motorParticles.isPlaying)
                motorParticles.Stop();
        }
    }
    
    void UpdateMotorAudio()
    {
        if (motorAudioSource == null || motorController == null) return;
        
        if (motorController.motorOn && motorController.rpm > 0)
        {
            if (!motorAudioSource.isPlaying && motorRunningSound != null)
            {
                motorAudioSource.clip = motorRunningSound;
                motorAudioSource.Play();
            }
            
            float rpmPercent = motorController.rpm / motorController.maxRPM;
            motorAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, rpmPercent);
            motorAudioSource.volume = Mathf.Lerp(0.3f, 1.0f, rpmPercent);
        }
        else
        {
            if (motorAudioSource.isPlaying)
                motorAudioSource.Stop();
        }
    }
    
    void UpdateUI()
    {
        if (motorController == null) return;
        
        if (statusText != null)
        {
            string statusColor = motorController.motorOn ? "#00FF00" : "#FF0000";
            string statusIcon = motorController.motorOn ? "■" : "□";
            
            statusText.text = $"<size=48><b>MOTOR CONTROL</b></size>\n\n" +
                             $"<color=#00FFFF><b>RPM:</b></color> <size=40><b>{motorController.rpm:F1}</b></size>\n" +
                             $"<color=#FFFF00><b>Speed:</b></color> {motorController.currentSpeed}/255\n" +
                             $"<color=#FF4444><b>Temp:</b></color> {motorController.temperature:F1}°C\n" +
                             $"<color=#44FF44><b>Current:</b></color> {motorController.current:F0}mA\n" +
                             $"<color=#FF8844><b>Voltage:</b></color> {motorController.voltage:F2}V\n" +
                             $"<color=#FF44FF><b>Power:</b></color> {motorController.power:F0}mW\n" +
                             $"<color=#FFFFFF><b>Vibration:</b></color> {motorController.vibration:F2}\n\n" +
                             $"<size=36><b><color={statusColor}>{statusIcon} {(motorController.motorOn ? "RUNNING" : "STOPPED")}</color></b></size>";
        }
        
        if (connectionStatusText != null)
        {
            if (motorController.usePhysicalMotor)
            {
                if (motorController.isConnected)
                    connectionStatusText.text = $"<color=#00FF00>● CONNECTED</color>\n{motorController.esp32IP}";
                else
                    connectionStatusText.text = $"<color=#FF0000>● DISCONNECTED</color>\n{motorController.lastError}";
            }
            else
            {
                connectionStatusText.text = "<color=#FFFF00>◆ SIMULATION MODE</color>";
            }
        }
        
        if (speedSlider != null)
        {
            speedSlider.value = motorController.currentSpeed;
        }
    }
    
    void HandleVRInput()
    {
        if (rightController != null)
        {
            if (rightController.activateAction.action.WasPressedThisFrame())
                OnStartButtonClick();
            
            if (rightController.selectAction.action.WasPressedThisFrame())
                OnStopButtonClick();
        }
        
        if (leftController != null)
        {
            if (leftController.activateAction.action.WasPressedThisFrame())
            {
                motorController.ToggleMotor();
                SendHapticFeedback();
            }
        }
    }
    
    void SendHapticFeedback()
    {
        if (leftController != null)
            leftController.SendHapticImpulse(hapticIntensity, hapticDuration);
        
        if (rightController != null)
            rightController.SendHapticImpulse(hapticIntensity, hapticDuration);
        
        if (debugMode)
            Debug.Log("Haptic feedback sent");
    }
    
    void OnStartButtonClick()
    {
        motorController.StartMotor();
        SendHapticFeedback();
        if (debugMode) Debug.Log("→ Start button pressed");
    }
    
    void OnStopButtonClick()
    {
        motorController.StopMotor();
        SendHapticFeedback();
        if (debugMode) Debug.Log("→ Stop button pressed");
    }
    
    void OnSpeedSliderChange(float value)
    {
        int speed = Mathf.RoundToInt(value);
        motorController.SetMotorSpeed(speed);
        
        if (rightController != null)
            rightController.SendHapticImpulse(0.1f, 0.05f);
    }
    
    public void SetCanvasDistance(float distance) => canvasDistance = Mathf.Clamp(distance, 1f, 5f);
    public void SetCanvasHeight(float height) => canvasHeight = Mathf.Clamp(height, -1f, 3f);
    public void ToggleFollowGaze() => followGaze = !followGaze;
}