using System.Numerics;
using ReconEngine.PhysicsHandler.LibraryWrappers;
using ReconEngine.System3D;

namespace ReconEngine.PhysicsHandler;

public interface IPhysicsBody
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    internal PhysicsEntity PhysicsEntity { get; set; }

    public object? Shape { get; set; }

    public string CollisionGroup { get; set; }
    public bool CanCollide { get; set; }
    public bool CanBeCast { get; set; }
    public bool Static { get; set; }
    public bool IsTrigger { get; set; }

    public event Action<Jitter2Body>? BeginCollide;
    public event Action<Jitter2Body>? EndCollide;
}
