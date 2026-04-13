using UnityEngine;

/// <summary>
/// Draws X (red), Y (green), Z (blue) axis lines on each controller anchor.
/// Attach to any GameObject in the scene and assign the controller anchors.
/// </summary>
public class ControllerAxisVisualizer : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;

    public bool showLeft = true;
    public bool showRight = true;
    public bool showGlobal = true;


    [Range(0.01f, 0.5f)]
    public float axisLength = 0.1f;

    [Range(0.0005f, 0.01f)]
    public float localLineWidth = 0.002f;
    [Tooltip("Origin point for the global axes in world space")]
    public Vector3 globalAxisOrigin = Vector3.zero;

    [Range(0.01f, 2.0f)]
    public float globalAxisLength = 0.5f;
    [Range(0.0005f, 0.01f)]
    public float globalLineWidth = 0.004f;

    [Tooltip("Unlit/Color material with red color — assigned in Inspector")]
    public Material xAxisMaterial;
    [Tooltip("Unlit/Color material with green color — assigned in Inspector")]
    public Material yAxisMaterial;
    [Tooltip("Unlit/Color material with blue color — assigned in Inspector")]
    public Material zAxisMaterial;

    private LineRenderer[] leftLines;
    private LineRenderer[] rightLines;
    private LineRenderer[] globalLines;

    private TestReader testReader;

    void Start()
    {
        leftLines = CreateAxisLines("LeftAxis", localLineWidth);
        rightLines = CreateAxisLines("RightAxis", localLineWidth);
        globalLines = CreateAxisLines("GlobalAxis", globalLineWidth);
        testReader = FindFirstObjectByType<TestReader>();
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

        SetActive(globalLines, showGlobal);
        if (showGlobal)
            UpdateGlobalAxisLines(globalLines);
    }

    private LineRenderer[] CreateAxisLines(string label, float width = 0.002f)
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
            lr.startWidth = width;
            lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.sharedMaterial = materials[i];

            lines[i] = lr;
        }

        return lines;
    }

    private void UpdateAxisLines(LineRenderer[] lines, Transform anchor)
    {
        bool rh = testReader != null && testReader.rightHandedOutput;
        Vector3 origin = anchor.position;

        // X axis - red (unchanged in both frames)
        lines[0].SetPosition(0, origin);
        lines[0].SetPosition(1, origin + anchor.right * axisLength);

        // Y axis - green: up in Unity LH, forward in RH
        lines[1].SetPosition(0, origin);
        lines[1].SetPosition(1, origin + (rh ? anchor.forward : anchor.up) * axisLength);

        // Z axis - blue: forward in Unity LH, up in RH
        lines[2].SetPosition(0, origin);
        lines[2].SetPosition(1, origin + (rh ? anchor.up : anchor.forward) * axisLength);
    }

    private void UpdateGlobalAxisLines(LineRenderer[] lines)
    {
        bool rh = testReader != null && testReader.rightHandedOutput;

        // X axis - red (unchanged in both frames)
        lines[0].SetPosition(0, globalAxisOrigin);
        lines[0].SetPosition(1, globalAxisOrigin + Vector3.right * globalAxisLength);

        // Y axis - green: up in Unity LH, forward in RH
        lines[1].SetPosition(0, globalAxisOrigin);
        lines[1].SetPosition(1, globalAxisOrigin + (rh ? Vector3.forward : Vector3.up) * globalAxisLength);

        // Z axis - blue: forward in Unity LH, up in RH
        lines[2].SetPosition(0, globalAxisOrigin);
        lines[2].SetPosition(1, globalAxisOrigin + (rh ? Vector3.up : Vector3.forward) * globalAxisLength);
    }

    private void SetActive(LineRenderer[] lines, bool active)
    {
        foreach (var lr in lines)
            lr.gameObject.SetActive(active);
    }
}
