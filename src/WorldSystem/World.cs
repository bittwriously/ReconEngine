using ReconEngine.MeshSystem;
using ReconEngine.PhysicsHandler;
using ReconEngine.PhysicsHandler.LibraryWrappers;
using ReconEngine.UISystem;

namespace ReconEngine.WorldSystem;

public class ReconWorld
{
    public WorldRootEntity Root { get; private set; }
    public string WorldName { get; set; }
    public float TimeScale = 1.0f;
    public GuiContainerRegistry WorldGuiRegistry = new();
    public ReconMeshRegistry WorldMeshRegistry = new();
    public IPhysicsEngine PhysicsEngine = new Jitter2World();

    public ReconWorld(string name = "ReconWorld")
    {
        WorldName = name;
        Root = new WorldRootEntity(this) { Name = name };
    }

    public void Render(float deltaTime) => RenderRecursive(Root, deltaTime);
    public void Physics(float deltaTime)
    {
        PhysicsEngine.Update(deltaTime);
        PhysicsRecursive(Root, deltaTime);
    }

    private void RenderRecursive(ReconEntity entity, float deltaTime)
    {
        if (!entity.IsActive) return;
        if (entity is IUpdatable updatable)
            updatable.RenderStep(deltaTime);
        foreach (var child in entity.Children)
        {
            RenderRecursive(child, deltaTime);
        }
    }
    private void PhysicsRecursive(ReconEntity entity, float deltaTime)
    {
        if (!entity.IsActive) return;
        if (entity is IUpdatable updatable)
            updatable.RenderStep(deltaTime);
        foreach (var child in entity.Children)
        {
            PhysicsRecursive(child, deltaTime);
        }
    }

    public void Clear()
    {
        Root.Destroy();
        Root = new WorldRootEntity(this) { Name = WorldName };
    }
}

public class WorldRootEntity : ReconEntity
{
    public readonly ReconWorld WorldParent;
    internal WorldRootEntity(ReconWorld _worldRef)
    {
        WorldParent = _worldRef;
        _currentWorld = _worldRef;
    }
}
