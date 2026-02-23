using ReconEngine.PhysicsHandler;

namespace ReconEngine.System3D;

public class PhysicsEntity : ReconEntity3D
{
    internal readonly IPhysicsBody _physicsBody;

    public bool CanCollide = true;
    public bool CanBeCast = true;
    public bool Static = false;
    public string CollisionGroup = "";
}