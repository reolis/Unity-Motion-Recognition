using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static Detection;

namespace Diplom1
{
    internal class FingersDetection
    {
        private Dictionary<(string hand, int jointId), Vector3> fingerJointsPositions = new Dictionary<(string hand, int jointId), Vector3>();

        float GetBendAngle(Vector3 joint1, Vector3 joint2, Vector3 joint3)
        {
            Vector3 v1 = (joint2 - joint1);
            Vector3 v2 = (joint3 - joint2);
            return Vector3.Distance(v1, v2);
        }

        Vector3 ToUnityCoords(Landmark lm0, float scale = 1.8f)
        {
            return new Vector3(
                lm0.x * scale,
                lm0.y * scale,
                lm0.z * scale
            );
        }

        public string BuildFingerDataMessage(List<Landmark> landmarks, string side)
        {
            StringBuilder msg = new StringBuilder();

            string[] fingers = { "thumb", "index", "middle", "ring", "pinky" };

            Dictionary<string, (int mcp, int pip, int dip, int tip)> fingerJoints = new Dictionary<string, (int, int, int, int)>
            {
                { "thumb", (1, 2, 3, 4) },
                { "index", (5, 6, 7, 8) },
                { "middle", (9, 10, 11, 12) },
                { "ring", (13, 14, 15, 16) },
                { "pinky", (17, 18, 19, 20) }
            };

            foreach (var finger in fingers)
            {
                var (mcpId, pipId, dipId, tipId) = fingerJoints[finger];

                SetFingerJointPosition(side.ToLower(), mcpId, ToUnityCoords(landmarks[mcpId]));
                SetFingerJointPosition(side.ToLower(), pipId, ToUnityCoords(landmarks[pipId]));
                SetFingerJointPosition(side.ToLower(), dipId, ToUnityCoords(landmarks[dipId]));
                SetFingerJointPosition(side.ToLower(), tipId, ToUnityCoords(landmarks[tipId]));

                var rotations = GetFingerRotations(side.ToLower(), mcpId, pipId, dipId, tipId);

                AddBone(msg, $"B-{finger}1.{side}", landmarks[mcpId], landmarks[pipId]);
                AddBone(msg, $"B-{finger}2.{side}", landmarks[pipId], landmarks[dipId]);
                AddBone(msg, $"B-{finger}3.{side}", landmarks[dipId], landmarks[tipId]);
            }

            return msg.ToString();
        }

        void AddBone(StringBuilder msg, string boneName, Landmark from, Landmark to, float scale = 1.8f)
        {
            Vector3 pos = new Vector3(to.x * scale, to.y * scale, to.z * scale);
            msg.AppendFormat("{0}:{1:F3}={2:F3}={3:F3};", boneName, pos.X, pos.Y, pos.Z);
        }

        public Matrix4x4[] GetFingerRotations(string hand, int mcpId, int pipId, int dipId, int tipId)
        {
            if (!TryGetFingerJointPosition(hand, mcpId, out Vector3 MCP)) return null;
            if (!TryGetFingerJointPosition(hand, pipId, out Vector3 PIP)) return null;
            if (!TryGetFingerJointPosition(hand, dipId, out Vector3 DIP)) return null;
            if (!TryGetFingerJointPosition(hand, tipId, out Vector3 TIP)) return null;

            Matrix4x4 rot1 = RotateBone(MCP, PIP);
            Matrix4x4 rot2 = RotateBone(PIP, DIP);
            Matrix4x4 rot3 = RotateBone(DIP, TIP);

            return new[] { rot1, rot2, rot3 };
        }

        public static Matrix4x4 RotateBone(Vector3 from, Vector3 to)
        {
            if (Vector3.Distance(from, to) < 1e-5f)
                return Matrix4x4.Identity;

            Vector3 forward = Vector3.Normalize(to - from);

            Vector3 up = Vector3.UnitY;
            if (Vector3.Cross(forward, up).LengthSquared() < 1e-3f)
                up = Vector3.UnitX;

            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            Vector3 trueUp = Vector3.Normalize(Vector3.Cross(forward, right));

            return new Matrix4x4(
                right.X, trueUp.X, forward.X, 0,
                right.Y, trueUp.Y, forward.Y, 0,
                right.Z, trueUp.Z, forward.Z, 0,
                0, 0, 0, 1
            );
        }

        public void SetFingerJointPosition(string hand, int jointId, Vector3 position)
        {
            var key = (hand.ToLower(), jointId);
            fingerJointsPositions[key] = position;
        }

        public bool TryGetFingerJointPosition(string hand, int jointId, out Vector3 position)
        {
            return fingerJointsPositions.TryGetValue((hand.ToLower(), jointId), out position);
        }
    }
}
