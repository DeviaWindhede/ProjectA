using UnityEngine;

public class HelperFunctions {
    public static void log(params System.Object[] arguments)
    {
        string finalString = string.Empty;
        for (int i = 0; i < arguments.Length; i++)
        {
            finalString += arguments[i];
            if (i != arguments.Length - 1)
                finalString += " , ";
        }
        Debug.Log(finalString);
    }
    public static bool ShouldSlide(RaycastHit groundHitInfo, Vector3 forward) {
        var left = Vector3.Cross(groundHitInfo.normal, Vector3.up);
        var downhill = Vector3.Cross(groundHitInfo.normal, left);
        var dot = Vector3.Dot(downhill, forward);
        return dot >= -0.2f;
    }

    public static bool IsSurfaceClimbable(Vector3 vec1, Vector3 vec2, float _maxClimbableSlopeAngle) {
        float angle = Vector3.Angle(vec1, vec2);
        return angle < _maxClimbableSlopeAngle;
    }

    public static float GetNegativeAngle(Vector3 vectorA, Vector3 vectorB) {
        float angle = Vector3.Angle(vectorA, vectorB);
        Vector3 cross = Vector3.Cross(vectorA, vectorB);
        if (cross.y < 0)
            angle = -angle;
        return angle;
    }
}