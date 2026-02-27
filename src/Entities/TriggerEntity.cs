using ReconEngine.PhysicsHandler;
using ReconEngine.PhysicsHandler.LibraryWrappers;
using ReconEngine.System3D;

namespace ReconEngine.Entities;

public class TriggerEntity : PhysicsEntity
{
    public event Action<PhysicsEntity>? Entered;
    public event Action<PhysicsEntity>? Exited;

    public IReadOnlyCollection<PhysicsEntity> Overlapping => _overlapping;
    private readonly HashSet<PhysicsEntity> _overlapping = [];

    public string? FilterGroup = null;

    private IPhysicsEngine? _world;

    public new bool Static;
    public new bool CanCollide;

    public override void Ready()
    {
        base.Ready();
        base.Static = true;
        base.CanCollide = false;
        _physicsBody?.IsTrigger = true;
        AncestryChanged += (_, __) => RewireEvents();
        RewireEvents();
    }

    private void RewireEvents()
    {
        if (_world != null)
        {
            _world.BeginContact -= OnBeginContact;
            _world.EndContact -= OnEndContact;
            _world = null;
        }
        if (CurrentWorld?.PhysicsEngine is Jitter2World jw)
        {
            _world = jw;
            _world.BeginContact += OnBeginContact;
            _world.EndContact += OnEndContact;
        }
        _overlapping.Clear();
    }

    private void OnBeginContact(PhysicsEntity a, PhysicsEntity b)
    {
        PhysicsEntity? visitor = null;
        if (a == this) visitor = b;
        else if (b == this) visitor = a;

        if (visitor == null) return;
        if (!PassesFilter(visitor)) return;
        if (!_overlapping.Add(visitor)) return; // already inside

        Entered?.Invoke(visitor);
    }

    private void OnEndContact(PhysicsEntity a, PhysicsEntity b)
    {
        PhysicsEntity? visitor = null;
        if (a == this) visitor = b;
        else if (b == this) visitor = a;

        if (visitor == null) return;
        if (!_overlapping.Remove(visitor)) return; // wasn't inside

        Exited?.Invoke(visitor);
    }

    private bool PassesFilter(PhysicsEntity entity)
    {
        if (FilterGroup == null) return true;
        return _world?.CGRegistry.Collides(entity.CollisionGroup, FilterGroup) ?? true;
    }

    public override void Destroy()
    {
        if (_world != null)
        {
            _world.BeginContact -= OnBeginContact;
            _world.EndContact -= OnEndContact;
        }
        base.Destroy();
    }
}
