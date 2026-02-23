using System.Numerics;
using ReconEngine.PhysicsHandler;

namespace ReconEngine.System3D;

public class PhysicsEntity : ReconEntity3D
{
    internal IPhysicsBody? _physicsBody;

    public new Vector3 Position
    {
        get => _physicsBody != null ? _physicsBody.Position : Vector3.Zero;
        set => _physicsBody?.Position = value;
    }
    public new Quaternion Rotation
    {
        get => _physicsBody != null ? _physicsBody.Rotation : Quaternion.Identity;
        set => _physicsBody?.Rotation = value;
    }

    public bool CanCollide
    {
        get => _physicsBody == null || _physicsBody.CanCollide;
        set => _physicsBody?.CanCollide = value;
    }
    public bool CanBeCast
    {
        get => _physicsBody == null || _physicsBody.CanBeCast;
        set => _physicsBody?.CanBeCast = value;
    }
    public bool Static
    {
        get => _physicsBody == null || _physicsBody.Static;
        set => _physicsBody?.Static = value;
    }
    public string CollisionGroup
    {
        get => _physicsBody != null ? _physicsBody.CollisionGroup : "";
        set => _physicsBody?.CollisionGroup = value;
    }

    public object? Shape
    {
        get => _physicsBody?.Shape;
        set => _physicsBody?.Shape = value;
    }

    public override void Ready()
    {
        base.Ready();

        ParentChanged += (sender, oldParent) =>
        {
            if (_physicsBody != null) oldParent?.CurrentWorld?.PhysicsEngine.RemoveBody(_physicsBody);
            if (CurrentWorld != null)
            {
                IPhysicsBody body = CurrentWorld.PhysicsEngine.CreateBody();
                body.PhysicsEntity = this;
                _physicsBody = body;
            }
        };
    }
}