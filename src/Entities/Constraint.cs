using System.Numerics;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.Entities;

public abstract class ReconConstraint3D : ReconEntity
{
    public ReconEntity3D? EntityA;
    public ReconEntity3D? EntityB;

    public Matrix4x4 MatrixA = Matrix4x4.Identity;
    public Matrix4x4 MatrixB = Matrix4x4.Identity;

    public bool Enabled = true;
}
