# Gear Motor XR Control and Telemetry System

## Overview
This project is a Unity-based virtual motor monitoring and control system designed for monitoring a gear-driven motor in both desktop simulation mode and real hardware mode. The architecture combines interactive 3D visualization, telemetry dashboards, and XR controls so the motor can be observed and controlled either by keyboard or in a VR environment.

At its core, the project is built around a single motor model (`MotorController`) that exposes runtime data such as RPM, speed, temperature, current, voltage, power, and vibration. That data is then consumed by several supporting systems:

- `GearRotate.cs` visually spins connected gears based on motor speed.
- `VRMotorController.cs` adds VR UI, haptic feedback, audio, particle effects, and controller-driven interaction.
- `SimpleChartManager.cs` draws real-time charts for telemetry values.

The system can run in two modes:

1. Simulation mode: fully works without a physical motor.
2. Physical mode: connects to an ESP32 device over HTTP and syncs live sensor and motor values.

This makes the project suitable for simulation, demonstration, lab experimentation, and XR training scenarios.

---

## High-level architecture

The project follows a simple event-driven Unity component architecture built around `MonoBehaviour` scripts.

```text
                  +---------------------+
                  |  ESP32 Device       |
                  |  /status            |
                  |  /control           |
                  +----------+----------+
                             |
                             v
                  +---------------------+
                  | MotorController     |
                  | - rpm               |
                  | - motorOn           |
                  | - temperature       |
                  | - current           |
                  | - voltage           |
                  | - power             |
                  | - vibration         |
                  +-----+-----------+---+
                        |           |
                        |           +----------------------+
                        |                                      |
                        v                                      v
              +--------------------+              +--------------------------+
              | GearRotate         |              | VRMotorController        |
              | rotates gear mesh  |              | UI, audio, haptics,     |
              | based on RPM       |              | control actions          |
              +--------------------+              +--------------------------+
                        |
                        v
              +--------------------+
              | SimpleChartManager |
              | telemetry plots    |
              +--------------------+
```

### Data flow
1. The motor controller determines current state.
2. Real sensor data is fetched from the ESP32 (physical mode) or simulated locally (simulation mode).
3. `GearRotate` synchronizes the visible gear motion to motor RPM.
4. `VRMotorController` exposes the current state to a user through VR UI, rendered text, haptics, audio, and controller inputs.
5. `SimpleChartManager` visualizes recent telemetry as graphs.

---

## Core components

### 1) MotorController
File: `Assets/MotorController.cs`

This is the central logic controller of the project.

Responsibilities:
- Maintains the motor runtime state: `rpm`, `motorOn`, `currentSpeed`, `temperature`, `current`, `voltage`, `power`, `vibration`.
- Connects to an ESP32 through HTTP endpoints at `/status` and `/control`.
- Supports either physical hardware or simulation behavior.
- Exposes public methods such as `StartMotor()`, `StopMotor()`, `SetMotorSpeed()`, and `ToggleMotor()`.
- Provides getter methods that other scripts can call for telemetry display.

Data model:
- `MotorStatus` serializes status data from the ESP32 using `JsonUtility`.
- `MotorCommand` represents POST-body commands sent to the ESP32.

Key behavior:
- In physical mode, a coroutine continuously polls the ESP32 status endpoint using `UnityWebRequest.Get`.
- In simulation mode, speed is updated by keyboard input or through public API calls and is not tied to external hardware.
- Keyboard commands allow interactive control:
  - `Space` toggles motor on/off
  - `Up/Down` arrows increase/decrease speed
  - `S` stops the motor

This controller effectively acts as the system bus or central state engine for all other scripts.

### 2) GearRotate
File: `Assets/GearRotate.cs`

This script is responsible for the visual movement of gear meshes.

Responsibilities:
- Reads the connected `MotorController` reference.
- Converts RPM to degrees per second.
- Applies rotational motion to the object using `transform.Rotate(...)`.
- Supports `gearRatio` and `reverseDirection` configuration.

Mathematically, it converts motor speed to local visual rotation:

```csharp
float rpm = motor.rpm * gearRatio;
float dps = rpm * 6f;
float dir = reverseDirection ? -1f : 1f;
transform.Rotate(Vector3.forward * dir * dps * Time.deltaTime, Space.Self);
```

This is the representation layer for the mechanical system. It turns raw motor data into visible motion.

### 3) VRMotorController
File: `Assets/VRMotorController.cs`

This is the XR-facing user experience layer.

Responsibilities:
- Finds and links to the `MotorController`.
- Builds a world-space VR canvas for displaying telemetry.
- Runs in XR environments with `XR Interaction Toolkit` using `ActionBasedController`.
- Updates UI labels for RPM, speed, temperature, current, voltage, power, vibration, and connection state.
- Applies visual feedback (material changes and emission glow) when the motor is running.
- Starts and stops motor audio based on runtime state.
- Triggers haptic pulses on controller interaction.
- Offers button and slider-driven control for motor activation and speed modification.

Key features:
- Gaze-following canvas placement.
- Real-time status text updates.
- Audio pitch tied to RPM.
- Particle system toggling during operation.
- Haptic feedback for immersive motor control.

This makes the project usable in VR headsets such as Meta Quest or other XR-compatible platforms.

### 4) SimpleChartManager
File: `Assets/SimpleChartManager.cs`

This component handles telemetry visualization.

Responsibilities:
- Stores recent values in queues for RPM, temperature, current, and power.
- Updates real-time `LineRenderer` graphs.
- Updates numeric HUD text values.
- Maintains a rolling window of data points for smooth trend charts.

It is a lightweight charting system intended to provide monitoring insight without external charting packages.

### 5) Unity Input and XR interaction assets
Files:
- `Assets/InputSystem_Actions.inputactions`
- Unity XR packages included from `Packages/manifest.json`

These assets enable the project to work across standard keyboard input and XR devices.

The project includes Unity's `InputSystem` package and XR Interaction Toolkit packages, allowing the project to support:
- desktop testing
- XR controller interaction
- Meta Quest / Oculus device support
- gamepad or controller input if needed

---

## Runtime behavior and operating modes

### Simulation mode
When `usePhysicalMotor` is false:
- `MotorController` ignores ESP32 connectivity.
- User input can still drive the motor state.
- The motor can be started/stopped and speed can be adjusted via keyboard or API.
- The rest of the system behaves as though a motor is active, allowing demonstration without hardware.

This mode is useful for previewing the system in the editor, debugging visual logic, or creating immersive demos without hardware.

### Physical motor mode
When `usePhysicalMotor` is true:
- `Start()` initializes the controller with the configured ESP32 IP address.
- It polls the status endpoint continuously.
- The controller updates internal values from real sensor readings.
- Commands are sent to `/control` using HTTP POST requests.
- If the ESP32 is unavailable, the controller reports a connection loss through `isConnected` and `lastError`.

This is the real-world integration path for a physical motor unit or industrial hardware platform.

---

## Technology stack

### Core engine and platform
- Unity 6000.3.4f1
- C# with Unity's `MonoBehaviour` architecture
- Unity GameObject / component model
- URP (Universal Render Pipeline)

### Input and interaction
- Unity Input System
- XR Interaction Toolkit 3.3.1
- XR Management
- Oculus XR plugin
- Action-based VR controllers

### UI technology used in this project
- Unity UI (`UnityEngine.UI`) for UI elements such as `Button`, `Slider`, and `Canvas`
- TextMeshPro (`TMPro`) for high-quality text rendering via `TextMeshProUGUI`
- World-space VR UI using `Canvas` positioned in front of the camera
- `RectTransform` for dashboard layout and chart panel positioning
- `LineRenderer` for real-time charts and trend visualization
- `Material` and emissive color changes to reflect motor state visually inside the UI/scene
- `AudioSource` and `ParticleSystem` as additional feedback elements paired with the UI/interaction layer

### Real-time communication
- `UnityWebRequest` for REST communication
- JSON serialization with `JsonUtility`
- HTTP-based control/status protocol with ESP32

### Visual effects and audio
- `ParticleSystem` for motor activity feedback
- `AudioSource` and `AudioClip` for motor sound
- Material emissive effects for running state feedback

### General runtime features
- Coroutines for periodic polling
- Time-based updates using `Time.deltaTime`
- Lightweight telemetry data queue system for charts

---

## Project structure

```text
gearmotorext/
├── Assets/
│   ├── GearRotate.cs
│   ├── MotorController.cs
│   ├── VRMotorController.cs
│   ├── SimpleChartManager.cs
│   ├── InputSystem_Actions.inputactions
│   ├── Scenes/
│   ├── Settings/
│   ├── Samples/
│   ├── XR/
│   ├── XRI/
│   └── model/
├── Packages/
│   └── manifest.json
├── ProjectSettings/
├── UserSettings/
├── Assembly-CSharp.csproj
├── Assembly-CSharp-Editor.csproj
├── gearmotorext.slnx
├── README.md
└── Library/
```

---

## Notable design decisions

### 1) Centralized state management
`MotorController` is the single source of truth for the motor state. All other systems read from it rather than each storing their own independent state. This keeps telemetry, VR UI, and motion synchronization coherent.

### 2) Separation of concerns
- `MotorController`: hardware/sensor state and commands
- `GearRotate`: mechanical animation
- `VRMotorController`: user experience and interaction
- `SimpleChartManager`: visualization

This separation makes the system modular and easier to expand.

### 3) Hardware abstraction
Physical and simulation behavior share the same external API. That means the UI and visual logic do not need to know whether the data is coming from a real ESP32 or from a simulated motor.

### 4) Real-world integration
The project is not just a visual demo; it is designed to communicate with physical hardware over HTTP. This is valuable for industrial learning environments and engineering prototypes.

---

## Intended use cases

This project can be used for:

- motor visualization in a 3D engineering environment
- educational demonstrations of gear rotation and motor behavior
- VR training and inspection workflows
- telemetry dashboards for real-time monitoring
- hardware-in-the-loop prototyping with an ESP32-connected motor

---

## Setup instructions

### Prerequisites
- Unity 6000.3.4f1 or compatible Unity version
- XR-enabled setup if using Meta Quest or other VR hardware
- Optional ESP32 module with a matching HTTP endpoint implementation

### For simulation mode
1. Open the Unity project.
2. Load the scene containing the motor object.
3. Ensure `MotorController` is attached to the motor GameObject.
4. Set `usePhysicalMotor = false` in the Inspector.
5. Press Play in the editor.
6. Use keyboard controls to start/stop and adjust speed.

### For physical mode
1. Attach `MotorController` to the motor object.
2. Set `usePhysicalMotor = true`.
3. Set `esp32IP` to the ESP32 device IP address.
4. Ensure the ESP32 exposes `/status` and `/control` endpoints.
5. Build and run the Unity scene.
6. Confirm that the connection state and telemetry reflect the live device.

---

## Limitations and notes

- The project is a simulation and visualization framework rather than a full industrial control system.
- Communication is based on a simplified HTTP JSON protocol and assumes a compatible ESP32 side implementation.
- The code uses Unity's standard telemetry model and is well suited for prototype and educational use.
- The project is intended to be extended for richer safety logic, device validation, persistence, and production-grade control.

---

## Summary
This project is a Unity XR motor monitoring system with a real-time telemetry dashboard, gear-based mechanical animation, and ESP32 hardware integration. It demonstrates how simulation, live sensor data, user input, and immersive VR interfaces can be combined into a single engineering visual platform.

The architecture is deliberately modular:
- `MotorController` owns the electrical/operational state.
- `GearRotate` visualizes motion.
- `VRMotorController` creates the immersive interaction layer.
- `SimpleChartManager` surfaces telemetry over time.

Together, these components form a practical and extensible framework for studying motor behavior in both simulated and physical environments.
