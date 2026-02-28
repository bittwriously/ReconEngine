using ReconEngine.WorldSystem;

namespace ReconEngine.Serialization;

public static class PrefabTypeRegistry
{
    private static readonly Dictionary<string, Func<ReconEntity>> _factories = [];

    public static void AutoRegister()
    {
        var entityType = typeof(ReconEntity);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                if (!type.IsClass) continue;
                if (type.IsAbstract) continue;
                if (!entityType.IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                string typeName = type.FullName!;
                _factories[typeName] = () => (ReconEntity)Activator.CreateInstance(type)!;
            }
        }

        Console.WriteLine($"[PREFAB_TYPE_REGISTRY] Auto-registered {_factories.Count} entity types");
    }

    public static void Register<T>(Func<T> factory) where T : ReconEntity
    {
        string typeName = typeof(T).FullName!;
        _factories[typeName] = () => factory();
        Console.WriteLine($"[PREFAB_TYPE_REGISTRY] Registered: {typeName}");
    }

    public static ReconEntity? Create(string typeName)
    {
        if (_factories.TryGetValue(typeName, out var factory))
            return factory();

        Console.WriteLine($"[PREFAB_TYPE_REGISTRY] WARNING: No factory for type '{typeName}'");
        return null;
    }

    public static string GetTypeName(ReconEntity entity) =>
        entity.GetType().FullName!;
}
