using System.Numerics;

namespace ReconEngine.PhysicsHandler;

public interface IPhysicsBody
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public string CollisionGroup { get; set; }
    public bool CanCollide { get; set; }
    public bool CanBeCast { get; set; }
    public bool Static { get; set; }
}