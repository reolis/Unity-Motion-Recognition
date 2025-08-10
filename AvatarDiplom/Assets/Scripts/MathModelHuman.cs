using UnityEngine;

namespace Assets.Scripts
{
    public class MathModelHuman
    {
        public float m1 = 2f, m2 = 1f; // массы
        public float L1 = 0.5f, L2 = 0.4f; // длины сегментов
        public float I1 = 0.02f, I2 = 0.01f; // моменты инерции

        private Vector2 theta;      // углы суставов (радианы)
        private Vector2 thetaDot;   // угловые скорости

        public Vector2 muscleTorque; // управляющие моменты (управление мышцами)

        private float gravity = 9.81f;

        public MathModelHuman()
        {
            // Начальные углы
            theta = new Vector2(Mathf.PI / 4, Mathf.PI / 4);
            thetaDot = Vector2.zero;
            muscleTorque = Vector2.zero;
        }

        public void Update(float dt)
        {
            Vector2 acceleration = ComputeAcceleration(theta, thetaDot, muscleTorque);

            thetaDot += acceleration * dt;
            theta += thetaDot * dt;

            // Ограничения углов
            theta.x = Mathf.Clamp(theta.x, 0f, Mathf.PI / 2);  // плечо 0-90 градусов
            theta.y = Mathf.Clamp(theta.y, 0f, Mathf.PI);      // локоть 0-180 градусов
        }

        public Vector2 GetJointAngles()
        {
            return theta;
        }

        private Vector2 ComputeAcceleration(Vector2 th, Vector2 thDot, Vector2 tau)
        {
            float c2 = Mathf.Cos(th.y);
            float s2 = Mathf.Sin(th.y);

            float m11 = I1 + I2 + m2 * L1 * L1 + 2 * m2 * L1 * (L2 / 2) * c2;
            float m12 = I2 + m2 * L1 * (L2 / 2) * c2;
            float m21 = m12;
            float m22 = I2;

            Matrix2x2 M = new Matrix2x2(m11, m12, m21, m22);

            float h = -m2 * L1 * (L2 / 2) * s2;
            float c1 = h * thDot.y * (2 * thDot.x + thDot.y);
            float c2_val = h * thDot.x * thDot.x;

            Vector2 C = new Vector2(c1, c2_val);

            float g1 = (m1 * (L1 / 2) + m2 * L1) * gravity * Mathf.Cos(th.x) + m2 * (L2 / 2) * gravity * Mathf.Cos(th.x + th.y);
            float g2 = m2 * (L2 / 2) * gravity * Mathf.Cos(th.x + th.y);

            Vector2 G = new Vector2(g1, g2);

            Vector2 rhs = tau - C - G;

            Vector2 thetaDD = M.Inverse() * rhs;

            return thetaDD;
        }
    }

    public struct Matrix2x2
    {
        public float m00, m01, m10, m11;

        public Matrix2x2(float _m00, float _m01, float _m10, float _m11)
        {
            m00 = _m00; m01 = _m01; m10 = _m10; m11 = _m11;
        }

        public Vector2 Inverse()
        {
            float det = m00 * m11 - m01 * m10;
            if (Mathf.Abs(det) < 1e-6f) return Vector2.zero;

            float invDet = 1f / det;

            return new Vector2(
                m11 * invDet,
                -m10 * invDet
            );
        }

        public static Vector2 operator *(Matrix2x2 m, Vector2 v)
        {
            return new Vector2(m.m00 * v.x + m.m01 * v.y, m.m10 * v.x + m.m11 * v.y);
        }
    }
}