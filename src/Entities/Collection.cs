using ReconEngine.WorldSystem;

namespace ReconEngine.Entities;

public class Collection : ReconEntity
{
    private readonly Dictionary<string, ReconEntity> _namedChildren = new();
    private readonly Dictionary<ReconEntity, Action> _nameHandlers = new();

    public override void AddChild(ReconEntity entity)
    {
        if (_namedChildren.ContainsKey(entity.Name))
            throw new InvalidOperationException(
                $"[Collection] '{Name}' already has a child named '{entity.Name}'. children must have unique names.");

        _namedChildren[entity.Name] = entity;

        Action handler = () =>
        {
            foreach (var key in _namedChildren.Where(kv => kv.Value == entity).Select(kv => kv.Key).ToList())
                _namedChildren.Remove(key);

            if (_namedChildren.ContainsKey(entity.Name))
                throw new InvalidOperationException(
                    $"[Folder] '{Name}' already has a child named '{entity.Name}'.");

            _namedChildren[entity.Name] = entity;
        };

        _nameHandlers[entity] = handler;
        entity.GetPropertyChangedSignal(nameof(entity.Name)) += handler;

        base.AddChild(entity);
    }

    public override void RemoveChild(ReconEntity entity)
    {
        _namedChildren.Remove(entity.Name);
        if (_nameHandlers.TryGetValue(entity, out var handler))
        {
            entity.GetPropertyChangedSignal(nameof(entity.Name)) -= handler;
            _nameHandlers.Remove(entity);
        }
        base.RemoveChild(entity);
    }

    public ReconEntity? Get(string name)
    {
        _namedChildren.TryGetValue(name, out var entity);
        return entity;
    }

    public T? Get<T>(string name) where T : ReconEntity
        => Get(name) as T;

    public bool Has(string name) => _namedChildren.ContainsKey(name);
}
