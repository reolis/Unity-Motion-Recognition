using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class HandController
{
    private MathModelHuman physicsModel;
    private Skeleton skeleton;

    public HandController(Skeleton skeleton)
    {
        this.skeleton = skeleton;
        physicsModel = new MathModelHuman();
    }

    public void Update(float deltaTime, Dictionary<string, Vector3> observedPositions)
    {
        Vector2 desiredAngles = ExtractJointAnglesFromHandAI();
        Vector2 currentAngles = physicsModel.GetJointAngles();
        Vector2 angleError = desiredAngles - currentAngles;

        float Kp = 100f;
        Vector2 muscleTorques = angleError * Kp;

        physicsModel.muscleTorque = muscleTorques;
        physicsModel.Update(deltaTime);

        UpdateSkeletonWithPhysics(physicsModel.GetJointAngles());
    }

    private Vector2 ExtractJointAnglesFromHandAI()
    {
        var shoulderBone = skeleton.Bones["B-hand.R"];
        var elbowBone = skeleton.Bones["B-forearm.R"];

        float shoulderAngle = NormalizeAngle(shoulderBone.LocalRotation.eulerAngles.z) * Mathf.Deg2Rad;
        float elbowAngle = NormalizeAngle(elbowBone.LocalRotation.eulerAngles.z) * Mathf.Deg2Rad;

        return new Vector2(shoulderAngle, elbowAngle);
    }

    private void UpdateSkeletonWithPhysics(Vector2 angles)
    {
        var shoulderBone = skeleton.Bones["B-hand.R"];
        var elbowBone = skeleton.Bones["B-forearm.R"];

        shoulderBone.LocalRotation = Quaternion.Euler(0, 0, angles.x * Mathf.Rad2Deg);
        elbowBone.LocalRotation = Quaternion.Euler(0, 0, angles.y * Mathf.Rad2Deg);
    }

    private float NormalizeAngle(float angleDegrees)
    {
        if (angleDegrees > 180f)
            angleDegrees -= 360f;
        return angleDegrees;
    }
}