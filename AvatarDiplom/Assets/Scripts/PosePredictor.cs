using System.Collections.Generic;
using UnityEngine;

public class PosePredictor
{
    private class BoneHistory
    {
        public Queue<Vector3> posHistory = new Queue<Vector3>();
        public Queue<Vector3> rotHistory = new Queue<Vector3>();

        public Vector3 LastFilteredPos = Vector3.zero;
        public Vector3 LastFilteredRotEuler = Vector3.zero;
    }

    private readonly Dictionary<string, BoneHistory> history = new();

    private int maxHistory = 5;
    private float outlierDistanceThreshold = 1f;

    public void UpdateBone(string boneName, Vector3 newPosition, Quaternion newRotation)
    {
        if (!history.ContainsKey(boneName))
        {
            history[boneName] = new BoneHistory();
        }

        var h = history[boneName];

        Vector3 filteredPos = FilterOutliersAndSmooth(h.posHistory, newPosition);

        Vector3 newRotEuler = newRotation.eulerAngles;
        Vector3 filteredRotEuler = FilterOutliersAndSmooth(h.rotHistory, newRotEuler);

        h.LastFilteredPos = filteredPos;
        h.LastFilteredRotEuler = filteredRotEuler;
    }

    public Vector3 PredictPosition(string boneName, Vector3 currentObserved)
    {
        if (currentObserved != Vector3.zero)
            return currentObserved;

        if (!history.TryGetValue(boneName, out var h))
            return Vector3.zero;

        return h.LastFilteredPos;
    }

    public Quaternion PredictRotation(string boneName, Quaternion currentObserved)
    {
        if (currentObserved != Quaternion.identity)
            return currentObserved;

        if (!history.TryGetValue(boneName, out var h))
            return Quaternion.identity;

        return Quaternion.Euler(h.LastFilteredRotEuler);
    }

    private Vector3 FilterOutliersAndSmooth(Queue<Vector3> queue, Vector3 newValue)
    {
        if (queue.Count > 0)
        {
            Vector3 last = queue.Peek();
            float dist = Vector3.Distance(last, newValue);

            if (dist > outlierDistanceThreshold)
            {
                newValue = last;
            }
        }

        queue.Enqueue(newValue);
        if (queue.Count > maxHistory)
            queue.Dequeue();

        Vector3 sum = Vector3.zero;
        foreach (var v in queue)
            sum += v;

        return sum / queue.Count;
    }
}