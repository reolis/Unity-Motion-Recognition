using System.Collections.Generic;
using UnityEngine;

public class RightHandControl : MonoBehaviour
{
    public Transform shoulder;
    public Transform upperArm;
    public Transform forearm;
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
            { "B-shoulder.R", shoulder },
            { "B-upperArm.R", upperArm },
            { "B-forearm.R", forearm },
            { "B-hand.R", hand },

            { "B-thumb1.R", thumb1 },
            { "B-thumb2.R", thumb2 },
            { "B-thumb3.R", thumb3 },

            { "B-index1.R", index1 },
            { "B-index2.R", index2 },
            { "B-index3.R", index3 },

            { "B-middle1.R", middle1 },
            { "B-middle2.R", middle2 },
            { "B-middle3.R", middle3 },

            { "B-ring1.R", ring1 },
            { "B-ring2.R", ring2 },
            { "B-ring3.R", ring3 },

            { "B-pinky1.R", pinky1 },
            { "B-pinky2.R", pinky2 },
            { "B-pinky3.R", pinky3 }
        };
    }

    void Update()
    {
        Dictionary<string, Vector3> newPositions = new Dictionary<string, Vector3>();

        while (ConnectToApp.rightHandQueue.TryDequeue(out BoneData boneData))
        {
            if (!boneData.boneName.EndsWith(".R")) continue;

            Vector3 rawPos = boneData.position;

            // ѕросто используем сырые позиции из очереди
            newPositions[boneData.boneName] = rawPos;
        }

        if (bones.TryGetValue("B-upperArm.R", out var upperArm) &&
            bones.TryGetValue("B-forearm.R", out var forearm) &&
            bones.TryGetValue("B-hand.R", out var hand) &&
            newPositions.TryGetValue("B-upperArm.R", out var upperArmPos) &&
            newPositions.TryGetValue("B-forearm.R", out var forearmPos) &&
            newPositions.TryGetValue("B-hand.R", out var handPos))
        {
            Vector3 dirUpper = forearmPos - upperArmPos;
            if (dirUpper.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotUpper = Quaternion.LookRotation(dirUpper, Vector3.forward);
                upperArm.rotation = Quaternion.Slerp(upperArm.rotation, targetRotUpper, 0.1f);
            }

            Vector3 dirForearm = handPos - forearmPos;
            if (dirForearm.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotForearm = Quaternion.LookRotation(dirForearm, Vector3.forward);
                forearm.rotation = Quaternion.Slerp(forearm.rotation, targetRotForearm, 0.1f);
            }
        }

        UpdateFingerRotation("B-thumb1.R", "B-thumb2.R", newPositions);
        UpdateFingerRotation("B-thumb2.R", "B-thumb3.R", newPositions);

        UpdateFingerRotation("B-index1.R", "B-index2.R", newPositions);
        UpdateFingerRotation("B-index2.R", "B-index3.R", newPositions);

        UpdateFingerRotation("B-middle1.R", "B-middle2.R", newPositions);
        UpdateFingerRotation("B-middle2.R", "B-middle3.R", newPositions);

        UpdateFingerRotation("B-ring1.R", "B-ring2.R", newPositions);
        UpdateFingerRotation("B-ring2.R", "B-ring3.R", newPositions);

        UpdateFingerRotation("B-pinky1.R", "B-pinky2.R", newPositions);
        UpdateFingerRotation("B-pinky2.R", "B-pinky3.R", newPositions);
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