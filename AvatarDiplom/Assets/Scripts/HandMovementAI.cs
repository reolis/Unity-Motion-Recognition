using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

public class HandMovementAI
{
    private Skeleton skeleton;

    private Dictionary<string, Quaternion> averagedRotations = new();
    private Dictionary<string, Queue<Quaternion>> rotationBuffer = new();
    private Dictionary<string, Quaternion> lastOutputRotations = new();

    private Dictionary<string, Vector3> averagedPositions = new();
    private Dictionary<string, Queue<Vector3>> positionBuffer = new();
    private Dictionary<string, Vector3> lastOutputPositions = new();

    private Dictionary<string, BoneConstraints> boneConstraints = new();

    private Dictionary<string, string> boneGroupMap = new();
    private Dictionary<string, BoneConfig> boneConfigs = new();

    private int bufferSize = 25;
    private MathModelHuman humanModel;

    public class BoneConfig
    {
        public float Smoothing = 0.85f;
        public float MaxSwing = 45f;
        public float MaxTwist = 90f;
        public float MinTwist = -90f;
    }

    public HandMovementAI(Skeleton skeleton)
    {
        this.skeleton = skeleton;
        humanModel = new MathModelHuman();

        boneConfigs["hand"] = new BoneConfig { Smoothing = 0.85f, MaxSwing = 45f, MaxTwist = 90f, MinTwist = -90f };
        boneConfigs["finger"] = new BoneConfig { Smoothing = 0.65f, MaxSwing = 25f, MaxTwist = 15f, MinTwist = -15f };

        string[] fingerKeywords = { "thumb", "index", "middle", "ring", "pinky" };

        foreach (var boneName in skeleton.Bones.Keys)
        {
            averagedRotations[boneName] = Quaternion.identity;
            lastOutputRotations[boneName] = Quaternion.identity;

            string lowerName = boneName.ToLower();

            if (lowerName.Contains("hand"))
            {
                boneGroupMap[boneName] = "hand";
            }
            else
            {
                bool isFinger = false;
                foreach (var keyword in fingerKeywords)
                {
                    if (lowerName.Contains(keyword))
                    {
                        boneGroupMap[boneName] = "finger";
                        isFinger = true;
                        break;
                    }
                }
                if (!isFinger)
                {
                    boneGroupMap[boneName] = "finger";
                }
            }

            boneConstraints[boneName] = new BoneConstraints
            {
                TwistAxis = ComputeBoneDirection(skeleton.Bones[boneName]),
                MinTwistAngle = boneConfigs[boneGroupMap[boneName]].MinTwist,
                MaxTwistAngle = boneConfigs[boneGroupMap[boneName]].MaxTwist,
                MaxSwingAngle = boneConfigs[boneGroupMap[boneName]].MaxSwing
            };
        }
    }

    public void AddTrainingSample(Dictionary<string, Vector3> observedWorldPositions)
    {
        foreach (var kvp in observedWorldPositions)
        {
            if (!skeleton.Bones.ContainsKey(kvp.Key))
                continue;

            var bone = skeleton.Bones[kvp.Key];
            if (bone.Parent == null)
                continue;

            Vector3 parentWorldPos = bone.Parent.GetWorldPosition();
            Vector3 currentWorldPos = kvp.Value;

            Vector3 directionWorld = (currentWorldPos - parentWorldPos).normalized;
            Quaternion parentRot = bone.Parent.GetWorldRotation();
            Quaternion desiredRot = Quaternion.LookRotation(directionWorld, Vector3.up);
            Quaternion localRot = Quaternion.Inverse(parentRot) * desiredRot;

            if (boneConstraints.ContainsKey(kvp.Key))
            {
                var constraint = boneConstraints[kvp.Key];
                Vector3 twistAxis = constraint.TwistAxis;

                DecomposeSwingTwist(localRot, twistAxis, out Quaternion swing, out Quaternion twist);

                float twistAngle = Vector3.SignedAngle(twistAxis, twist * twistAxis, twistAxis);
                twistAngle = Mathf.Clamp(twistAngle, constraint.MinTwistAngle, constraint.MaxTwistAngle);
                twist = Quaternion.AngleAxis(twistAngle, twistAxis);

                swing.ToAngleAxis(out float swingAngle, out Vector3 swingAxis);
                swingAngle = Mathf.Clamp(swingAngle, 0f, constraint.MaxSwingAngle);
                swing = Quaternion.AngleAxis(swingAngle, swingAxis);

                localRot = swing * twist;
            }

            if (!rotationBuffer.ContainsKey(kvp.Key))
                rotationBuffer[kvp.Key] = new Queue<Quaternion>();
            rotationBuffer[kvp.Key].Enqueue(localRot);
            if (rotationBuffer[kvp.Key].Count > bufferSize)
                rotationBuffer[kvp.Key].Dequeue();
            averagedRotations[kvp.Key] = AverageQuaternions(rotationBuffer[kvp.Key].ToArray());

            if (!positionBuffer.ContainsKey(kvp.Key))
                positionBuffer[kvp.Key] = new Queue<Vector3>();
            positionBuffer[kvp.Key].Enqueue(currentWorldPos);
            if (positionBuffer[kvp.Key].Count > bufferSize)
                positionBuffer[kvp.Key].Dequeue();
            averagedPositions[kvp.Key] = AverageVectors(positionBuffer[kvp.Key].ToArray());
        }
    }

    public void ApplyLearnedPose()
    {
        foreach (var kvp in skeleton.Bones)
        {
            string boneName = kvp.Key;
            Bone bone = kvp.Value;

            string group = boneGroupMap.ContainsKey(boneName) ? boneGroupMap[boneName] : "finger";
            var config = boneConfigs[group];

            Quaternion current = lastOutputRotations[boneName];
            Quaternion target = averagedRotations[boneName];

            float angleDelta = Quaternion.Angle(current, target);
            float adaptiveSmoothing = Mathf.Lerp(0.3f, config.Smoothing, angleDelta / 90f);

            Quaternion result = Quaternion.Slerp(current, target, adaptiveSmoothing);

            bone.LocalRotation = result;
            lastOutputRotations[boneName] = result;

            if (averagedPositions.ContainsKey(boneName))
            {
                Vector3 currentPos = bone.GetWorldPosition();
                Vector3 targetPos = averagedPositions[boneName];
                Vector3 smoothedPos = Vector3.Lerp(currentPos, targetPos, adaptiveSmoothing);

                if (bone.Parent != null)
                {
                    Vector3 parentPos = bone.Parent.GetWorldPosition();
                    Vector3 localPos = Quaternion.Inverse(bone.Parent.GetWorldRotation()) * (smoothedPos - parentPos);
                    bone.LocalPosition = localPos;
                }
                lastOutputPositions[boneName] = smoothedPos;
            }
        }
    }

    public void Update(float dt)
    {
        humanModel.muscleTorque = new Vector2(0.5f, -0.3f);
        humanModel.Update(dt);

        Vector2 angles = humanModel.GetJointAngles();
        if (skeleton.Bones.TryGetValue("upperArm", out Bone upperArm))
        {
            upperArm.LocalRotation = Quaternion.Euler(0, 0, angles.x * Mathf.Rad2Deg);
        }

        if (skeleton.Bones.TryGetValue("forearm", out Bone forearm))
        {
            forearm.LocalRotation = Quaternion.Euler(0, 0, angles.y * Mathf.Rad2Deg);
        }
    }

    public Dictionary<string, Vector3> GetWorldPose() => skeleton.GetWorldPositions();

    public void SetWorldPose(Dictionary<string, Vector3> worldPositions)
    {
        foreach (var kvp in worldPositions)
        {
            if (!skeleton.Bones.ContainsKey(kvp.Key))
                continue;

            var bone = skeleton.Bones[kvp.Key];
            if (bone.Parent == null)
                continue;

            Vector3 parentWorldPos = bone.Parent.GetWorldPosition();
            Vector3 currentWorldPos = kvp.Value;

            Vector3 directionWorld = (currentWorldPos - parentWorldPos).normalized;
            Quaternion parentRot = bone.Parent.GetWorldRotation();
            Quaternion desiredRot = Quaternion.LookRotation(directionWorld, Vector3.down);
            Quaternion localRot = Quaternion.Inverse(parentRot) * desiredRot;

            averagedRotations[kvp.Key] = localRot;
            lastOutputRotations[kvp.Key] = localRot;
        }
    }

    private Vector3 AverageVectors(Vector3[] vectors)
    {
        if (vectors.Length == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var v in vectors) sum += v;
        return sum / vectors.Length;
    }

    private Quaternion AverageQuaternions(Quaternion[] quats)
    {
        if (quats.Length == 0) return Quaternion.identity;
        Quaternion avg = quats[0];
        for (int i = 1; i < quats.Length; i++)
            avg = Quaternion.Slerp(avg, quats[i], 1f / (i + 1));
        return avg;
    }

    private void DecomposeSwingTwist(Quaternion rot, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
    {
        twistAxis.Normalize();
        Vector3 proj = Vector3.Project(rot * twistAxis, twistAxis);
        twist = Quaternion.FromToRotation(rot * twistAxis, proj) * rot;
        twist.Normalize();
        swing = rot * Quaternion.Inverse(twist);
    }

    private Vector3 ComputeBoneDirection(Bone bone)
    {
        if (bone.Parent == null) return Vector3.forward;
        return bone.LocalPosition.normalized;
    }
}