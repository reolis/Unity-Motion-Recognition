using UnityEngine;
using System.Collections.Generic;

public class BoneController : MonoBehaviour
{
    public Transform rightShoulder;
    public Transform rightUpperArm;
    public Transform rightForearm;
    public Transform rightWrist;

    public Transform leftShoulder;
    public Transform leftUpperArm;
    public Transform leftForearm;
    public Transform leftWrist;

    public Transform spine;
    public Transform chest;
    public Transform neck;
    public Transform head;

    public Transform rightThumb1;
    public Transform rightThumb2;
    public Transform rightThumb3;

    public Transform rightIndex1;
    public Transform rightIndex2;
    public Transform rightIndex3;

    public Transform rightMiddle1;
    public Transform rightMiddle2;
    public Transform rightMiddle3;

    public Transform rightRing1;
    public Transform rightRing2;
    public Transform rightRing3;

    public Transform rightPinky1;
    public Transform rightPinky2;
    public Transform rightPinky3;

    public Transform leftThumb1;
    public Transform leftThumb2;
    public Transform leftThumb3;

    public Transform leftIndex1;
    public Transform leftIndex2;
    public Transform leftIndex3;

    public Transform leftMiddle1;
    public Transform leftMiddle2;
    public Transform leftMiddle3;

    public Transform leftRing1;
    public Transform leftRing2;
    public Transform leftRing3;

    public Transform leftPinky1;
    public Transform leftPinky2;
    public Transform leftPinky3;

    private static Dictionary<string, Transform> boneMap;

    void Awake()
    {
        boneMap = new Dictionary<string, Transform>
        {
            // Основные кости
            { "B-shoulder.R", rightShoulder },
            { "B-upperArm.R", rightUpperArm },
            { "B-forearm.R", rightForearm },
            { "B-hand.R", rightWrist },

            { "B-shoulder.L", leftShoulder },
            { "B-upperArm.L", leftUpperArm },
            { "B-forearm.L", leftForearm },
            { "B-hand.L", leftWrist },

            { "B-spine", spine },
            { "B-chest", chest },
            { "B-neck", neck },
            { "B-head", head },

            // Правая рука - пальцы
            { "B-thumb1.R", rightThumb1 },
            { "B-thumb2.R", rightThumb2 },
            { "B-thumb3.R", rightThumb3 },

            { "B-index1.R", rightIndex1 },
            { "B-index2.R", rightIndex2 },
            { "B-index3.R", rightIndex3 },

            { "B-middle1.R", rightMiddle1 },
            { "B-middle2.R", rightMiddle2 },
            { "B-middle3.R", rightMiddle3 },

            { "B-ring1.R", rightRing1 },
            { "B-ring2.R", rightRing2 },
            { "B-ring3.R", rightRing3 },

            { "B-pinky1.R", rightPinky1 },
            { "B-pinky2.R", rightPinky2 },
            { "B-pinky3.R", rightPinky3 },

            // Левая рука - пальцы
            { "B-thumb1.L", leftThumb1 },
            { "B-thumb2.L", leftThumb2 },
            { "B-thumb3.L", leftThumb3 },

            { "B-index1.L", leftIndex1 },
            { "B-index2.L", leftIndex2 },
            { "B-index3.L", leftIndex3 },

            { "B-middle1.L", leftMiddle1 },
            { "B-middle2.L", leftMiddle2 },
            { "B-middle3.L", leftMiddle3 },

            { "B-ring1.L", leftRing1 },
            { "B-ring2.L", leftRing2 },
            { "B-ring3.L", leftRing3 },

            { "B-pinky1.L", leftPinky1 },
            { "B-pinky2.L", leftPinky2 },
            { "B-pinky3.L", leftPinky3 },
        };
    }

    public static void SetBoneRotation(string boneName, Vector3 eulerRotation)
    {
        if (boneMap.TryGetValue(boneName, out Transform bone) && bone != null)
        {
            bone.localRotation = Quaternion.Euler(eulerRotation);
            Debug.Log($"Set rotation of {boneName} to {eulerRotation}");
        }
        else
        {
            Debug.LogWarning($"Bone '{boneName}' не назначена или не найдена.");
        }
    }


    public static void SetBonePosition(string boneName, Vector3 position)
    {
        if (boneMap.TryGetValue(boneName, out Transform bone) && bone != null)
        {
            bone.localPosition = position;
        }
        else
        {
            Debug.LogWarning($"Bone '{boneName}' не назначена или не найдена.");
        }
    }

    public static bool TryGetBone(string boneName, out Transform bone)
    {
        return boneMap.TryGetValue(boneName, out bone) && bone != null;
    }

}
