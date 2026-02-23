using System.Numerics;
using ReconEngine.PhysicsHandler;

namespace ReconEngine.System3D;

public class PhysicsEntity : ReconEntity3D
{
    internal IPhysicsBody? _physicsBody;

    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;
    private bool _canCollide = true;
    private bool _canBeCast = true;
    private bool _static = false;
    private string _collisionGroup = "Default";
    private object? _shape = null;

    public new Vector3 Position
    {
        get => _physicsBody != null ? _physicsBody.Position : _position;
        set { _position = value; _physicsBody?.Position = value; }
    }
    public new Quaternion Rotation
    {
        get => _physicsBody != null ? _physicsBody.Rotation : _rotation;
        set { _rotation = value; _physicsBody?.Rotation = value; }
    }
    public bool CanCollide
    {
        get => _physicsBody != null ? _physicsBody.CanCollide : _canCollide;
        set { _canCollide = value; _physicsBody?.CanCollide = value; }
    }
    public bool CanBeCast
    {
        get => _physicsBody != null ? _physicsBody.CanBeCast : _canBeCast;
        set { _canBeCast = value; _physicsBody?.CanBeCast = value; }
    }
    public bool Static
    {
        get => _physicsBody != null ? _physicsBody.Static : _static;
        set { _static = value; _physicsBody?.Static = value; }
    }
    public string CollisionGroup
    {
        get => _physicsBody != null ? _physicsBody.CollisionGroup : _collisionGroup;
        set { _collisionGroup = value; _physicsBody?.CollisionGroup = value; }
    }
    public object? Shape
    {
        get => _physicsBody != null ? _physicsBody.Shape : _shape;
        set { _shape = value; _physicsBody?.Shape = value; }
    }

    public override void Ready()
    {
        base.Ready();

        ParentChanged += (sender, oldParent) =>
        {
            if (_physicsBody != null)
            {
                _position = _physicsBody.Position;
                _rotation = _physicsBody.Rotation;
                _canCollide = _physicsBody.CanCollide;
                _canBeCast = _physicsBody.CanBeCast;
                _static = _physicsBody.Static;
                _collisionGroup = _physicsBody.CollisionGroup;
                _shape = _physicsBody.Shape;

                oldParent?.CurrentWorld?.PhysicsEngine.RemoveBody(_physicsBody);
                _physicsBody = null;
            }

            if (CurrentWorld != null)
            {
                IPhysicsBody body = CurrentWorld.PhysicsEngine.CreateBody();
                body.PhysicsEntity = this;
                body.Position = _position;
                body.Rotation = _rotation;
                body.CanCollide = _canCollide;
                body.CanBeCast = _canBeCast;
                body.Static = _static;
                body.CollisionGroup = _collisionGroup;
                body.Shape = _shape;
                _physicsBody = body;
            }
        };
    }
}