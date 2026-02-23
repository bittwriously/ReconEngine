using ReconEngine.PhysicsHandler;

namespace ReconEngine.System3D;

public class PhysicsEntity : ReconEntity3D
{
    internal IPhysicsBody? _physicsBody;

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


}