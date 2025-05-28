using UnityEngine;

static class MiscHelper
{
    public static Vector3 Vec2ToVec3Pos(Vector2 vec2)
    {
        return new Vector3(vec2.x, 0, vec2.y);
    }
    
    public static Vector2 Vec3ToVec2Pos(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public static Vector3 DifferenceVector(Vector3 vecA, Vector3 vecB) {
        return vecA - vecB;
        
    }
    
    public static Vector2 DifferenceVector(Vector2 vecA, Vector2 vecB) {
        return vecA - vecB;
    }
    
}

