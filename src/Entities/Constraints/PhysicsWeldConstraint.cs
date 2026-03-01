using System.Numerics;
using Raylib_cs;
using ReconEngine.RenderingEngines;

namespace ReconEngine.Entities.Constraints;

public class PhysicsWeldConstraint : PhysicsConstraint
{
    protected override void Attach()
    {
        if (EntityA == null || EntityB == null)
        {
            Console.WriteLine("[WeldEntity] BodyA or BodyB is null, skipping");
            return;
        }
        if (EntityA._physicsBody == null || EntityB._physicsBody == null)
        {
            Console.WriteLine("[WeldEntity] One or both bodies have no physics body yet");
            return;
        }

        Matrix4x4 worldA = EntityA.Transform;
        Matrix4x4 pivotWorld = MatrixA * worldA;

        Matrix4x4.Invert(MatrixB, out Matrix4x4 c1Inv);
        Matrix4x4 worldB = c1Inv * pivotWorld;

        Matrix4x4.Decompose(worldB, out _, out Quaternion rotB, out Vector3 posB);
        EntityB.Position = posB;
        EntityB.Rotation = rotB;

        _constraint = CurrentWorld!.PhysicsEngine.CreateWeld(
            EntityA._physicsBody,
            EntityB._physicsBody
        );
    }

    public override void DrawStep3D(float deltaTime, IRenderer renderer)
    {
        base.DrawStep3D(deltaTime, renderer);

        if (!DrawConstraint) return;
        if (EntityA == null || EntityB == null) return;
        if (renderer is not RaylibRenderer) return; // only render while using raylib

        Matrix4x4 worldA = Matrix4x4.CreateFromQuaternion(EntityA.Rotation)
                         * Matrix4x4.CreateTranslation(EntityA.Position);

        Matrix4x4 pivotWorld = MatrixA * worldA;
        Vector3 pivot = pivotWorld.Translation;

        Raylib.DrawSphere(pivot, 0.1f, Color.Yellow);

        Raylib.DrawLine3D(EntityA.Position, pivot, Color.Green);
        Raylib.DrawLine3D(EntityB.Position, pivot, Color.Red);

        Raylib.DrawSphere(EntityA.Position, 0.07f, Color.Green);
        Raylib.DrawSphere(EntityB.Position, 0.07f, Color.Red);

        Matrix4x4 worldB = Matrix4x4.CreateFromQuaternion(EntityB.Rotation)
                         * Matrix4x4.CreateTranslation(EntityB.Position);
        Vector3 mBWorld = (MatrixB * worldB).Translation;
        Raylib.DrawSphere(mBWorld, 0.05f, Color.Blue);
        Raylib.DrawLine3D(EntityB.Position, mBWorld, Color.Blue);
    }
}
