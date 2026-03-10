using System;
using Unity.VisualScripting;
using UnityEngine;

public class DrawLines : MonoBehaviour
{
    [SerializeField] public static LineRenderer lineRenderer;

    private void Start() {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color) {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}