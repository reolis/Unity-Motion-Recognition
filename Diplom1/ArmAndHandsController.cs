using System.Numerics;
using System.Collections.Generic;

namespace Diplom1
{
    internal class ArmAndHandsController
    {
        public Dictionary<int, Vector3> rightHandBones = new Dictionary<int, Vector3>();
        public Dictionary<int, Vector3> leftHandBones = new Dictionary<int, Vector3>();

        private Dictionary<int, Vector3> rightLastHandBones = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> leftLastHandBones = new Dictionary<int, Vector3>();

        private Vector3 rightArmRotation;
        private Vector3 leftArmRotation;

        public void SetRightHandBonePosition(int boneId, Vector3 position)
        {
            rightHandBones[boneId] = position;
        }

        public Vector3 GetRightHandBonePosition(int boneId)
        {
            if (rightHandBones.TryGetValue(boneId, out var pos))
                return pos;
            return Vector3.Zero;
        }

        public void SetLeftHandBonePosition(int boneId, Vector3 position)
        {
            leftHandBones[boneId] = position;
        }

        public Vector3 GetLeftHandBonePosition(int boneId)
        {
            if (leftHandBones.TryGetValue(boneId, out var pos))
                return pos;
            return Vector3.Zero;
        }

        public void SetRightArmRotation(Vector3 rotation) => rightArmRotation = rotation;
        public Vector3 GetRightArmRotation() => rightArmRotation;

        public void SetLeftArmRotation(Vector3 rotation) => leftArmRotation = rotation;
        public Vector3 GetLeftArmRotation() => leftArmRotation;
    }
}