using UnityEngine;

public class ChalkExampleUsage : MonoBehaviour
{
    public ChalkLine targetLine;

    [ContextMenu("Draw Sample Polyline")]
    public void DrawSamplePolyline()
    {
        if (targetLine == null)
            return;

        Vector3[] points =
        {
            new Vector3(-2.4f, 0.2f, 0f),
            new Vector3(-1.1f, 0.5f, 0f),
            new Vector3(0.5f, 0.1f, 0f),
            new Vector3(2.1f, 0.4f, 0f)
        };

        targetLine.SetPoints(points, false);
        targetLine.Apply();
    }
}

