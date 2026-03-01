using System.Numerics;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using ReconEngine.System3D;
using ReconEngine.WorldSystem;

namespace ReconEngine.PhysicsHandler.LibraryWrappers;

public class Jitter2CollisionGroupFilter(CollisionGroupRegistry registry) : INarrowPhaseFilter
{
    private readonly CollisionGroupRegistry _registry = registry;
    public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pointA, ref JVector pointB, ref JVector normal, ref float penetration)
    {
        if (shapeA.RigidBody?.Tag is not Jitter2Body a || shapeB.RigidBody?.Tag is not Jitter2Body b) return true;
        if (a.IsTrigger || b.IsTrigger) return false;
        return _registry.Collides(a.CollisionGroup, b.CollisionGroup);
    }
}

public class Jitter2Body : IPhysicsBody
{
    internal RigidBody body;
    private readonly World _worldRef;
    private readonly Jitter2World _wrapperRef;

    public event Action<IPhysicsBody>? BeginCollide;
    public event Action<IPhysicsBody>? EndCollide;

    public PhysicsEntity PhysicsEntity { get; set; } = null!; //sybau

    public Jitter2Body(World jitterWorld, Jitter2World wrapper)
    {
        body = jitterWorld.CreateRigidBody();
        _worldRef = jitterWorld;
        _wrapperRef = wrapper;

        body.MotionType = MotionType.Dynamic;
        body.Tag = this;

        body.BeginCollide += arbiter =>
        {
            RigidBody otherRigidBody = arbiter.Body1 == body ? arbiter.Body2 : arbiter.Body1;
            if (otherRigidBody.Tag is Jitter2Body otherBody)
                BeginCollide?.Invoke(otherBody);
        };

        body.EndCollide += arbiter =>
        {
            RigidBody otherRigidBody = arbiter.Body1 == body ? arbiter.Body2 : arbiter.Body1;
            if (otherRigidBody.Tag is Jitter2Body otherBody)
                EndCollide?.Invoke(otherBody);
        };
    }
    
    private RigidBodyShape? _shape;
    public object? Shape
    {
        get => _shape;
        set
        {
            if (value is not RigidBodyShape newshape) return;
            if (_shape != null) body.RemoveShape(_shape);
            body.AddShape(newshape);
            _shape = newshape;
        }
    }

    public Vector3 Position
    {
        get => body.Position;
        set => body.Position = value;
    }
    public Quaternion Rotation
    {
        get => body.Orientation;
        set => body.Orientation = value;
    }

    public bool CanCollide { get; set; } = true;
    public bool CanBeCast { get; set; } = true;
    public bool Static
    {
        get => body.MotionType == MotionType.Static;
        set => body.MotionType = value ? MotionType.Static : MotionType.Dynamic;
    }

    private string _collisionGroup = "Default";
    public string CollisionGroup
    {
        get => _collisionGroup;
        set
        {
            _wrapperRef.CGRegistry.UnregisterBody(this);
            _wrapperRef.CGRegistry.RegisterBody(this, value);
            body.Tag = value;
            _collisionGroup = value;
        }
    }
    public bool IsTrigger { get; set; } = false;

}

public class Jitter2World : IPhysicsEngine
{
    private readonly World _world = new();
    public CollisionGroupRegistry CGRegistry { get; } = new();
    internal readonly ulong defaultCG;

    public event Action<PhysicsEntity, PhysicsEntity>? BeginContact;
    public event Action<PhysicsEntity, PhysicsEntity>? EndContact;

    private readonly List<Jitter2Body> _bodies = [];

    public Jitter2World()
    {
        defaultCG = CGRegistry.Register("Default");
        _world.NarrowPhaseFilter = new Jitter2CollisionGroupFilter(CGRegistry);
        _world.Gravity = new JVector(0, -9.81f, 0);
    }

    public IPhysicsBody CreateBody()
    {
        Jitter2Body body = new(_world, this);
        _bodies.Add(body);

        body.BeginCollide += other =>
        {
            if (body.PhysicsEntity != null && other.PhysicsEntity != null)
                BeginContact?.Invoke(body.PhysicsEntity, other.PhysicsEntity);
        };

        body.EndCollide += other =>
        {
            if (body.PhysicsEntity != null && other.PhysicsEntity != null)
                EndContact?.Invoke(body.PhysicsEntity, other.PhysicsEntity);
        };

        return body;
    }
    public void RemoveBody(IPhysicsBody body) { if (body is Jitter2Body wrapper) _removeBody(wrapper); }
    private void _removeBody(Jitter2Body wrapper)
    {
        _world.Remove(wrapper.body);
        _bodies.Remove(wrapper);
    }

    public string GetDefaultCollisionGroup() => "Default";

    public ulong CreateCollisionGroup(string name) => CGRegistry.Register(name);
    public void SetCollisionGroupBit(string coll1, string coll2, bool value) => CGRegistry.SetCollision(coll1, coll2, value);
    public bool GetCollisionGroupBit(string coll1, string coll2) => CGRegistry.Collides(coll1, coll2);
    public IPhysicsBody[] GetCollisionGroupMembers(string name) => [.. CGRegistry.GetGroupBodies(name)];
    public void RemoveCollisionGroup(string name) => CGRegistry.Unregister(name);

    public IPhysicsBody[] GetBodies() => [.. _bodies];
    public void RemoveAllBodies()
    {
        foreach (Jitter2Body body in _bodies) RemoveBody(body);
    }

    private static bool IsInHierarchy(ReconEntity entity, HashSet<ReconEntity> filter)
    {
        var current = entity;
        while (current != null)
        {
            if (filter.Contains(current)) return true;
            current = current.Parent;
        }
        return false;
    }

    public RaycastResult? Raycast(Vector3 origin, Vector3 direction, RaycastParameters rcparams)
    {
        bool hit = _world.DynamicTree.RayCast(
            origin,
            direction,
            proxy =>
            {
                if (proxy is not RigidBodyShape shape) return false;
                if (shape.RigidBody?.Tag is not Jitter2Body body) return false;
                if (body.PhysicsEntity == null) return false;
                if (rcparams.ForceCanCollide && !body.CanCollide) return false;
                if (!body.CanBeCast) return false;
                switch (rcparams.FilterMode)
                {
                    case RaycastFilterMode.Whitelist:
                        if (!rcparams.FilterEntities.Contains(body.PhysicsEntity)) return false;
                        break;
                    case RaycastFilterMode.WhitelistDescendants:
                        if (!IsInHierarchy(body.PhysicsEntity, rcparams.FilterEntities)) return false;
                        break;
                    case RaycastFilterMode.Blacklist:
                        if (rcparams.FilterEntities.Contains(body.PhysicsEntity)) return false;
                        break;
                    case RaycastFilterMode.BlacklistDescendants:
                        if (IsInHierarchy(body.PhysicsEntity, rcparams.FilterEntities)) return false;
                        break;
                }
                return CGRegistry.Collides(body.CollisionGroup, rcparams.CollisionGroup);
            },
            null,
            out IDynamicTreeProxy? proxy,
            out JVector normal,
            out float lambda
        );
        if (!hit) return null;
        RigidBodyShape? hitShape = hit ? proxy as RigidBodyShape : null;
        if (hitShape == null) return null;
        if (hitShape.RigidBody.Tag is not Jitter2Body body) return null;
        Vector3 hitpos = origin + direction * lambda;
        float dist = (direction * lambda).Length();
        return new RaycastResult(
            hitpos,
            dist,
            body.PhysicsEntity!, // im too lazy to check for this rn
            normal
        );
    }

    public void Update(float deltaTime) => _world.Step(deltaTime, multiThread: false);

    public object GetBoxShape(Vector3 size) => new BoxShape(size);
    public object GetSphereShape(float radius) => new SphereShape(radius);
    public object GetConeShape(float radius, float height) => new ConeShape(radius, height);
    public object GetCapsuleShape(float radius, float length) => new CapsuleShape(radius, length);

    public IPhysicsConstraint CreateWeld(IPhysicsBody a, IPhysicsBody b)
    {
        if (a is not Jitter2Body ja || b is not Jitter2Body jb)
            throw new ArgumentException("[Jitter2] Both bodies must be Jitter2Body");

        var ballSocket = _world.CreateConstraint<BallSocket>(ja.body, jb.body);
        ballSocket.Initialize(ja.body.Position);

        var fixedAngle = _world.CreateConstraint<FixedAngle>(ja.body, jb.body);
        fixedAngle.Initialize();

        return new Jitter2Constraint(() =>
        {
            _world.Remove(ballSocket);
            _world.Remove(fixedAngle);
        });
    }
}

public class Jitter2Constraint(Action remove) : IPhysicsConstraint
{
    private readonly Action _remove = remove;
    public void Remove() => _remove();
}
