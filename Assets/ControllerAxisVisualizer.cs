using UnityEngine;

/// <summary>
/// Draws X (red), Y (green), Z (blue) axis lines on each controller anchor.
/// Attach to any GameObject in the scene and assign the controller anchors.
/// </summary>
public class ControllerAxisVisualizer : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;

    [Range(0.01f, 0.5f)]
    public float axisLength = 0.1f;

    [Range(0.0005f, 0.01f)]
    public float lineWidth = 0.002f;

    public bool showLeft = true;
    public bool showRight = true;

    [Tooltip("Unlit/Color material with red color — assigned in Inspector")]
    public Material xAxisMaterial;
    [Tooltip("Unlit/Color material with green color — assigned in Inspector")]
    public Material yAxisMaterial;
    [Tooltip("Unlit/Color material with blue color — assigned in Inspector")]
    public Material zAxisMaterial;

    private LineRenderer[] leftLines;
    private LineRenderer[] rightLines;

    void Start()
    {
        leftLines = CreateAxisLines("LeftAxis");
        rightLines = CreateAxisLines("RightAxis");
    }

    void Update()
    {
        if (leftHandAnchor != null)
        {
            SetActive(leftLines, showLeft);
            if (showLeft)
                UpdateAxisLines(leftLines, leftHandAnchor);
        }

        if (rightHandAnchor != null)
        {
            SetActive(rightLines, showRight);
            if (showRight)
                UpdateAxisLines(rightLines, rightHandAnchor);
        }
    }

    private LineRenderer[] CreateAxisLines(string label)
    {
        Material[] materials = {xAxisMaterial, yAxisMaterial, zAxisMaterial};
        string[] axes = {"X", "Y", "Z"};
        LineRenderer[] lines = new LineRenderer[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject($"{label}_{axes[i]}");
            go.transform.SetParent(transform);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.sharedMaterial = materials[i];

            lines[i] = lr;
        }

        return lines;
    }

    private void UpdateAxisLines(LineRenderer[] lines, Transform anchor)
    {
        Vector3 origin = anchor.position;

        // X axis - red
        lines[0].SetPosition(0, origin);
        lines[0].SetPosition(1, origin + anchor.right * axisLength);

        // Y axis - green
        lines[1].SetPosition(0, origin);
        lines[1].SetPosition(1, origin + anchor.up * axisLength);

        // Z axis - blue
        lines[2].SetPosition(0, origin);
        lines[2].SetPosition(1, origin + anchor.forward * axisLength);
    }

    private void SetActive(LineRenderer[] lines, bool active)
    {
        foreach (var lr in lines)
            lr.gameObject.SetActive(active);
    }
}
