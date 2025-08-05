using System;
using System.Collections.Generic;
using UnityEngine;


public class FrameRecord
{
    public float timestamp;
    public List<BoneData> bones = new();
}

public class MotionRecorder
{
    public Dictionary<string, Transform> leftTargets;
    public Dictionary<string, Transform> rightTargets;

    private List<FrameRecord> recordedFrames = new();
    private bool isRecording = false;
    private bool isReplaying = false;
    private int replayIndex = 0;
    private float replayStartTime = 0f;

    private Dictionary<string, Vector3> currentLeft = new();
    private Dictionary<string, Vector3> currentRight = new();

    public void UpdateBonePositions(Dictionary<string, Vector3> left, Dictionary<string, Vector3> right)
    {
        currentLeft = new Dictionary<string, Vector3>(left);
        currentRight = new Dictionary<string, Vector3>(right);
    }

    void Update()
    {
        if (isRecording)
            RecordCurrentFrame();

        if (isReplaying)
            ReplayFrames();
    }

    private void RecordCurrentFrame()
    {
        FrameRecord frame = new FrameRecord
        {
            timestamp = Time.time
        };

        foreach (var kv in currentLeft)
        {
            if (leftTargets.TryGetValue(kv.Key, out var t))
                frame.bones.Add(new BoneData(kv.Key, kv.Value, t.rotation));
        }

        foreach (var kv in currentRight)
        {
            if (rightTargets.TryGetValue(kv.Key, out var t))
                frame.bones.Add(new BoneData(kv.Key, kv.Value, t.rotation));
        }

        recordedFrames.Add(frame);
    }

    private void ReplayFrames()
    {
        if (replayIndex >= recordedFrames.Count) return;

        float elapsed = Time.time - replayStartTime;

        while (replayIndex < recordedFrames.Count &&
               recordedFrames[replayIndex].timestamp <= elapsed)
        {
            ApplyFrame(recordedFrames[replayIndex]);
            replayIndex++;
        }
    }

    private void ApplyFrame(FrameRecord frame)
    {
        foreach (var bone in frame.bones)
        {
            if (bone.boneName.EndsWith(".L") && leftTargets.TryGetValue(bone.boneName, out var l))
            {
                l.position = bone.position;
                l.rotation = bone.rotation;
            }
            else if (bone.boneName.EndsWith(".R") && rightTargets.TryGetValue(bone.boneName, out var r))
            {
                r.position = bone.position;
                r.rotation = bone.rotation;
            }
        }
    }


    public void StartRecording()
    {
        recordedFrames.Clear();
        isRecording = true;
        isReplaying = false;
    }

    public void StartReplay()
    {
        isRecording = false;
        isReplaying = true;
        replayIndex = 0;
        replayStartTime = Time.time;
    }

    public void Stop()
    {
        isRecording = false;
        isReplaying = false;
    }

    public bool IsRecording => isRecording;
    public bool IsReplaying => isReplaying;

}