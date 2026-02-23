using System.Numerics;

namespace ReconEngine.PhysicsHandler;

public enum RaycastFilterMode
{
    Whitelist,
    Blacklist,
    WhitelistDescendants,
}

public struct RaycastParameters
{
    public string CollisionGroup;
    public RaycastFilterMode FilterMode;
    public IPhysicsBody[] FilteredBodies;
    public bool ForceCanCollide;
}

public readonly struct RaycastResult
{
    public readonly Vector3 Position;
    public readonly float Distance;
    public readonly IPhysicsBody Body;
    public readonly Vector3 Normal;
}

public interface IPhysicsEngine
{
    public IPhysicsBody CreateBody();
    public void RemoveBody(IPhysicsBody body);

    public string GetDefaultCollisionGroup();

    public uint CreateCollisionGroup(string name);
    public void SetCollisionGroupBit(string coll1, string coll2);
    public bool GetCollisionGroupBit(string coll1, string coll2);
    public IPhysicsBody[] GetCollisionGroupMembers(string name);
    public void RemoveCollisionGroup(string name);

    public IPhysicsBody[] GetBodies();
    public void RemoveAllBodies();

    public RaycastResult Raycast(Vector3 origin, Vector3 direction, ref RaycastParameters rcparams);
}