using UnityEngine;

public class BoneData
{
    public string boneName;
    public Vector3 position;
    public Quaternion rotation;

    public BoneData(string name, Vector3 pos, Quaternion rot)
    {
        boneName = name;
        position = pos;
        rotation = rot;
    }
}
