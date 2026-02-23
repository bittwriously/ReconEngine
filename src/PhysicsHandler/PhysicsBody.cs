using System.Numerics;

namespace ReconEngine.PhysicsHandler;

public interface IPhysicsBody
{
    public Vector3 GetPosition();
    public Quaternion GetRotation();

    public void SetCollisionGroup(string name);
    public string GetCollisionGroup();
}