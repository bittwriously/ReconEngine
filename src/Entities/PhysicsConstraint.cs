using System.Numerics;
using ReconEngine.PhysicsHandler;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.Entities;

public abstract class PhysicsConstraint : ReconEntity
{
    public PhysicsEntity? EntityA;
    public PhysicsEntity? EntityB;

    public Matrix4x4 MatrixA = Matrix4x4.Identity;
    public Matrix4x4 MatrixB = Matrix4x4.Identity;

    public bool Enabled = true;

    protected IPhysicsConstraint? physicsConstraint;
}
