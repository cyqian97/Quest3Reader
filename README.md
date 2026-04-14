# Quest3Reader

## Introduction

Quest3Reader is a Unity application for Meta Quest 3 that streams real-time controller poses and button states over Android logcat. It replicates the wire protocol of the original `oculus_reader` C++ project (`OculusTeleop.cpp`), making it a drop-in replacement readable by the same Python/ROS tooling that parses the `wE9ryARX` logcat tag.

Each frame the app encodes both controller 4×4 transformation matrices and full button states into a single string, which an external process can read via `adb logcat`. An in-headset axis visualizer overlays the coordinate frames of both controllers and the world origin directly in VR.

---

## How to Use

### Testing in the Unity Editor

1. Open the project in Unity (2022.3 LTS or later recommended).
2. Ensure the **Meta XR SDK** is installed via the Package Manager.
3. Open the main scene, press **Play**.
4. Controller poses are printed to the Unity Console each frame as:
   ```
   [Android Log Simulation] wE9ryARX: l:... | r:...&...
   ```

### Deploying to Meta Quest 3 (Build and Run)

1. **Enable Developer Mode** on your Quest via the Meta mobile app.
2. In Unity: **File → Build Settings → Android → Switch Platform**.
3. Connect the headset via USB and accept the "Allow USB Debugging" prompt inside the headset.
4. Click **Build and Run** — Unity builds the APK and installs it directly on the headset.
5. Once running, read the output on a connected PC with:
   ```bash
   adb logcat -s wE9ryARX
   ```

### Inspector Controls (on the `QuestReader` component)

| Field | Description |
|---|---|
| `relativeToHead` | If true, hand poses are expressed relative to the headset; if false, absolute world space |
| `rightHandedOutput` | If true, converts matrices from Unity's left-handed frame (Y up, Z forward) to right-handed (Z up, Y forward) |
| `APP_VERSION` | Version string shown as a floating label in the headset |
| `versionLabel` | Drag the in-scene TextMeshPro object here to display the version |

### Inspector Controls (on the `ControllerAxisVisualizer` component)

| Field | Description |
|---|---|
| `showLeft` / `showRight` / `showGlobal` | Toggle each set of axes on/off |
| `axisLength` | Length of controller axis lines (meters) |
| `globalAxisLength` | Length of world-origin axis lines (meters) |
| `globalAxisOrigin` | World-space position of the global axes origin |
| `xAxisMaterial` / `yAxisMaterial` / `zAxisMaterial` | Assign three `Unlit/Color` materials (red, green, blue) created in `Assets/Materials/` |

---

## Project Structure

```
Assets/
├── QuestReader.cs                  # Main data capture and output script
├── ControllerAxisVisualizer.cs     # In-headset axis overlay
└── Materials/
    ├── Red.mat                     # X axis material (red)
    ├── Green.mat                   # Y axis material (green)
    └── Blue.mat                    # Z axis material (blue)
```

---

## Main Functions and Math

### Coordinate Frames

Unity uses a **left-handed** coordinate system:

| Axis | Direction |
|---|---|
| X | Right |
| Y | Up |
| Z | Forward |

All `OVRCameraRig` anchors (`leftControllerAnchor`, `rightControllerAnchor`, `centerEyeAnchor`) report poses in **Unity world space**, which is anchored to the headset's position at app launch (or to the floor/Guardian center depending on `OVRManager.trackingOriginType`).

### Pose Matrix Construction — `BuildOutputString()` ([QuestReader.cs:152–185](Assets/QuestReader.cs#L152))

Each controller's 4×4 affine transformation matrix is built from its world-space position and rotation:

```
M_hand = TRS(position, rotation, (1,1,1))
```

This produces a column-major matrix where the upper-left 3×3 is the rotation matrix and the last column is the translation vector:

```
[ R  | t ]
[ 0  | 1 ]
```

### Head-Relative Transform — `BuildOutputString()` ([QuestReader.cs:142–158](Assets/QuestReader.cs#L142))

When `relativeToHead = true`, each hand matrix is transformed into the head (center-eye) coordinate frame:

```
M_head = TRS(centerEye.position, centerEye.rotation, (1,1,1))
M_relative = M_head⁻¹ × M_hand
```

This matches the original C++ formula `headPoseMatrix.Inverted() * handPoseMatrix`. The result expresses the controller's pose as seen from the headset — position (0,0,0) means the controller is at the same point as the eye, and the rotation is relative to where the user is looking.

### Right-Handed Coordinate Conversion — `ConvertToRightHanded()` ([QuestReader.cs:208–222](Assets/QuestReader.cs#L208))

When `rightHandedOutput = true`, the matrix is converted from Unity's left-handed frame to a right-handed frame (X right, Y forward, Z up) via a **change of basis**:

```
M_rh = C × M_lh × C
```

where C is the permutation matrix that swaps the Y and Z axes (indices 1 and 2):

```
C = [ 1  0  0  0 ]
    [ 0  0  1  0 ]
    [ 0  1  0  0 ]
    [ 0  0  0  1 ]
```

Since C = C⁻¹ (swapping twice is identity), the operation is symmetric. In practice this swaps rows 1↔2 and columns 1↔2 simultaneously, implemented as:

```csharp
int[] idx = {0, 2, 1, 3};
result[row, col] = m[idx[row], idx[col]];
```

### Output String Format — `BuildOutputString()` / `MatrixToString()` / `ButtonsToString()` ([QuestReader.cs:133–205](Assets/QuestReader.cs#L133))

The final output string matches the original C++ wire format exactly:

```
l:<16 floats>|r:<16 floats>&L,<button data>,R,<button data>
```

- Pose section: `l:` and `r:` prefixed, separated by `|`
- Matrix: 16 space-separated floats in **row-major** order
- Button section appended after `&`, left and right separated by `,`

Example:
```
l:1 0 0 -0.3 0 1 0 1.4 0 0 1 0.2 0 0 0 1|r:...&L,LThU,leftJS 0 0,leftTrig 0,leftGrip 0,R,RThU,rightJS 0 0,rightTrig 0,rightGrip 0
```

---

## File and Function Reference

### [QuestReader.cs](Assets/QuestReader.cs)

The main MonoBehaviour. Attach to any GameObject in the scene.

| Function | Description |
|---|---|
| `Start()` | Locates `OVRCameraRig` and its anchors (`leftControllerAnchor`, `rightControllerAnchor`, `centerEyeAnchor`). Sets the version label if assigned. |
| `Update()` | Called every frame. Calls `UpdateButtonStates()`, `BuildOutputString()`, `LogToAndroid()`, and `LogDetailedDebug()`. |
| `UpdateButtonStates()` | Polls all `OVRInput` buttons and analog axes for both controllers and stores them in `leftButtons` / `rightButtons` (`ControllerButtonState`). |
| `BuildOutputString()` | Constructs the full output string. Builds each hand's matrix via `Matrix4x4.TRS`, optionally applies head-relative and/or right-handed transforms, then serialises poses and buttons. |
| `ConvertToRightHanded(Matrix4x4)` | Applies change-of-basis `C×M×C` to convert from Unity left-handed to right-handed (Z up). |
| `MatrixToString(Matrix4x4)` | Serialises a 4×4 matrix as 16 space-separated floats in row-major order. |
| `ButtonsToString(char, ControllerButtonState)` | Serialises button flags and analog values for one controller into the wire-format string. |
| `LogToAndroid(string)` | On device: writes to logcat via `android.util.Log` with tag `wE9ryARX`. In Editor: prints to Console. |
| `LogDetailedDebug(string)` | Parses the output string and prints a human-readable breakdown of position, rotation, and button states to the Console. |

### `ControllerButtonState` ([QuestReader.cs:438](Assets/QuestReader.cs#L438))

A serializable data class holding the full state of one controller:
- **Face buttons**: `A`, `B` (right), `X`, `Y` (left)
- **Digital triggers**: `TriggerButton`, `GripButton`
- **Touch**: `ThumbUp` (true when thumb is lifted off thumbrest)
- **Joystick**: `JoystickButton` (click), `JoystickVec` (Vector2, −1 to 1)
- **Analog**: `IndexTriggerValue`, `GripTriggerValue` (0 to 1)

---

### [ControllerAxisVisualizer.cs](Assets/ControllerAxisVisualizer.cs)

Draws X (red), Y (green), Z (blue) `LineRenderer` axes on each controller anchor and at the world origin. Attach to any GameObject.

| Function | Description |
|---|---|
| `Start()` | Creates `LineRenderer` GameObjects for left, right, and global axes via `CreateAxisLines()`. Finds the `QuestReader` component to read `rightHandedOutput`. |
| `Update()` | Each frame: toggles visibility and calls `UpdateAxisLines()` or `UpdateGlobalAxisLines()`. |
| `CreateAxisLines(string, float)` | Instantiates three child GameObjects each with a `LineRenderer`, assigned the X/Y/Z serialized materials. |
| `UpdateAxisLines(LineRenderer[], Transform)` | Sets each line's start/end positions from the anchor's `right`, `up`, and `forward` vectors. When `rightHandedOutput` is true, swaps Y↔Z directions to match the output frame. |
| `UpdateGlobalAxisLines(LineRenderer[])` | Same as above but uses `Vector3.right`, `Vector3.up`, `Vector3.forward` (world axes). Also respects `rightHandedOutput`. |