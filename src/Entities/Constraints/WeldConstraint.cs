using System.Numerics;

namespace ReconEngine.Entities;

/// <summary>
/// NOT RECOMMENDED FOR PHYSICS OBJECTS
/// WILL KINDA WORK BUT WILL HAVE MORE SIDE EFFECTS
/// THAN ACTUAL USES
/// FOR CONNECTING 2 PHYSICS BODIES TOGETHER USE PhysicsWeldConstraint
/// </summary>
public class WeldConstraint : ReconConstraint3D
{
    public override void PostPhysicsStep(float deltaTime)
    {
        base.PostPhysicsStep(deltaTime);

        if (!Enabled) return;
        if (EntityA == null || EntityB == null) return;
        if (EntityA.CurrentWorld != EntityB.CurrentWorld) return;
        if (EntityA.CurrentWorld != CurrentWorld) return;

        if (!Matrix4x4.Invert(MatrixA, out Matrix4x4 invertedAnchorA)) return;
        Matrix4x4 deltaA = invertedAnchorA * EntityA.Transform;

        EntityB.Transform = MatrixB * deltaA;
    }
}
