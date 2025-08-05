using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

public class BodyTorsoControl : MonoBehaviour
{
    public Transform torsoTarget;
    public Transform neckTarget;
    public Transform headTarget;

    Dictionary<string, Transform> bodyTargets;

    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    public float rotationSmoothFactor = 0.1f;

    private float dataTimeout = 1.0f;
    private float lastUpdateTime;

    void Start()
    {
        bodyTargets = new Dictionary<string, Transform>()
        {
            { "B-torso", torsoTarget },
            { "B-neck", neckTarget },
            { "B-head", headTarget }
        };
    }

    void Update()
    {
        Dictionary<string, Vector3> newPositions = new Dictionary<string, Vector3>();

        while (ConnectToApp.centerPoseQueue.TryDequeue(out BoneData boneData))
        {
            if (bodyTargets.ContainsKey(boneData.boneName))
            {
                lastUpdateTime = Time.time;
                Vector3 newPos = boneData.position * positionScale + positionOffset;
                newPositions[boneData.boneName] = newPos;
            }
        }

        if (Time.time - lastUpdateTime > dataTimeout)
        {
            return;
        }

        if (newPositions.Count > 0)
        {
            UpdateBodyRotationsFromPositions(bodyTargets, newPositions);
        }
    }

    void UpdateBodyRotationsFromPositions(Dictionary<string, Transform> targets, Dictionary<string, Vector3> newPositions)
    {
        if (targets.TryGetValue("B-torso", out var torso) &&
            targets.TryGetValue("B-neck", out var neck) &&
            newPositions.TryGetValue("B-torso", out var torsoPos) &&
            newPositions.TryGetValue("B-neck", out var neckPos))
        {
            Vector3 dirTorsoNeck = (neckPos - torsoPos).normalized;
            if (dirTorsoNeck.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirTorsoNeck, Vector3.up);
                torso.rotation = Quaternion.Slerp(torso.rotation, targetRot, rotationSmoothFactor);
            }
        }

        if (targets.TryGetValue("B-neck", out var neck2) &&
            targets.TryGetValue("B-head", out var head) &&
            newPositions.TryGetValue("B-neck", out var neckPos2) &&
            newPositions.TryGetValue("B-head", out var headPos))
        {
            Vector3 dirNeckHead = (headPos - neckPos2).normalized;
            if (dirNeckHead.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirNeckHead, Vector3.up);
                neck2.rotation = Quaternion.Slerp(neck2.rotation, targetRot, rotationSmoothFactor);
            }
        }
    }
}