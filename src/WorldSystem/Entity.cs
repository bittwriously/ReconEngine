namespace ReconEngine.WorldSystem;

public static class ReconEntityRegistry
{
    private readonly static Dictionary<uint, ReconEntity> registry = [];
    private static uint idcounter = 0;
    private static readonly List<uint> freeids = [];
    private static readonly Lock _lock = new();

    public static ReconEntity? GetEntity(uint id)
    {
        ReconEntity? entity;
        lock (_lock) { registry.TryGetValue(id, out entity); }
        return entity;
    }
    public static void RegisterEntity(ReconEntity entity)
    {
        lock (_lock)
        {
            if (registry.ContainsKey(entity.EntityId)) return;
            registry.Add(entity.EntityId, entity);
        }
    }
    public static uint GetNextId()
    {
        lock (_lock)
        {
            if (freeids.Count > 0)
            {
                int lastIndex = freeids.Count - 1;
                uint id = freeids[lastIndex];
                freeids.RemoveAt(lastIndex);
                return id;
            }
            return ++idcounter;
        }
    }
    public static void FreeId(uint id)
    {
        lock (_lock) {
            registry.Remove(id);
            freeids.Add(id);
        }
    }
}

public struct HierarchyData
{
    public HierarchyData() {}
    public uint ParentId = 0; // 0 means no node is here
    public uint SiblingBefore = 0;
    public uint SiblingAfter = 0;
    public uint ChildrenEntry = 0;
    public uint ChildrenExit = 0;
}

public enum CacheResetDirection
{
    Up,      // clear ancestors (use when this entity moved to a new parent)
    Down,    // clear descendants (use when this entity's children changed)
    Both     // full branch reset
}

public class ReconEntity: IUpdatable
{
    public readonly uint EntityId;
    public ReconEntity? Parent
    {
        get 
        {
            if (hierarchyData.ParentId == 0) return null;
            return ReconEntityRegistry.GetEntity(hierarchyData.ParentId);
        }
        set
        {
            ReconEntity? prevParent = Parent;
            prevParent?.RemoveChild(this);
            if (value == null)
            {
                hierarchyData.ParentId = 0;
                _currentWorld = null;
            }
            else
            {
                hierarchyData.ParentId = value.EntityId;
                value.AddChild(this);
                _currentWorld = value.CurrentWorld;
            }
            ResetCache(CacheResetDirection.Both);
            prevParent?.ResetCache(CacheResetDirection.Both);
        }
    }
    public ReconWorld? CurrentWorld { get => _currentWorld; }
    public string Name
    {
        get { return name; }
        set
        {
            name = value;
            namehash = value.GetHashCode(StringComparison.CurrentCulture);
            ResetCache(CacheResetDirection.Both);
        }
    }
    public IEnumerable<ReconEntity> Children
    {
        get
        {
            uint currentId = hierarchyData.ChildrenEntry;
            while (currentId != 0)
            {
                var entity = ReconEntityRegistry.GetEntity(currentId);
                if (entity == null) yield break;
                yield return entity;
                currentId = entity.hierarchyData.SiblingAfter;
            }
        }
    }
    public string ClassName
    {
        get { return this.GetType().Name; }
    }
    public bool IsActive = true;

    private int namehash = 0;
    private string name = "";
    private HierarchyData hierarchyData = new();

    public ReconEntity()
    {
        EntityId = ReconEntityRegistry.GetNextId();
        Name = ClassName;
        ReconEntityRegistry.RegisterEntity(this);

        Ready();
    }

    private string? _pathCache;
    private bool _isPathDirty = true;
    protected ReconWorld? _currentWorld;
    private readonly Dictionary<int, ReconEntity> _childCache = [];
    private readonly Dictionary<int, ReconEntity> _descCache = [];
    private readonly Dictionary<int, ReconEntity> _ancestorCache = [];

    public string GetPath()
    {
        if (!_isPathDirty && _pathCache != null) return _pathCache;
        if (Parent == null) _pathCache = Name;
        else _pathCache = $"{Parent.GetPath()}/{Name}";
        _isPathDirty = false;
        return _pathCache;
    }
    public virtual void AddChild(ReconEntity entity)
    {
        uint lastid = hierarchyData.ChildrenExit;
        if (hierarchyData.ChildrenEntry == 0) hierarchyData.ChildrenEntry = entity.EntityId;
        if (lastid != 0) ReconEntityRegistry.GetEntity(lastid)?.hierarchyData.SiblingAfter = entity.EntityId;
        hierarchyData.ChildrenExit = entity.EntityId;
        entity.hierarchyData.ParentId = EntityId;
        entity.hierarchyData.SiblingBefore = lastid;
        entity.hierarchyData.SiblingAfter = 0;
    }
    public virtual void RemoveChild(ReconEntity entity)
    {
        uint nextid = entity.hierarchyData.SiblingAfter;
        uint previd = entity.hierarchyData.SiblingBefore;

        if (previd != 0) 
            ReconEntityRegistry.GetEntity(previd)!.hierarchyData.SiblingAfter = nextid;
        else hierarchyData.ChildrenEntry = nextid;

        if (nextid != 0) 
            ReconEntityRegistry.GetEntity(nextid)!.hierarchyData.SiblingBefore = previd;
        else hierarchyData.ChildrenExit = previd;
        
        entity.hierarchyData.ParentId = 0;
        entity.hierarchyData.SiblingAfter = 0;
        entity.hierarchyData.SiblingBefore = 0;
    }
    public virtual void Destroy()
    {
        uint currentId = hierarchyData.ChildrenEntry;
        while (currentId != 0)
        {
            var child = ReconEntityRegistry.GetEntity(currentId);
            uint nextId = child?.hierarchyData.SiblingAfter ?? 0;
            child?.Destroy();
            currentId = nextId;
        }
        Parent?.RemoveChild(this);
        ReconEntityRegistry.FreeId(EntityId);
    }

    public ReconEntity? FindChild(string targetName)
    {
        int targetHash = targetName.GetHashCode(StringComparison.CurrentCulture);
        _childCache.TryGetValue(targetHash, out ReconEntity? cachedEntity);
        if (cachedEntity != null) return cachedEntity;
        foreach (var child in Children)
        {
            if (child.namehash == targetHash && child.Name == targetName)
            {
                _childCache[targetHash] = child;
                return child;
            }
        }
        return null;
    }
    public ReconEntity? FindChild(string targetName, bool recursive)
    {
        if (recursive) return FindDescendant(targetName);
        else return FindChild(targetName);
    }
    public ReconEntity? FindAncestor(string targetName)
    {
        int targetHash = targetName.GetHashCode(StringComparison.CurrentCulture);
        _ancestorCache.TryGetValue(targetHash, out ReconEntity? cachedEntity);
        if (cachedEntity != null) return cachedEntity;
        ReconEntity? current = Parent;
        while (current != null)
        {
            if (current.namehash == targetHash && current.Name == targetName)
            {
                _ancestorCache[targetHash] = current;
                return current;
            }
            current = current.Parent;
        }
        return null;
    }
    public ReconEntity? FindDescendant(string targetName)
    {
        int targetHash = targetName.GetHashCode(StringComparison.CurrentCulture);
        _descCache.TryGetValue(targetHash, out ReconEntity? cachedEntity);
        if (cachedEntity != null) return cachedEntity;
        ReconEntity? desc = SearchRecursive(this, targetName, targetHash);
        if (desc != null) _descCache[targetHash] = desc;
        return desc;
    }
    private static ReconEntity? SearchRecursive(ReconEntity current, string name, int hash)
    {
        foreach (var child in current.Children)
        {
            if (child.namehash == hash && child.Name == name)
                return child;

            var found = SearchRecursive(child, name, hash);
            if (found != null) return found;
        }
        return null;
    }
    public ReconEntity? FindByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        ReconEntity? current = this;
        foreach (string part in parts)
        {
            current = current.FindChild(part);
            if (current == null) return null;
        }
        return current;
    }
    public virtual void ResetCache(CacheResetDirection direction)
    {
        _isPathDirty = true;
        if (direction == CacheResetDirection.Up || direction == CacheResetDirection.Both)
        {
            _childCache.Clear();
            _descCache.Clear();
            Parent?.ResetCache(CacheResetDirection.Up);
        }
        if (direction == CacheResetDirection.Down || direction == CacheResetDirection.Both)
        {
            _ancestorCache.Clear();
            foreach (var child in Children)
            {
                child.ResetCache(CacheResetDirection.Down);
            }
        }
    }

    public virtual void RenderStep(float deltaTime, IRenderer renderer)
    {
        foreach (ReconEntity entity in Children) entity.RenderStep(deltaTime, renderer);
    }
    public virtual void PhysicsStep(float deltaTime)
    {
        foreach (ReconEntity entity in Children) entity.PhysicsStep(deltaTime);
    }
    public virtual void Ready() {}
    
    public virtual ReconEntity Clone()
    {
        var clone = (ReconEntity)Activator.CreateInstance(this.GetType())!;
        clone.Name = this.Name;
        foreach (var child in this.Children)
        {
            var childClone = child.Clone();
            clone.AddChild(childClone);
        }
        return clone;
    }
}