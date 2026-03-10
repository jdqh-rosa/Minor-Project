using UnityEngine;

public class DrawCircleLine : MonoBehaviour
{
    [SerializeField] private int segments = 50;
    [SerializeField] private float radius = 1f;
    private void Start() {
        
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = false;

        for (int i = 0; i <= segments; i++) {
            float angle = i * Mathf.PI * 2 / segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    
}

