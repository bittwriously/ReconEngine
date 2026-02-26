using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine.AnimationSystem;

public class AnimationController : ReconEntity
{
    public readonly Dictionary<string, AnimationJoint> Joints = [];

    public void RegisterJoint(AnimationJoint joints)
    {
        if (string.IsNullOrEmpty(joints.JointName))
        {
            Console.WriteLine("[AnimationController] AnimationJoint has no JointName, skipping...");
            return;
        }
        Joints[joints.JointName] = joints;
    }

    public void RegisterJoints(IEnumerable<AnimationJoint> joints)
    {
        foreach (var joint in joints)
            RegisterJoint(joint);
    }

    public void UnregisterJoint(string jointName) => Joints.Remove(jointName);

    public void ResetPose()
    {
        foreach (var joint in Joints.Values)
        {
            joint.AnimPosition = Vector3.Zero;
            joint.AnimRotation = Quaternion.Identity;
        }
    }
}
