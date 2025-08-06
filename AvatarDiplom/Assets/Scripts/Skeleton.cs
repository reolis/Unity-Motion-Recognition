using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Skeleton
{
    public Dictionary<string, Bone> Bones = new Dictionary<string, Bone>();

    public void AddBone(string name, string parentName, Vector3 localPosition)
    {
        Bone parent = null;
        if (parentName != null && Bones.ContainsKey(parentName))
            parent = Bones[parentName];

        var bone = new Bone
        {
            Name = name,
            Parent = parent,
            LocalPosition = localPosition,
        };

        Bones[name] = bone;
    }

    public Dictionary<string, Vector3> GetWorldPositions()
    {
        return Bones.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetWorldPosition());
    }
}