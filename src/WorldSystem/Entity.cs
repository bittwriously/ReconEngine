namespace ReconEngine.WorldSystem;

public static class ReconEntityRegistry
{
    private readonly static Dictionary<uint, ReconEntity> _registry = [];
    private static uint _idcounter = 0;
    private static readonly List<uint> _freeids = [];
    private static readonly Lock _lock = new();

    public static ReconEntity? GetEntity(uint id)
    {
        ReconEntity? entity;
        lock (_lock) { _registry.TryGetValue(id, out entity); }
        return entity;
    }
    public static void RegisterEntity(ReconEntity entity)
    {
        lock (_lock)
        {
            if (_registry.ContainsKey(entity.EntityId)) return;
            _registry.Add(entity.EntityId, entity);
        }
    }
    public static uint GetNextId()
    {
        lock (_lock)
        {
            if (_freeids.Count > 0)
            {
                int lastIndex = _freeids.Count - 1;
                uint id = _freeids[lastIndex];
                _freeids.RemoveAt(lastIndex);
                return id;
            }
            return ++_idcounter;
        }
    }
    public static void FreeId(uint id)
    {
        lock (_lock)
        {
            _registry.Remove(id);
            _freeids.Add(id);
        }
    }
}

public struct HierarchyData
{
    public HierarchyData() { }
    public uint ParentId = 0; // 0 means no node is here
    public uint SiblingBefore = 0;
    public uint SiblingAfter = 0;
    public uint ChildrenEntry = 0;
    public uint ChildrenExit = 0;
    public int CachedDepth = -1; // -1 indicates dirty
    public int CachedSiblingIndex = -1;
}

public enum CacheResetDirection
{
    Up,      // clear ancestors (use when this entity moved to a new parent)
    Down,    // clear descendants (use when this entity's children changed)
    Both     // full branch reset
}

public class ReconEntity : IUpdatable
{
    public event EventHandler<ReconEntity?>? ParentChanged;
    public event EventHandler<ReconWorld?>? AncestryChanged;
    public event EventHandler<ReconEntity>? ChildAdded;
    public event EventHandler<ReconEntity>? ChildRemoved;

    public readonly uint EntityId;
    public ReconEntity? Parent
    {
        get
        {
            if (_hierarchyData.ParentId == 0) return null;
            return ReconEntityRegistry.GetEntity(_hierarchyData.ParentId);
        }
        set
        {
            ReconEntity? prevParent = Parent;
            prevParent?.RemoveChild(this);
            if (value == null)
            {
                _hierarchyData.ParentId = 0;
                _currentWorld = null;
            }
            else
            {
                _hierarchyData.ParentId = value.EntityId;
                value.AddChild(this);
                _currentWorld = value.CurrentWorld;
            }
            ResetCache(CacheResetDirection.Both);
            prevParent?.ResetCache(CacheResetDirection.Both);
            ParentChanged?.Invoke(this, prevParent);
            AncestryChanged?.Invoke(this, prevParent?.CurrentWorld);
            foreach (ReconEntity entity in Descendants) entity.AncestryChanged?.Invoke(this, prevParent?.CurrentWorld);
        }
    }
    public ReconWorld? CurrentWorld { get => _currentWorld; }
    public string Name
    {
        get { return _name; }
        set
        {
            _name = value;
            _namehash = value.GetHashCode(StringComparison.CurrentCulture);
            ResetCache(CacheResetDirection.Both);
        }
    }
    public IEnumerable<ReconEntity> Children
    {
        get
        {
            uint currentId = _hierarchyData.ChildrenEntry;
            while (currentId != 0)
            {
                var entity = ReconEntityRegistry.GetEntity(currentId);
                if (entity == null) yield break;
                yield return entity;
                currentId = entity._hierarchyData.SiblingAfter;
            }
        }
    }
    public IEnumerable<ReconEntity> Descendants
    {
        get
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.Descendants)
                {
                    yield return descendant;
                }
            }
        }
    }
    public string ClassName
    {
        get { return GetType().Name; }
    }
    public bool IsActive = true;

    public int HierarchyDepth
    {
        get
        {
            if (_hierarchyData.CachedDepth != -1) return _hierarchyData.CachedDepth;

            if (Parent == null) _hierarchyData.CachedDepth = 0;
            else _hierarchyData.CachedDepth = Parent.HierarchyDepth + 1;

            return _hierarchyData.CachedDepth;
        }
    }

    public int SiblingIndex
    {
        get
        {
            if (_hierarchyData.CachedSiblingIndex != -1) return _hierarchyData.CachedSiblingIndex;

            if (_hierarchyData.SiblingBefore == 0)
                _hierarchyData.CachedSiblingIndex = 0;
            else
            {
                var prev = ReconEntityRegistry.GetEntity(_hierarchyData.SiblingBefore);
                _hierarchyData.CachedSiblingIndex = (prev != null) ? prev.SiblingIndex + 1 : 0;
            }

            return _hierarchyData.CachedSiblingIndex;
        }
    }

    private int _namehash = 0;
    private string _name = "";
    private HierarchyData _hierarchyData = new();

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
        uint lastid = _hierarchyData.ChildrenExit;

        if (_hierarchyData.ChildrenEntry == 0) _hierarchyData.ChildrenEntry = entity.EntityId;
        if (lastid != 0) ReconEntityRegistry.GetEntity(lastid)?._hierarchyData.SiblingAfter = entity.EntityId;

        _hierarchyData.ChildrenExit = entity.EntityId;
        entity._hierarchyData.ParentId = EntityId;
        entity._hierarchyData.SiblingBefore = lastid;
        entity._hierarchyData.SiblingAfter = 0;

        ChildAdded?.Invoke(this, entity);
    }
    public virtual void RemoveChild(ReconEntity entity)
    {
        uint nextid = entity._hierarchyData.SiblingAfter;
        uint previd = entity._hierarchyData.SiblingBefore;

        if (previd != 0)
            ReconEntityRegistry.GetEntity(previd)!._hierarchyData.SiblingAfter = nextid;
        else _hierarchyData.ChildrenEntry = nextid;

        if (nextid != 0)
            ReconEntityRegistry.GetEntity(nextid)!._hierarchyData.SiblingBefore = previd;
        else _hierarchyData.ChildrenExit = previd;

        entity._hierarchyData.ParentId = 0;
        entity._hierarchyData.SiblingAfter = 0;
        entity._hierarchyData.SiblingBefore = 0;

        ChildRemoved?.Invoke(this, entity);
    }
    public virtual void Destroy()
    {
        uint currentId = _hierarchyData.ChildrenEntry;
        while (currentId != 0)
        {
            var child = ReconEntityRegistry.GetEntity(currentId);
            uint nextId = child?._hierarchyData.SiblingAfter ?? 0;
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
            if (child._namehash == targetHash && child.Name == targetName)
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
            if (current._namehash == targetHash && current.Name == targetName)
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
            if (child._namehash == hash && child.Name == name)
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
        _hierarchyData.CachedDepth = -1;
        _hierarchyData.CachedSiblingIndex = -1;
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
    public virtual void Ready() { }

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
