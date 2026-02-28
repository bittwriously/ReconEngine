using ReconEngine.WorldSystem;

namespace ReconEngine.Serialization;

public class PrefabWriteContext(Dictionary<uint, uint> entityToLocalId)
{
    private readonly Dictionary<uint, uint> _entityToLocalId = entityToLocalId;

    public uint GetLocalId(ReconEntity? entity)
    {
        if (entity == null) return 0;
        _entityToLocalId.TryGetValue(entity.EntityId, out uint localId);
        return localId;
    }
}

public class PrefabReadContext(Dictionary<uint, ReconEntity> localIdToEntity)
{
    private readonly Dictionary<uint, ReconEntity> _localIdToEntity = localIdToEntity;
    private readonly List<(Action<ReconEntity> setter, uint localId)> _pendingRefs = new();

    public void ResolveRef<T>(uint localId, Action<T> setter) where T : ReconEntity
    {
        if (localId == 0) return;
        _pendingRefs.Add((e => setter((T)e), localId));
    }
    public void FlushReferences()
    {
        foreach (var (setter, localId) in _pendingRefs)
        {
            if (_localIdToEntity.TryGetValue(localId, out var entity))
                setter(entity);
            else
                Console.WriteLine($"[PREFAB] WARNING: could not resolve reference to local ID {localId}");
        }
        _pendingRefs.Clear();
    }
}
