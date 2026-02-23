using System.Numerics;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.PhysicsHandler;

public enum RaycastFilterMode
{
    Whitelist,
    Blacklist,
    WhitelistDescendants,
    BlacklistDescendants,
}

public struct RaycastParameters
{
    public string CollisionGroup;
    public RaycastFilterMode FilterMode;
    public HashSet<ReconEntity> FilterEntities;
    public bool ForceCanCollide;
}

public readonly struct RaycastResult(Vector3 pos, float dist, PhysicsEntity body, Vector3 normal)
{
    public readonly Vector3 Position = pos;
    public readonly float Distance = dist;
    public readonly PhysicsEntity Body = body;
    public readonly Vector3 Normal = normal;
}

public interface IPhysicsEngine
{
    public IPhysicsBody CreateBody();
    public void RemoveBody(IPhysicsBody body);

    public string GetDefaultCollisionGroup();

    public ulong CreateCollisionGroup(string name);
    public void SetCollisionGroupBit(string coll1, string coll2, bool value);
    public bool GetCollisionGroupBit(string coll1, string coll2);
    public IPhysicsBody[] GetCollisionGroupMembers(string name);
    public void RemoveCollisionGroup(string name);

    public IPhysicsBody[] GetBodies();
    public void RemoveAllBodies();

    public RaycastResult? Raycast(Vector3 origin, Vector3 direction, RaycastParameters rcparams);

    public void Update(float deltaTime);

    public object GetBoxShape(Vector3 size);
    public object GetSphereShape(float radius);
    public object GetConeShape(float radius, float height);
    public object GetCapsuleShape(float radius, float length);
}