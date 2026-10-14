# Simple Architecture Notes

This project is a Unity-based motor control and monitoring system.

Think of it like this:

- A motor has a real state: ON/OFF, speed, RPM, temperature, power, etc.
- A Unity scene shows that motor visually as a rotating gear.
- A VR interface shows the same motor data to the user.
- A telemetry dashboard displays charts and numbers.
- A real ESP32 device can send live data if connected.

So the whole project is a small digital twin of a motor system.

---

## 1. Big idea

The project has one main controller called `MotorController`.
This script acts like the brain of the whole system.

It stores and updates the motor data.
Then other scripts read this data and do different jobs:

- `GearRotate` moves the gear visually
- `VRMotorController` shows the status in VR and lets the user interact
- `SimpleChartManager` plots trends like RPM and temperature

So the main design pattern is:

`MotorController` = source of truth
Other scripts = viewers and controllers

---

## 2. Main building blocks

### A. MotorController
This is the most important script.
It does the following:

- stores the live motor state
- reads sensor values
- sends commands to the physical motor
- works in simulation mode or hardware mode
- lets other scripts ask for values like RPM or temperature

Important variables:

- `rpm` = motor speed in rotations per minute
- `motorOn` = whether the motor is running
- `currentSpeed` = motor speed from 0 to 255
- `temperature` = heat reading
- `current` = electrical current
- `voltage` = voltage
- `power` = power usage
- `vibration` = vibration level

It also has methods like:

- `StartMotor()`
- `StopMotor()`
- `SetMotorSpeed(int speed)`
- `ToggleMotor()`
- `GetRPM()`, `GetTemperature()`, etc.

This means the project is built around one central state system instead of separate random scripts storing their own values.

---

### B. GearRotate
This is the visual motion system.
It takes the motor RPM and turns it into a rotating mesh.

Example:

- if motor RPM is high, the gear spins faster
- if motor RPM is low, it spins slower
- if reverse direction is enabled, it spins backward

The logic is very simple:

- get RPM from motor
- convert to degrees/second
- rotate the object on the Z axis or chosen axis

This is how the 3D gear looks alive.

---

### C. VRMotorController
This is the user-facing Unity UI controller for VR.
It connects the motor logic with the user interface.

It handles:

- world-space canvas for VR display
- motor status text
- speed slider
- start button and stop button
- connection status
- audio when motor runs
- particles when motor is active
- haptic feedback on the VR controllers

It also looks for the main camera and positions the UI in front of the user.
This makes the user feel like they are standing in front of a real machine panel.

So this script is like the dashboard + control panel for VR.

---

### D. SimpleChartManager
This is the telemetry graph script.
It monitors recent values and draws graphs.

It keeps several queues of numbers:

- RPM history
- temperature history
- current history
- power history

Then it updates `LineRenderer` objects to draw those values over time.

It also updates text fields showing the current values.

So this is the monitoring part of the system.

---

## 3. How the system works at runtime

### Case 1: Simulation mode
When `usePhysicalMotor` is false:

- the project does not try to connect to real hardware
- the motor values are simulated locally
- user can start/stop motor and change speed
- the gear rotates based on these simulated values
- the charts and UI look normal

This is useful for:

- testing without hardware
- demos
- learning
- debug in Unity editor

---

### Case 2: Physical motor mode
When `usePhysicalMotor` is true:

- `MotorController` tries to connect to the ESP32 IP address
- it calls the `/status` endpoint repeatedly
- it reads JSON data from the ESP32
- updates `rpm`, `temperature`, `current`, etc.
- sends commands to `/control` when motor is started or speed changes

This means the Unity project can act as a digital dashboard for a real motor system.

---

## 4. Communication flow

The system basically follows this flow:

1. User presses Start or uses keyboard input
2. `MotorController` receives the control command
3. It sends a request to the ESP32 if hardware mode is enabled
4. Motor state is updated in Unity
5. `GearRotate` rotates the gear according to RPM
6. `VRMotorController` shows the status to the user
7. `SimpleChartManager` plots real-time charts

This is a clean pipeline:

User input -> MotorController -> Data state -> Visual system + UI + charts

---

## 5. Why the architecture is good

This project is intentionally simple and clean.

### Good points:

- One central motor object controls most logic
- UI, visuals, and charts do not have to manage data themselves
- simulation and physical hardware share the same public API
- each script has one job
- easy to extend later

### In plain English:

The project separates:

- the actual motor logic
- the moving gear animation
- the VR interface
- the chart display

This separation is very important in real projects because it avoids chaos.

---

## 6. Layer-by-layer architecture

### Layer 1: Device layer
This is the physical hardware and real motor.
In this project, the hardware is represented by an ESP32.
It exposes:

- `/status` to get current motor measurements
- `/control` to send commands

This layer is outside Unity but connected to it.

---

### Layer 2: Controller layer
This is `MotorController`.
It translates hardware data into Unity-friendly values.
It also translates Unity commands into hardware commands.

Think of it as the translator between real machine and Unity scene.

---

### Layer 3: Visual layer
This includes:

- `GearRotate`
- GameObjects and meshes
- material changes
- particle effects

This layer makes the system visible and understandable.

---

### Layer 4: User interaction layer
This includes:

- VR UI
- buttons
- sliders
- keyboard controls
- controller trigger actions

This is the part a user interacts with directly.

---

### Layer 5: Monitoring layer
This includes:

- telemetry text
- chart rendering
- live graphs

This is for observation and analysis.

---

## 7. What the project is really doing

This project is not just a simple spinning gear.
It is a small industrial monitoring dashboard built in Unity.

It is doing three things together:

1. controlling a motor
2. visualizing the motor in 3D
3. monitoring and displaying its health in real time

That is why it uses both:

- interactive 3D visuals
- telemetry and status panels

---

## 8. How data moves through the app

Think of it like this:

`ESP32 / keyboard / VR input` -> `MotorController` -> `Motor state` -> `Gear rotation + UI + charts`

Data is not copied everywhere randomly.
It flows through one central controller, which keeps everything synchronized.

That is the heart of the architecture.

---

## 9. Simple flow diagram

```text
+--------------------+
| User input         |
| keyboard / VR      |
+---------+----------+
          |
          v
+--------------------+
| MotorController    |
| stores motor state |
+---------+----------+
          |
          +------------------------+
          |                        |
          v                        v
+------------------+     +----------------------+
| GearRotate       |     | VRMotorController    |
| rotate gear      |     | UI + buttons + VR   |
+------------------+     +----------------------+
          |
          v
+----------------------+
| SimpleChartManager    |
| charts and live data  |
+----------------------+
          |
          v
+----------------------+
| ESP32 physical motor  |
| if hardware enabled   |
+----------------------+
```

---

## 10. Final simple understanding

If I explain the full project in one sentence:

This project is a Unity app that simulates and monitors a motor, shows the motor movement visually in 3D, lets the user control it from keyboard or VR, and displays real-time electrical and operational telemetry while optionally connecting to a real ESP32-based motor.

In other words:

It is a digital twin of a motor system.

---

## 11. Important files in simple terms

- `MotorController.cs` = brain and data holder
- `GearRotate.cs` = gear animation
- `VRMotorController.cs` = VR dashboard and controls
- `SimpleChartManager.cs` = line graphs and telemetry
- `InputSystem_Actions.inputactions` = keyboard/controller inputs
- `Packages/manifest.json` = Unity packages used for XR, UI, and input

---

## 12. Short conclusion

The architecture is straightforward because it follows a single-source-of-truth setup:

- The motor state is centralized in one script.
- Different scripts consume that data for different purposes.
- Real hardware and simulation share the same system behavior.
- The project becomes easier to maintain and extend.

That is why the project feels organized even though it is made of a few scripts.
