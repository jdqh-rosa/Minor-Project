using UnityEngine;
using System.Collections;
using System.Collections.Generic;

static class RadialHelper
{
    public static float CalculateCircumference(float pRadius)
    {
        return (2 * Mathf.PI * pRadius);
    }

    public static float CalculateCircumferenceDeg(float pRadius)
    {
        return 360 / CalculateCircumference(pRadius);
    }

    public static Vector2 PolarToCart(float pDeg, float pRadius)
    {
        float _x = pRadius * Mathf.Cos(pDeg * Mathf.Deg2Rad);
        float _y = pRadius * Mathf.Sin(pDeg * Mathf.Deg2Rad);
        return new Vector2(_x, _y);
    }

    public static Vector2 CartesianToPol(float pX, float pY)
    {
        float radius = Mathf.Sqrt(pX * pX + pY * pY);
        float angle = Mathf.Atan2(pY, pX) * Mathf.Rad2Deg;
        return new Vector2(radius, angle);
    }
    
    public static Vector2 CartesianToPol(Vector2 pCartesian)
    {
        float _radius = Mathf.Sqrt(pCartesian.x * pCartesian.x + pCartesian.y * pCartesian.y);
        float _angle = Mathf.Atan2(pCartesian.y, pCartesian.x) * Mathf.Rad2Deg;
        return new Vector2(_radius, _angle);
    }

    public static Vector2 OrbitPoint(Vector2 pivot, Vector2 point, float angle)
    {
        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
        float cos = Mathf.Cos(angle * Mathf.Deg2Rad);

        point.x -= pivot.x;
        point.y -= pivot.y;

        float xnew = point.x * cos - point.y * sin;
        float ynew = point.x * sin + point.y * cos;

        point.x = xnew + pivot.x;
        point.y = ynew + pivot.y;
        return point;
    }
    public static float NormalizeAngle(float a) => (a + 360) % 360f;
}
