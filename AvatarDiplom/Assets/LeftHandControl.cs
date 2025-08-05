using System.Collections.Generic;
using UnityEngine;

public class LeftHandControl : MonoBehaviour
{
    public Transform hand;

    public Transform thumb1, thumb2, thumb3;
    public Transform index1, index2, index3;
    public Transform middle1, middle2, middle3;
    public Transform ring1, ring2, ring3;
    public Transform pinky1, pinky2, pinky3;

    private Dictionary<string, Transform> bones;

    void Start()
    {
        bones = new Dictionary<string, Transform>()
        {
            { "B-hand.L", hand },

            { "B-thumb1.L", thumb1 },
            { "B-thumb2.L", thumb2 },
            { "B-thumb3.L", thumb3 },

            { "B-index1.L", index1 },
            { "B-index2.L", index2 },
            { "B-index3.L", index3 },

            { "B-middle1.L", middle1 },
            { "B-middle2.L", middle2 },
            { "B-middle3.L", middle3 },

            { "B-ring1.L", ring1 },
            { "B-ring2.L", ring2 },
            { "B-ring3.L", ring3 },

            { "B-pinky1.L", pinky1 },
            { "B-pinky2.L", pinky2 },
            { "B-pinky3.L", pinky3 }
        };
    }

    void Update()
    {
        Dictionary<string, Vector3> newPositions = new Dictionary<string, Vector3>();

        while (ConnectToApp.leftHandQueue.TryDequeue(out BoneData boneData))
        {
            if (!boneData.boneName.EndsWith(".L")) continue;

            Vector3 rawPos = boneData.position;

            newPositions[boneData.boneName] = rawPos;
        }

        UpdateFingerRotation("B-thumb1.L", "B-thumb2.L", newPositions);
        UpdateFingerRotation("B-thumb2.L", "B-thumb3.L", newPositions);

        UpdateFingerRotation("B-index1.L", "B-index2.L", newPositions);
        UpdateFingerRotation("B-index2.L", "B-index3.L", newPositions);

        UpdateFingerRotation("B-middle1.L", "B-middle2.L", newPositions);
        UpdateFingerRotation("B-middle2.L", "B-middle3.L", newPositions);

        UpdateFingerRotation("B-ring1.L", "B-ring2.L", newPositions);
        UpdateFingerRotation("B-ring2.L", "B-ring3.L", newPositions);

        UpdateFingerRotation("B-pinky1.L", "B-pinky2.L", newPositions);
        UpdateFingerRotation("B-pinky2.L", "B-pinky3.L", newPositions);
    }

    private void UpdateFingerRotation(string jointA, string jointB, Dictionary<string, Vector3> positions)
    {
        if (bones.TryGetValue(jointA, out var boneA) &&
            bones.TryGetValue(jointB, out var boneB) &&
            boneA.parent != null)
        {
            if (!positions.TryGetValue(jointA, out var posA)) return;
            if (!positions.TryGetValue(jointB, out var posB)) return;

            Vector3 dirWorld = posB - posA;
            if (dirWorld.sqrMagnitude > 0.0001f)
            {
                Vector3 dirLocal = boneA.parent.InverseTransformDirection(dirWorld.normalized);

                Vector3 localForward = Vector3.right;

                Quaternion targetLocalRot = Quaternion.FromToRotation(localForward, dirLocal);

                targetLocalRot = ClampRotationAroundAxis(targetLocalRot, 0f, 90f);

                boneA.localRotation = Quaternion.Slerp(boneA.localRotation, targetLocalRot, 0.3f);
            }
        }
    }

    private Quaternion ClampRotationAroundAxis(Quaternion q, float minAngle, float maxAngle)
    {
        q.ToAngleAxis(out float angle, out Vector3 axis);
        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        return Quaternion.AngleAxis(angle, axis);
    }
}