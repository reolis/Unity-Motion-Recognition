using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Linq;

public class BodyControl : MonoBehaviour
{
    public Transform playerTransform;

    // Левая рука
    public Transform shoulderLTarget;
    public Transform upperArmLTarget;
    public Transform forearmLTarget;
    public Transform handLTarget;

    // Правая рука
    public Transform shoulderRTarget;
    public Transform upperArmRTarget;
    public Transform forearmRTarget;
    public Transform handRTarget;

    // Тело и голова
    public Transform spineTarget;
    public Transform chestTarget;
    public Transform neckTarget;
    public Transform headTarget;

    private Dictionary<string, Transform> leftTargets;
    private Dictionary<string, Transform> rightTargets;
    private Dictionary<string, Transform> bodyTargets;

    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    public float rotationSmoothFactor = 0.1f;

    public float leftDataTimeout = 1.0f;
    public float rightDataTimeout = 1.0f;
    public float bodyDataTimeout = 1.0f;

    private float lastLeftUpdateTime;
    private float lastRightUpdateTime;
    private float lastBodyUpdateTime;

    public Vector3 leftHandOffset = new Vector3(-0.3f, 0f, 0.5f);
    public Vector3 rightHandOffset = new Vector3(0.3f, 0f, 0.5f);

    public Vector3 leftPositionOffset = Vector3.zero;
    public Vector3 rightPositionOffset = Vector3.zero;

    private HandMovementAI leftHandAI = new HandMovementAI();
    private HandMovementAI rightHandAI = new HandMovementAI();
    public bool useAIHands = true;

    Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();

    void Start()
    {
        leftTargets = new Dictionary<string, Transform>
        {
            { "B-shoulder.L", shoulderLTarget },
            { "B-upperArm.L", upperArmLTarget },
            { "B-forearm.L", forearmLTarget },
            { "B-hand.L", handLTarget }
        };

        rightTargets = new Dictionary<string, Transform>
        {
            { "B-shoulder.R", shoulderRTarget },
            { "B-upperArm.R", upperArmRTarget },
            { "B-forearm.R", forearmRTarget },
            { "B-hand.R", handRTarget }
        };

        bodyTargets = new Dictionary<string, Transform>
        {
            { "B-spine", spineTarget },
            { "B-chest", chestTarget },
            { "B-neck", neckTarget },
            { "B-head", headTarget }
        };
    }

    void Update()
    {
        var leftPositions = GetUpdatedPositions(ConnectToApp.leftPoseQueue, ".L", ref lastLeftUpdateTime, leftPositionOffset, true);
        var rightPositions = GetUpdatedPositions(ConnectToApp.rightPoseQueue, ".R", ref lastRightUpdateTime, rightPositionOffset, true);

        if (leftPositions.Count > 0)
            leftHandAI.AddTrainingSample(leftPositions);

        if (rightPositions.Count > 0)
            rightHandAI.AddTrainingSample(rightPositions);

        if (useAIHands)
        {
            leftPositions = leftHandAI.GenerateMovement(leftPositions);
            rightPositions = rightHandAI.GenerateMovement(rightPositions);
        }

        AdjustHandPositions(leftPositions, leftHandOffset, ".L");
        AdjustHandPositions(rightPositions, rightHandOffset, ".R");

        LowerHandsOverTime(leftPositions, lastLeftUpdateTime);
        LowerHandsOverTime(rightPositions, lastRightUpdateTime);

        UpdateBoneRotationsFromPositions(leftTargets, leftPositions, ".L");
        UpdateBoneRotationsFromPositions(rightTargets, rightPositions, ".R");

        if (Time.time - lastLeftUpdateTime > leftDataTimeout)
            PhysicsForArms.SimulateHangingArm(leftTargets, true);

        if (Time.time - lastRightUpdateTime > rightDataTimeout)
            PhysicsForArms.SimulateHangingArm(rightTargets, false);
    }

    void LowerHandsOverTime(Dictionary<string, Vector3> handPositions, float lastUpdateTime)
    {
        float timeSinceUpdate = Time.time - lastUpdateTime;

        float downFactor = Mathf.Clamp01(timeSinceUpdate / 2f);

        Vector3 down = Vector3.down * 0.3f;

        var keys = new List<string>(handPositions.Keys);

        foreach (var key in keys)
        {
            handPositions[key] = Vector3.Lerp(handPositions[key], handPositions[key] + down, downFactor * 0.05f);
        }
    }

    Dictionary<string, Vector3> GetUpdatedPositions(
        ConcurrentQueue<BoneData> queue,
        string suffix,
        ref float lastUpdateTime,
        Vector3 customOffset,
        bool mirrorX)
    {
        var positions = new Dictionary<string, Vector3>();

        while (queue.TryDequeue(out BoneData boneData))
        {
            if (!boneData.boneName.EndsWith(suffix)) continue;

            Vector3 pos = boneData.position;

            if (mirrorX)
                pos.z = -pos.z;

            pos = pos * positionScale + customOffset;

            positions[boneData.boneName] = pos;
            lastUpdateTime = Time.time;
        }

        return positions;
    }

    void UpdateBoneRotationsFromPositions(
        Dictionary<string, Transform> targets,
        Dictionary<string, Vector3> positions,
        string suffix)
    {
        string[] chain = { "B-upperArm", "B-forearm", "B-hand" };
        for (int i = 0; i < chain.Length - 1; i++)
        {
            string from = chain[i] + suffix;
            string to = chain[i + 1] + suffix;

            if (targets.TryGetValue(from, out var fromT) &&
                targets.TryGetValue(to, out var toT) &&
                positions.TryGetValue(from, out var fromPos) &&
                positions.TryGetValue(to, out var toPos))
            {
                Vector3 dir = toPos - fromPos;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.Cross(Vector3.right, dir.normalized));
                    fromT.rotation = Quaternion.Slerp(fromT.rotation, rot, rotationSmoothFactor);
                }
            }
        }

        if (string.IsNullOrEmpty(suffix))
        {
            string[] torsoChain = { "B-spine", "B-chest", "B-neck", "B-head" };
            for (int i = 0; i < torsoChain.Length - 1; i++)
            {
                string from = torsoChain[i];
                string to = torsoChain[i + 1];

                if (targets.TryGetValue(from, out var fromT) &&
                    targets.TryGetValue(to, out var toT) &&
                    positions.TryGetValue(from, out var fromPos) &&
                    positions.TryGetValue(to, out var toPos))
                {
                    Vector3 dir = toPos - fromPos;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        fromT.rotation = Quaternion.Slerp(fromT.rotation, rot, rotationSmoothFactor);
                    }
                }
            }
        }
    }

    void AdjustHandPositions(Dictionary<string, Vector3> handPositions, Vector3 handOffset, string handPrefix)
    {
        if (playerTransform == null) return;

        Vector3 forward = playerTransform.forward;
        Vector3 right = playerTransform.right;
        Vector3 up = playerTransform.up;

        Vector3 basePos = playerTransform.position
                          + right * handOffset.x
                          + up * handOffset.y
                          + forward * handOffset.z;

        foreach (var key in handPositions.Keys.ToList())
        {
            if (!key.StartsWith(handPrefix)) continue;

            Vector3 currentPos = handPositions[key];

            if (handPrefix == ".L")
                currentPos.x -= 0.5f;
            else if (handPrefix == ".R")
                currentPos.x += 0.5f;

            currentPos.z = -currentPos.z;

            float distanceZ = currentPos.z;
            float dynamicLerp = Mathf.Clamp01(1f - distanceZ);
            float lerpFactor = Mathf.Lerp(0.05f, 0.2f, dynamicLerp);

            if (previousPositions.TryGetValue(key, out Vector3 prev))
                handPositions[key] = Vector3.Lerp(prev, currentPos, 0.15f);
            else
                handPositions[key] = currentPos;

            previousPositions[key] = handPositions[key];

            handPositions[key] = Smooth(currentPos, previousPositions[key], 0.15f);
        }

        Vector3 Smooth(Vector3 current, Vector3 previous, float smoothing)
        {
            return Vector3.Lerp(previous, current, smoothing);
        }
    }
}