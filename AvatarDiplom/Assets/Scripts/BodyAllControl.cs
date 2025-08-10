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

    public Vector3 leftHandOffset = new Vector3(-0.3f, 0f, -0.5f);
    public Vector3 rightHandOffset = new Vector3(0.3f, 0f, -0.5f);

    public Vector3 leftPositionOffset = Vector3.zero;
    public Vector3 rightPositionOffset = Vector3.zero;

    private PosePredictor leftPredictor = new PosePredictor();
    private PosePredictor rightPredictor = new PosePredictor();

    private HandMovementAI leftHandAI;
    private HandMovementAI rightHandAI;
    public bool useAIHands = true;

    private Skeleton leftSkeleton;
    private Skeleton rightSkeleton;

    private Dictionary<string, Vector3> basePositions;
    private Dictionary<string, Quaternion> baseRotations;

    Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();

    private Skeleton bodySkeleton;
    private HandMovementAI bodyAI;

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

        leftSkeleton = new Skeleton();
        leftSkeleton.AddBone("B-shoulder.L", null, shoulderLTarget.localPosition);
        leftSkeleton.AddBone("B-upperArm.L", "B-shoulder.L", upperArmLTarget.localPosition - shoulderLTarget.localPosition);
        leftSkeleton.AddBone("B-forearm.L", "B-upperArm.L", forearmLTarget.localPosition - upperArmLTarget.localPosition);
        leftSkeleton.AddBone("B-hand.L", "B-forearm.L", handLTarget.localPosition - forearmLTarget.localPosition);

        rightSkeleton = new Skeleton();
        rightSkeleton.AddBone("B-shoulder.R", null, shoulderRTarget.localPosition);
        rightSkeleton.AddBone("B-upperArm.R", "B-shoulder.R", upperArmRTarget.localPosition - shoulderRTarget.localPosition);
        rightSkeleton.AddBone("B-forearm.R", "B-upperArm.R", forearmRTarget.localPosition - upperArmRTarget.localPosition);
        rightSkeleton.AddBone("B-hand.R", "B-forearm.R", handRTarget.localPosition - forearmRTarget.localPosition);

        leftHandAI = new HandMovementAI(leftSkeleton);
        rightHandAI = new HandMovementAI(rightSkeleton);

        bodySkeleton = new Skeleton();

        bodySkeleton.AddBone("B-spine", null, spineTarget.localPosition);
        bodySkeleton.AddBone("B-chest", "B-spine", chestTarget.localPosition - spineTarget.localPosition);
        bodySkeleton.AddBone("B-neck", "B-chest", neckTarget.localPosition - chestTarget.localPosition);
        bodySkeleton.AddBone("B-head", "B-neck", headTarget.localPosition - neckTarget.localPosition);

        bodyAI = new HandMovementAI(bodySkeleton);
    }

    void FixedUpdate()
    {
        var leftPositions = GetUpdatedPositions(ConnectToApp.leftPoseQueue, ".L", ref lastLeftUpdateTime, leftPositionOffset, true);
        var rightPositions = GetUpdatedPositions(ConnectToApp.rightPoseQueue, ".R", ref lastRightUpdateTime, rightPositionOffset, true);
        var bodyPositions = GetUpdatedPositions(ConnectToApp.bodyPoseQueue, "", ref lastBodyUpdateTime, positionOffset, false);

        if (leftPositions.Count > 0)
        {
            leftHandAI.AddTrainingSample(leftPositions);
            leftHandAI.ApplyLearnedPose();
            leftHandAI.Update(Time.deltaTime);
            leftPositions = leftHandAI.GetWorldPose();
        }

        if (rightPositions.Count > 0)
        {
            rightHandAI.AddTrainingSample(rightPositions);
            rightHandAI.ApplyLearnedPose();
            rightHandAI.Update(Time.deltaTime);
            rightPositions = rightHandAI.GetWorldPose();
        }

        if (bodyPositions.Count > 0)
        {
            bodyAI.AddTrainingSample(bodyPositions);
            bodyAI.ApplyLearnedPose();
            bodyPositions = bodyAI.GetWorldPose();
        }

        UpdateBoneRotationsFromPositions(leftTargets, leftPositions, ".L");
        UpdateBoneRotationsFromPositions(rightTargets, rightPositions, ".R");
        UpdateBoneRotationsFromPositions(bodyTargets, bodyPositions, "");
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
            pos.z = -pos.z;
            pos.y = -pos.y;
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
                dir.z = -dir.z;
                dir.y = -dir.y;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.Cross(Vector3.left, dir.normalized));
                    fromT.rotation = Quaternion.Slerp(fromT.rotation, rot, rotationSmoothFactor);
                }
            }
        }

        if (string.IsNullOrEmpty(suffix))
        {
            string[] torsoChain = { "B-head" };
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
                        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.down);
                        fromT.rotation = Quaternion.Slerp(fromT.rotation, rot, rotationSmoothFactor);
                    }
                }
            }
        }
    }
}