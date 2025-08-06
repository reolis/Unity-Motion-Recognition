using System.Collections.Generic;
using UnityEngine;

public class RightHandControl : MonoBehaviour
{
    public Transform hand;

    public Transform thumb1, thumb2, thumb3;
    public Transform index1, index2, index3;
    public Transform middle1, middle2, middle3;
    public Transform ring1, ring2, ring3;
    public Transform pinky1, pinky2, pinky3;

    private Dictionary<string, Transform> bones;
    private Skeleton rightSkeleton;
    private HandMovementAI handAI;

    void Start()
    {
        bones = new Dictionary<string, Transform>()
        {
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

        rightSkeleton = new Skeleton();

        rightSkeleton.AddBone("B-hand.R", null, hand.localPosition);

        rightSkeleton.AddBone("B-thumb1.R", "B-hand.R", thumb1.localPosition - hand.localPosition);
        rightSkeleton.AddBone("B-thumb2.R", "B-thumb1.R", thumb2.localPosition - thumb1.localPosition);
        rightSkeleton.AddBone("B-thumb3.R", "B-thumb2.R", thumb3.localPosition - thumb2.localPosition);

        rightSkeleton.AddBone("B-index1.R", "B-hand.R", index1.localPosition - hand.localPosition);
        rightSkeleton.AddBone("B-index2.R", "B-index1.R", index2.localPosition - index1.localPosition);
        rightSkeleton.AddBone("B-index3.R", "B-index2.R", index3.localPosition - index2.localPosition);

        rightSkeleton.AddBone("B-middle1.R", "B-hand.R", middle1.localPosition - hand.localPosition);
        rightSkeleton.AddBone("B-middle2.R", "B-middle1.R", middle2.localPosition - middle1.localPosition);
        rightSkeleton.AddBone("B-middle3.R", "B-middle2.R", middle3.localPosition - middle2.localPosition);

        rightSkeleton.AddBone("B-ring1.R", "B-hand.R", ring1.localPosition - hand.localPosition);
        rightSkeleton.AddBone("B-ring2.R", "B-ring1.R", ring2.localPosition - ring1.localPosition);
        rightSkeleton.AddBone("B-ring3.R", "B-ring2.R", ring3.localPosition - ring2.localPosition);

        rightSkeleton.AddBone("B-pinky1.R", "B-hand.R", pinky1.localPosition - hand.localPosition);
        rightSkeleton.AddBone("B-pinky2.R", "B-pinky1.R", pinky2.localPosition - pinky1.localPosition);
        rightSkeleton.AddBone("B-pinky3.R", "B-pinky2.R", pinky3.localPosition - pinky2.localPosition);

        handAI = new HandMovementAI(rightSkeleton);
    }

    void Update()
    {
        Dictionary<string, Vector3> newPositions = new Dictionary<string, Vector3>();

        while (ConnectToApp.rightHandQueue.TryDequeue(out BoneData boneData))
        {
            if (!boneData.boneName.EndsWith(".R")) continue;

            newPositions[boneData.boneName] = boneData.position;
        }

        if (newPositions.Count > 0)
        {
            handAI.AddTrainingSample(newPositions);
            handAI.ApplyLearnedPose();

            foreach (var kvp in bones)
            {
                if (!rightSkeleton.Bones.ContainsKey(kvp.Key)) continue;

                var bone = rightSkeleton.Bones[kvp.Key];
                kvp.Value.localRotation = bone.LocalRotation;
            }
        }
    }
}