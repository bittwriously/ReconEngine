using System.Numerics;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.AnimationSystem;

public class AnimationJoint : ReconEntity
{
    public string JointName = "";

    public ReconEntity3D? Entity1;
    public ReconEntity3D? Entity2;

    public Matrix4x4 M1 = Matrix4x4.Identity;
    public Matrix4x4 M2 = Matrix4x4.Identity;

    public Vector3 AnimPosition = Vector3.Zero;
    public Quaternion AnimRotation = Quaternion.Identity;

    public override void PhysicsStep(float deltaTime)
    {
        base.PhysicsStep(deltaTime);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (Entity1 == null || Entity2 == null) return;

        Matrix4x4 animTransform =
            Matrix4x4.CreateFromQuaternion(AnimRotation) *
            Matrix4x4.CreateTranslation(AnimPosition);

        Matrix4x4.Invert(M2, out Matrix4x4 m2Inv);
        Matrix4x4 world = animTransform * m2Inv * M1 * Entity1.Transform;
        Matrix4x4.Decompose(world, out _, out Quaternion rot, out Vector3 pos);

        Entity1.Position = pos;
        Entity2.Rotation = rot;
    }
}
