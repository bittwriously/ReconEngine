using System.Numerics;
using ReconEngine.PhysicsHandler;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.Entities;

public abstract class PhysicsConstraint : ReconEntity
{
    public PhysicsEntity? EntityA;
    public PhysicsEntity? EntityB;

    private Matrix4x4 _matA = Matrix4x4.Identity;
    private Matrix4x4 _matB = Matrix4x4.Identity;

    public bool DrawConstraint = false;

    public Matrix4x4 MatrixA
    {
        get => _matA;
        set
        {
            _matA = value;
            if (_constraint != null)
            {
                Detach();
                Attach();
            }
        }
    }

    public Matrix4x4 MatrixB
    {
        get => _matB;
        set
        {
            _matB = value;
            if (_constraint != null)
            {
                Detach();
                Attach();
            }
        }
    }

    public bool Enabled = true;

    protected IPhysicsConstraint? _constraint;

    public override void Ready()
    {
        base.Ready();
        AncestryChanged += (_, __) =>
        {
            Detach();
            if (CurrentWorld != null) Attach();
        };
    }

    protected virtual void Attach() { }
    protected void Detach()
    {
        _constraint?.Remove();
        _constraint = null;
    }

    public override void Destroy()
    {
        Detach();
        base.Destroy();
    }
}
