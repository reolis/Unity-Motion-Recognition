using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public class MathAdd
    {
        float maxDistance = 0.2f;

        public static Vector3 ClampPosition(Vector3 newPos, Vector3 basePos, float maxDist)
        {
            Vector3 offset = newPos - basePos;
            if (offset.magnitude > maxDist)
                return basePos + offset.normalized * maxDist;
            return newPos;
        }

        public static Quaternion ClampRotation(Quaternion rot, Vector3 minEuler, Vector3 maxEuler)
        {
            Vector3 euler = rot.eulerAngles;

            // Приведение углов к диапазону [-180, 180]
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);
            euler.z = NormalizeAngle(euler.z);

            euler.x = Mathf.Clamp(euler.x, minEuler.x, maxEuler.x);
            euler.y = Mathf.Clamp(euler.y, minEuler.y, maxEuler.y);
            euler.z = Mathf.Clamp(euler.z, minEuler.z, maxEuler.z);

            return Quaternion.Euler(euler);
        }

        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        public static Vector3 TranslationMatrix(float x0, float y0, float z0, float x, float y, float z)
        {
            Vector3 translation = new Vector3(x, y, z);

            Matrix4x4 translationMatrix = Matrix4x4.Translate(translation);

            Vector3 originalPoint = new Vector3(x0, y0, z0);
            Vector3 translatedPoint = translationMatrix.MultiplyPoint3x4(originalPoint);

            return translatedPoint;
        }
    }
}