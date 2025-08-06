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
    private Skeleton leftSkeleton;
    private HandMovementAI handAI;

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

        leftSkeleton = new Skeleton();

        leftSkeleton.AddBone("B-hand.L", null, hand.localPosition);

        leftSkeleton.AddBone("B-thumb1.L", "B-hand.L", thumb1.localPosition - hand.localPosition);
        leftSkeleton.AddBone("B-thumb2.L", "B-thumb1.L", thumb2.localPosition - thumb1.localPosition);
        leftSkeleton.AddBone("B-thumb3.L", "B-thumb2.L", thumb3.localPosition - thumb2.localPosition);

        leftSkeleton.AddBone("B-index1.L", "B-hand.L", index1.localPosition - hand.localPosition);
        leftSkeleton.AddBone("B-index2.L", "B-index1.L", index2.localPosition - index1.localPosition);
        leftSkeleton.AddBone("B-index3.L", "B-index2.L", index3.localPosition - index2.localPosition);

        leftSkeleton.AddBone("B-middle1.L", "B-hand.L", middle1.localPosition - hand.localPosition);
        leftSkeleton.AddBone("B-middle2.L", "B-middle1.L", middle2.localPosition - middle1.localPosition);
        leftSkeleton.AddBone("B-middle3.L", "B-middle2.L", middle3.localPosition - middle2.localPosition);

        leftSkeleton.AddBone("B-ring1.L", "B-hand.L", ring1.localPosition - hand.localPosition);
        leftSkeleton.AddBone("B-ring2.L", "B-ring1.L", ring2.localPosition - ring1.localPosition);
        leftSkeleton.AddBone("B-ring3.L", "B-ring2.L", ring3.localPosition - ring2.localPosition);

        leftSkeleton.AddBone("B-pinky1.L", "B-hand.L", pinky1.localPosition - hand.localPosition);
        leftSkeleton.AddBone("B-pinky2.L", "B-pinky1.L", pinky2.localPosition - pinky1.localPosition);
        leftSkeleton.AddBone("B-pinky3.L", "B-pinky2.L", pinky3.localPosition - pinky2.localPosition);

        handAI = new HandMovementAI(leftSkeleton);
    }

    void Update()
    {
        Dictionary<string, Vector3> newPositions = new Dictionary<string, Vector3>();

        while (ConnectToApp.leftHandQueue.TryDequeue(out BoneData boneData))
        {
            if (!boneData.boneName.EndsWith(".L")) continue;

            newPositions[boneData.boneName] = boneData.position;
        }

        if (newPositions.Count > 0)
        {
            handAI.AddTrainingSample(newPositions);
            handAI.ApplyLearnedPose();

            foreach (var kvp in bones)
            {
                if (!leftSkeleton.Bones.ContainsKey(kvp.Key)) continue;

                var bone = leftSkeleton.Bones[kvp.Key];
                kvp.Value.localRotation = bone.LocalRotation;
            }
        }
    }
}