using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class TestReader2 : MonoBehaviour
{
    private InputDevice leftController;
    private InputDevice rightController;

    void Start()
    {
        GetControllers();
    }

    void GetControllers()
    {
        var leftHandedControllers = new List<InputDevice>();
        var rightHandedControllers = new List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandedControllers);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandedControllers);

        if (leftHandedControllers.Count > 0)
            leftController = leftHandedControllers[0];
        if (rightHandedControllers.Count > 0)
            rightController = rightHandedControllers[0];
    }

    void Update()
    {
        if (!leftController.isValid || !rightController.isValid)
            GetControllers();

        if (leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos) &&
            leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRot))
        {
            Debug.Log($"Left Controller - Position: {leftPos}, Rotation: {leftRot.eulerAngles}");
        }

        if (rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos) &&
            rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRot))
        {
            Debug.Log($"Right Controller - Position: {rightPos}, Rotation: {rightRot.eulerAngles}");
        }
    }
}
