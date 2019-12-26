using UnityEngine;

public static class Util
{
    public static Vector2 ToPolar(Vector3 v)
    {
        return new Vector2(
            Vector3.SignedAngle(Vector3.ProjectOnPlane(v, Vector3.up), Vector3.forward, Vector3.up),
            Vector3.SignedAngle(Vector3.ProjectOnPlane(v, Vector3.right), Vector3.forward, Vector3.right));
    }

    public static float Sigmoid(float v)
    {
        return v / (1f + Mathf.Abs(v));
    }

    public static Vector3 Sigmoid(Vector3 v)
    {
        v.x = Sigmoid(v.x);
        v.y = Sigmoid(v.y);
        v.z = Sigmoid(v.z);
        return v;
    }

    public static float PowInt(float val, int exp)
    {
        float result = 1f;
        while (exp > 0)
        {
            if (exp % 2 == 1)
            {
                result *= val;
            }
            exp >>= 1;
            val *= val;
        }
        return result;
    }
}
