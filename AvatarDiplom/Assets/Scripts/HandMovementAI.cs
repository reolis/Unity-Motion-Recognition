using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandMovementAI
{
    private Dictionary<string, Vector3> averagedPose = new Dictionary<string, Vector3>();

    private Dictionary<string, Vector3> lastGeneratedPose = new Dictionary<string, Vector3>();

    private float baseLearningRate = 0.1f;
    private float smoothing = 0.2f;
    private float maxAllowedDistance = 0.5f;
    private float maxDeltaPerUpdate = 0.05f;

    private Dictionary<string, float> boneWeights = new Dictionary<string, float>()
    {
        { "B-hand.L", 1.0f },
        { "B-forearm.L", 0.7f },
        { "B-upperArm.L", 0.5f },

        { "B-hand.R", 1.0f },
        { "B-forearm.R", 0.7f },
        { "B-upperArm.R", 0.5f }
    };

    public void AddTrainingSample(Dictionary<string, Vector3> handPositions)
    {
        if (averagedPose.Count == 0)
        {
            averagedPose = handPositions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            lastGeneratedPose = new Dictionary<string, Vector3>(averagedPose);
            return;
        }

        foreach (var key in handPositions.Keys)
        {
            if (!averagedPose.ContainsKey(key))
            {
                averagedPose[key] = handPositions[key];
                lastGeneratedPose[key] = handPositions[key];
                continue;
            }

            float distance = Vector3.Distance(averagedPose[key], handPositions[key]);
            if (distance > maxAllowedDistance)
            {
                continue;
            }

            float adaptiveLR = Mathf.Clamp(distance, 0.01f, baseLearningRate);
            float weight = boneWeights.ContainsKey(key) ? boneWeights[key] : 0.5f;

            Vector3 delta = handPositions[key] - averagedPose[key];

            delta = Vector3.ClampMagnitude(delta, maxDeltaPerUpdate);

            averagedPose[key] += delta * adaptiveLR * weight;
        }
    }

    public Dictionary<string, Vector3> GenerateMovement(Dictionary<string, Vector3> currentPose)
    {
        if (averagedPose.Count == 0)
            return currentPose;

        var result = new Dictionary<string, Vector3>();

        foreach (var key in averagedPose.Keys)
        {
            Vector3 prev = lastGeneratedPose.ContainsKey(key) ? lastGeneratedPose[key] : currentPose.GetValueOrDefault(key, averagedPose[key]);
            Vector3 target = averagedPose[key];

            Vector3 next = Vector3.Lerp(prev, target, smoothing);

            result[key] = next;
        }

        lastGeneratedPose = result;
        return result;
    }
}