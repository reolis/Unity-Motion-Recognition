using System.Collections.Generic;
using UnityEngine;

public class HandMovementAI
{
    private Skeleton skeleton;

    private Dictionary<string, Quaternion> averagedRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, Queue<Quaternion>> rotationBuffer = new();
    private Dictionary<string, Quaternion> lastOutputRotations = new Dictionary<string, Quaternion>();

    private Dictionary<string, Vector3> averagedPositions = new();
    private Dictionary<string, Queue<Vector3>> positionBuffer = new();
    private Dictionary<string, Vector3> lastOutputPositions = new();

    private Dictionary<string, Quaternion> initialTwist = new();
    private bool initializedTwistReference = false;

    private float smoothing = 0.85f;
    private float initialLearningRate = 0.7f;
    private int trainingSamplesCount = 0;

    private float minAngleThreshold = 1f;
    private float maxAngleStep = 45f;
    int bufferSize = 5;

    public HandMovementAI(Skeleton skeleton)
    {
        this.skeleton = skeleton;

        foreach (var boneName in skeleton.Bones.Keys)
        {
            averagedRotations[boneName] = Quaternion.identity;
            lastOutputRotations[boneName] = Quaternion.identity;
        }
    }

    private Quaternion GetTwist(Quaternion q, Vector3 twistAxis)
    {
        DecomposeSwingTwist(q, twistAxis, out _, out Quaternion twist);
        return twist;
    }

    public void AddTrainingSample(Dictionary<string, Vector3> observedWorldPositions)
    {
        trainingSamplesCount++;

        float adaptiveRate = Mathf.Clamp01(1f - Mathf.Exp(-0.1f * trainingSamplesCount));
        float learningRate = Mathf.Lerp(0.3f, 0.9f, adaptiveRate);

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
            Quaternion desiredRot = Quaternion.LookRotation(directionWorld, Vector3.down);
            Quaternion localRot = Quaternion.Inverse(parentRot) * desiredRot;

            if (kvp.Key.ToLower().Contains("shoulder") || kvp.Key.ToLower().Contains("forearm") ||
                kvp.Key.ToLower().Contains("upperArm") || kvp.Key.ToLower().Contains("hand"))
            {
                Vector3 twistAxis = bone.LocalPosition.normalized;
                twistAxis.z = -twistAxis.z;
                /*twistAxis.x = -twistAxis.x;
                twistAxis.y = -twistAxis.y;*/
                Quaternion swing, twist;

                if (!initializedTwistReference)
                {
                    Quaternion baseTwist = GetTwist(localRot, twistAxis);
                    initialTwist[kvp.Key] = baseTwist;
                }

                DecomposeSwingTwist(localRot, twistAxis, out swing, out twist);

                if (initialTwist.ContainsKey(kvp.Key))
                {
                    twist = Quaternion.Inverse(initialTwist[kvp.Key]) * twist;
                }

                float angle;
                Vector3 axis;
                twist.ToAngleAxis(out angle, out axis);
                angle = Mathf.Clamp(angle, -90f, 90f);
                twist = Quaternion.AngleAxis(angle, axis);
                localRot = swing * twist;

                if (initialTwist.ContainsKey(kvp.Key))
                {
                    localRot = localRot * initialTwist[kvp.Key];
                }
            }

            if (!rotationBuffer.ContainsKey(kvp.Key))
                rotationBuffer[kvp.Key] = new Queue<Quaternion>();

            var buffer = rotationBuffer[kvp.Key];
            buffer.Enqueue(localRot);
            if (buffer.Count > bufferSize)
                buffer.Dequeue();

            Quaternion avg = AverageQuaternions(buffer.ToArray());
            averagedRotations[kvp.Key] = avg;

            if (!positionBuffer.ContainsKey(kvp.Key))
                positionBuffer[kvp.Key] = new Queue<Vector3>();

            var posBuffer = positionBuffer[kvp.Key];
            posBuffer.Enqueue(currentWorldPos);
            if (posBuffer.Count > bufferSize)
                posBuffer.Dequeue();

            Vector3 avgPos = AverageVectors(posBuffer.ToArray());
            averagedPositions[kvp.Key] = avgPos;

            initializedTwistReference = true;
        }
    }

    private Vector3 AverageVectors(Vector3[] vectors)
    {
        if (vectors.Length == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var v in vectors)
            sum += v;

        return sum / vectors.Length;
    }

    public void ApplyLearnedPose()
    {
        foreach (var kvp in skeleton.Bones)
        {
            string name = kvp.Key;
            Bone bone = kvp.Value;

            Quaternion current = lastOutputRotations.ContainsKey(name) ? lastOutputRotations[name] : Quaternion.identity;
            Quaternion target = averagedRotations.ContainsKey(name) ? averagedRotations[name] : Quaternion.identity;

            float angleDelta = Quaternion.Angle(current, target);
            float adaptiveSmoothing = Mathf.Lerp(0.3f, smoothing, angleDelta / 90f);

            Quaternion result = Quaternion.Slerp(current, target, adaptiveSmoothing);

            bone.LocalRotation = result;
            lastOutputRotations[name] = result;

            if (averagedPositions.ContainsKey(name))
            {
                Vector3 currentPos = bone.GetWorldPosition();
                Vector3 targetPos = averagedPositions[name];
                Vector3 smoothedPos = Vector3.Lerp(currentPos, targetPos, adaptiveSmoothing);

                if (bone.Parent != null)
                {
                    Vector3 parentPos = bone.Parent.GetWorldPosition();
                    Vector3 localPos = Quaternion.Inverse(bone.Parent.GetWorldRotation()) * (smoothedPos - parentPos);
                    bone.LocalPosition = localPos;
                }

                lastOutputPositions[name] = smoothedPos;
            }
        }
    }

    public Dictionary<string, Vector3> GetWorldPose()
    {
        return skeleton.GetWorldPositions();
    }

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

    private Quaternion AverageQuaternions(Quaternion[] quaternions)
    {
        if (quaternions.Length == 0) return Quaternion.identity;

        Quaternion cumulative = quaternions[0];
        for (int i = 1; i < quaternions.Length; i++)
        {
            cumulative = Quaternion.Slerp(cumulative, quaternions[i], 1f / (i + 1));
        }
        return cumulative;
    }

    private void DecomposeSwingTwist(Quaternion q, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
    {
        Vector3 ra = new Vector3(q.x, q.y, q.z);
        Vector3 proj = Vector3.Project(ra, twistAxis.normalized);
        twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
        twist = Quaternion.Normalize(twist);

        swing = q * Quaternion.Inverse(twist);
    }
}