using System.Collections.Generic;
using UnityEngine;

public class LearningHandAI
{
    private Transform playerTransform;

    private Dictionary<string, Vector3> previousHandPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Vector3> targetOffsets = new Dictionary<string, Vector3>();

    private float learningRate = 0.05f;
    private float returnStrength = 0.1f;

    public LearningHandAI(Transform player)
    {
        playerTransform = player;

        targetOffsets[".L"] = new Vector3(-0.3f, -0.7f, 0.2f);
        targetOffsets[".R"] = new Vector3(0.3f, -0.7f, 0.2f);
    }

    public void UpdateHands(Dictionary<string, Vector3> currentHandPositions)
    {
        foreach (var handName in currentHandPositions.Keys)
        {
            if (!previousHandPositions.ContainsKey(handName))
                previousHandPositions[handName] = currentHandPositions[handName];

            Vector3 baseWorldPosition = GetWorldPositionFromOffset(targetOffsets[handName]);

            float deviation = Vector3.Distance(currentHandPositions[handName], baseWorldPosition);
            float pullBack = Mathf.Clamp01(deviation) * returnStrength;

            Vector3 learnedPosition = Vector3.Lerp(previousHandPositions[handName], currentHandPositions[handName], learningRate);

            Vector3 adjusted = Vector3.Lerp(learnedPosition, baseWorldPosition, pullBack);

            currentHandPositions[handName] = adjusted;
            previousHandPositions[handName] = adjusted;
        }
    }

    private Vector3 GetWorldPositionFromOffset(Vector3 offset)
    {
        if (playerTransform == null) return Vector3.zero;

        return playerTransform.position
            + (-playerTransform.right) * offset.x
            + (-playerTransform.up) * offset.y
            + (-playerTransform.forward) * offset.z;
    }

    public void AdjustOffset(string hand, Vector3 delta)
    {
        if (targetOffsets.ContainsKey(hand))
        {
            targetOffsets[hand] += delta;
        }
    }

    public void ResetHand(string hand)
    {
        if (hand == "LeftHand")
            targetOffsets[hand] = new Vector3(-0.3f, -0.7f, 0.2f);
        else if (hand == "RightHand")
            targetOffsets[hand] = new Vector3(0.3f, -0.7f, 0.2f);
    }
}