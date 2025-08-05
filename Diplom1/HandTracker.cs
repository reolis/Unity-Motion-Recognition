using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Diplom1
{
    public class HandTracker
    {
        public enum HandType
        {
            Right,
            Left
        }

        public HandType Type { get; }

        private readonly PosePredictor posePredictor;

        private readonly List<int> trackedBoneIds;

        public HandTracker(HandType type, List<int> boneIds, int historyLength = 5, float mlWeight = 0.3f)
        {
            Type = type;
            trackedBoneIds = boneIds;
            posePredictor = new PosePredictor(historyLength, mlWeight);
        }

        public void UpdateBone(int boneId, Vector3 position, Quaternion rotation, float time)
        {
            if (!trackedBoneIds.Contains(boneId))
                return;

            posePredictor.UpdateBonePosition(boneId, position, time);
            posePredictor.UpdateBoneRotation(boneId, rotation, time);
        }

        public Vector3 PredictBonePosition(int boneId, float deltaTime)
        {
            return posePredictor.PredictPosition(boneId, deltaTime);
        }

        public Quaternion PredictBoneRotation(int boneId, float deltaTime)
        {
            return posePredictor.PredictRotation(boneId, deltaTime);
        }

        public void Dispose()
        {
            posePredictor.Dispose();
        }
    }
}
