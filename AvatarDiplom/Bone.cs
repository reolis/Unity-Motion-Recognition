using UnityEngine;

public class Bone
{
    public string Name;
    public Bone Parent;
    public Vector3 LocalPosition;
    public Quaternion LocalRotation = Quaternion.identity;

    public Vector3 GetWorldPosition()
    {
        if (Parent == null)
            return LocalPosition;
        return Parent.GetWorldPosition() + Parent.GetWorldRotation() * LocalPosition;
    }

    public Quaternion GetWorldRotation()
    {
        if (Parent == null)
            return LocalRotation;
        return Parent.GetWorldRotation() * LocalRotation;
    }

    public void ApplyRotation(Quaternion delta)
    {
        LocalRotation = delta * LocalRotation;
    }
}
