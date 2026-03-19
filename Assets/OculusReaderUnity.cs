using UnityEngine;
using System.Text;
using System.Collections;

/// <summary>
/// Main controller reader that matches the original oculus_reader protocol
/// This replicates the functionality from OculusTeleop.cpp
/// 
/// Key Output Format (matches C++ exactly):
/// "l:matrix|r:matrix&buttonData"
/// Matrix: space-separated 16 floats (4x4 row-major)
/// Buttons: comma-separated flags and values
/// </summary>
public class OculusReaderUnity : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showDetailedDebug = false;
    [SerializeField] private float debugLogInterval = 1.0f; // Log every N seconds
    
    [Header("Controller Settings")]
    // [SerializeField] private bool disableHaptics = true; // Disable haptics to avoid warnings
    private string class_name = "OculusReaderUnity: ";
    private float debugTimer = 0f;
    private bool controllersInitialized = false;
    
    // This matches the original log tag "wE9ryARX" from the C++ code
    private const string LOG_TAG = "wE9ryARX";
    
    // OVR components - will be found automatically
    private OVRCameraRig cameraRig;
    private Transform leftHandAnchor;
    private Transform rightHandAnchor;
    private Transform centerEyeAnchor;
    
    // Button tracking
    private ControllerButtonState leftButtons = new ControllerButtonState();
    private ControllerButtonState rightButtons = new ControllerButtonState();
    
    void Start()
    {
        // Find the OVRCameraRig in the scene
        cameraRig = FindFirstObjectByType<OVRCameraRig>();
        
        if (cameraRig == null)
        {
            Debug.LogError("OVRCameraRig not found! Please add OVRCameraRig prefab to your scene.");
            enabled = false;
            return;
        }
        
        // Get the tracking anchors
        leftHandAnchor = cameraRig.leftHandAnchor;
        rightHandAnchor = cameraRig.rightHandAnchor;
        centerEyeAnchor = cameraRig.centerEyeAnchor;
        
        // // Disable haptics if requested to avoid "SampleRateHz is 0" warnings
        // if (disableHaptics)
        // {
        //     OVRHaptics.Config.SampleRateHz = 0;
        //     Debug.Log("OculusReaderUnity: Haptics disabled");
        // }
        
        // Debug.Log("OculusReaderUnity initialized successfully");
        
        // Wait a frame for controllers to initialize
        // StartCoroutine(InitializeControllersDelayed());

        // WaitForSeconds(0.5f);
        
        // Check if controllers are connected
        var connectedControllers = OVRInput.GetConnectedControllers();
        Debug.Log(class_name + $"Connected controllers: {connectedControllers}");
        
        controllersInitialized = true;
    }
    
    // private System.Collections.IEnumerator InitializeControllersDelayed()
    // {
    //     // Wait a few frames for OVR system to fully initialize
    //     yield return new WaitForSeconds(0.5f);
        
    //     // Check if controllers are connected
    //     var connectedControllers = OVRInput.GetConnectedControllers();
    //     Debug.Log($"Connected controllers: {connectedControllers}");
        
    //     controllersInitialized = true;
    // }
    
    void Update()
    {
        if (cameraRig == null || !controllersInitialized) return;
        
        // Update button states
        UpdateButtonStates();
        
        // Build and send the pose + button data string
        // Format matches original: "l:matrix|r:matrix&buttonData"
        string outputString = BuildOutputString();
        
        // Only log if we have valid data
        if (!string.IsNullOrEmpty(outputString) && outputString.Contains(":"))
        {
            // Log using Android logcat (this is what the Python script reads)
            LogToAndroid(outputString);
            
            // Unity Debug Logging
            if (enableDebugLog)
            {
                debugTimer += Time.deltaTime;
                // Debug.Log($"{debugTimer:F3}");
                if (debugTimer >= debugLogInterval)
                {
                    debugTimer = 0f;
                    
                    if (showDetailedDebug)
                    {
                        LogDetailedDebug(outputString);
                    }
                    else
                    {
                        Debug.Log($"[{LOG_TAG}] {outputString}");
                    }
                }
            }
        }
    }
    
    private void UpdateButtonStates()
    {
        // Left controller buttons
        // Note: OVRInput.Button.One on left = X button, Button.Two = Y button
        leftButtons.X = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
        leftButtons.Y = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
        leftButtons.TriggerButton = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        leftButtons.GripButton = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        leftButtons.ThumbUp = !OVRInput.Get(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch);
        leftButtons.JoystickButton = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        leftButtons.JoystickVec = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        leftButtons.IndexTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        leftButtons.GripTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        
        // Right controller buttons
        // Note: OVRInput.Button.One on right = A button, Button.Two = B button
        rightButtons.A = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
        rightButtons.B = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
        rightButtons.TriggerButton = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        rightButtons.GripButton = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        rightButtons.ThumbUp = !OVRInput.Get(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch);
        rightButtons.JoystickButton = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
        rightButtons.JoystickVec = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        rightButtons.IndexTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        rightButtons.GripTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
    }
    
    private string BuildOutputString()
    {
        StringBuilder output = new StringBuilder();
        StringBuilder buttons = new StringBuilder();
        
        bool first = true;
        
        // Get head pose for coordinate transformation
        // This matches C++ code: headPoseMatrix.Inverted() * handPoseMatrix
        Matrix4x4 headPoseMatrix = Matrix4x4.TRS(
            centerEyeAnchor.position,
            centerEyeAnchor.rotation,
            Vector3.one
        );
        Matrix4x4 headPoseInv = headPoseMatrix.inverse;
        
        // Left hand - check if controller is connected/tracked
        if (OVRInput.GetConnectedControllers().ToString().Contains("LTouch") && leftHandAnchor != null)
        {
            Matrix4x4 leftHandMatrix = Matrix4x4.TRS(
                leftHandAnchor.position,
                leftHandAnchor.rotation,
                Vector3.one
            );
            
            // Transform to head coordinate system (matches C++ code)
            Matrix4x4 leftHandHeadCoord = headPoseInv * leftHandMatrix;
            
            if (!first)
            {
                output.Append('|');
                buttons.Append(',');
            }
            
            output.Append('l');
            output.Append(':');
            output.Append(MatrixToString(leftHandHeadCoord));
            buttons.Append(ButtonsToString('l', leftButtons));
            
            first = false;
        }
        
        // Right hand - check if controller is connected/tracked
        if (OVRInput.GetConnectedControllers().ToString().Contains("RTouch") && rightHandAnchor != null)
        {
            Matrix4x4 rightHandMatrix = Matrix4x4.TRS(
                rightHandAnchor.position,
                rightHandAnchor.rotation,
                Vector3.one
            );
            
            // Transform to head coordinate system
            Matrix4x4 rightHandHeadCoord = headPoseInv * rightHandMatrix;
            
            if (!first)
            {
                output.Append('|');
                buttons.Append(',');
            }
            
            output.Append('r');
            output.Append(':');
            output.Append(MatrixToString(rightHandHeadCoord));
            buttons.Append(ButtonsToString('r', rightButtons));
            
            first = false;
        }
        
        // Combine pose and button data with '&' separator
        output.Append('&');
        output.Append(buttons.ToString());
        
        return output.ToString();
    }
    
    private string MatrixToString(Matrix4x4 matrix)
    {
        // Format matches the C++ Matrix4f::ToString() output
        // Space-separated values, row-major order
        // C++ outputs: m[0][0] m[0][1] m[0][2] m[0][3] m[1][0] ...
        StringBuilder sb = new StringBuilder();
        
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                sb.Append(matrix[row, col]);
                sb.Append(' ');
            }
        }
        
        // Remove trailing space
        if (sb.Length > 0)
            sb.Length--;
        
        return sb.ToString();
    }
    
    private string ButtonsToString(char side, ControllerButtonState buttons)
    {
        // Matches EXACT format from Buttons.cpp in original code
        StringBuilder text = new StringBuilder();
        
        if (side == 'r')
        {
            // Right hand format from C++
            text.Append("R,");
            if (buttons.A) text.Append("A,");
            if (buttons.B) text.Append("B,");
            if (buttons.TriggerButton) text.Append("RTr,");
            if (buttons.GripButton) text.Append("RG,");
            if (buttons.ThumbUp) text.Append("RThU,");
            if (buttons.JoystickButton) text.Append("RJ,");
            text.Append("rightJS ");
            text.Append(buttons.JoystickVec.x);
            text.Append(' ');
            text.Append(buttons.JoystickVec.y);
            text.Append(',');
            text.Append("rightTrig ");
            text.Append(buttons.IndexTriggerValue);
            text.Append(',');
            text.Append("rightGrip ");
            text.Append(buttons.GripTriggerValue);
        }
        else if (side == 'l')
        {
            // Left hand format from C++
            text.Append("L,");
            if (buttons.X) text.Append("X,");
            if (buttons.Y) text.Append("Y,");
            if (buttons.TriggerButton) text.Append("LTr,");
            if (buttons.GripButton) text.Append("LG,");
            if (buttons.ThumbUp) text.Append("LThU,");
            if (buttons.JoystickButton) text.Append("LJ,");
            text.Append("leftJS ");
            text.Append(buttons.JoystickVec.x);
            text.Append(' ');
            text.Append(buttons.JoystickVec.y);
            text.Append(',');
            text.Append("leftTrig ");
            text.Append(buttons.IndexTriggerValue);
            text.Append(',');
            text.Append("leftGrip ");
            text.Append(buttons.GripTriggerValue);
        }
        
        return text.ToString();
    }
    
    private void LogToAndroid(string message)
    {
        // Use AndroidJavaClass to write to logcat with the exact same tag
        // This is what the Python script reads via adb logcat
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass log = new AndroidJavaClass("android.util.Log"))
            {
                log.CallStatic<int>("i", LOG_TAG, message);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to log to Android: {e.Message}");
        }
#endif
    }
    
    private void LogDetailedDebug(string outputString)
    {
        StringBuilder debugLog = new StringBuilder();
        debugLog.AppendLine("=== Oculus Reader Debug ===");
        
        // Parse the output string
        string[] mainParts = outputString.Split('&');
        if (mainParts.Length < 2)
        {
            Debug.LogWarning("Invalid output format");
            return;
        }
        
        string posePart = mainParts[0];
        string buttonPart = mainParts[1];
        
        // Parse poses
        string[] poses = posePart.Split('|');
        foreach (string pose in poses)
        {
            if (string.IsNullOrEmpty(pose)) continue;
            
            string[] poseData = pose.Split(':');
            if (poseData.Length < 2) continue;
            
            char side = poseData[0][0];
            string sideLabel = side == 'l' ? "LEFT HAND" : "RIGHT HAND";
            
            debugLog.AppendLine($"\n{sideLabel}:");
            
            // Parse matrix to show position
            string[] matrixValues = poseData[1].Trim().Split(' ');
            if (matrixValues.Length >= 16)
            {
                // Position is in last column: m[0,3], m[1,3], m[2,3]
                float posX = float.Parse(matrixValues[3]);
                float posY = float.Parse(matrixValues[7]);
                float posZ = float.Parse(matrixValues[11]);
                
                debugLog.AppendLine($"  Position (head-relative): ({posX:F3}, {posY:F3}, {posZ:F3})");
                
                // Show rotation (first 3x3 of matrix)
                debugLog.AppendLine("  Rotation Matrix (3x3):");
                debugLog.AppendLine($"    [{matrixValues[0]}, {matrixValues[1]}, {matrixValues[2]}]");
                debugLog.AppendLine($"    [{matrixValues[4]}, {matrixValues[5]}, {matrixValues[6]}]");
                debugLog.AppendLine($"    [{matrixValues[8]}, {matrixValues[9]}, {matrixValues[10]}]");
            }
        }
        
        // Parse buttons
        debugLog.AppendLine("\nBUTTONS:");
        string[] buttonGroups = buttonPart.Split(',');
        
        string currentHand = "";
        foreach (string buttonInfo in buttonGroups)
        {
            string trimmed = buttonInfo.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            
            if (trimmed == "L")
            {
                currentHand = "LEFT";
                debugLog.AppendLine($"\n  {currentHand} Controller:");
            }
            else if (trimmed == "R")
            {
                currentHand = "RIGHT";
                debugLog.AppendLine($"\n  {currentHand} Controller:");
            }
            else if (trimmed.StartsWith("leftJS") || trimmed.StartsWith("rightJS"))
            {
                string[] parts = trimmed.Split(' ');
                if (parts.Length >= 3)
                {
                    debugLog.AppendLine($"    Joystick: ({parts[1]}, {parts[2]})");
                }
            }
            else if (trimmed.StartsWith("leftTrig") || trimmed.StartsWith("rightTrig"))
            {
                string[] parts = trimmed.Split(' ');
                if (parts.Length >= 2)
                {
                    debugLog.AppendLine($"    Trigger (analog): {parts[1]}");
                }
            }
            else if (trimmed.StartsWith("leftGrip") || trimmed.StartsWith("rightGrip"))
            {
                string[] parts = trimmed.Split(' ');
                if (parts.Length >= 2)
                {
                    debugLog.AppendLine($"    Grip (analog): {parts[1]}");
                }
            }
            else
            {
                // Button flags
                debugLog.AppendLine($"    {trimmed} pressed");
            }
        }
        
        debugLog.AppendLine("\n=== Raw Output ===");
        debugLog.AppendLine(outputString);
        debugLog.AppendLine("==================");
        
        Debug.Log(debugLog.ToString());
    }
}

/// <summary>
/// Stores button state for a controller
/// Matches the data structure from Buttons.cpp
/// </summary>
[System.Serializable]
public class ControllerButtonState
{
    // Face buttons
    public bool A;  // Right controller A button (or left X)
    public bool B;  // Right controller B button (or left Y)
    public bool X;  // Left controller X button
    public bool Y;  // Left controller Y button
    
    // Triggers and grips (boolean states)
    public bool TriggerButton;  // Index trigger pressed (> threshold)
    public bool GripButton;     // Grip trigger pressed (> threshold)
    
    // Touch sensors
    public bool ThumbUp;        // Thumb lifted from thumbrest
    
    // Joystick
    public bool JoystickButton; // Joystick clicked
    public Vector2 JoystickVec; // Joystick position (-1 to 1)
    
    // Analog values (0 to 1)
    public float IndexTriggerValue; // Index trigger analog
    public float GripTriggerValue;  // Grip trigger analog
}