using System.Buffers;
using MessagePack;
using ReconEngine.WorldSystem;

namespace ReconEngine.Serialization;

public static class PrefabSerializer
{
    private static readonly MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard;

    public static byte[] Serialize(ReconEntity root, string prefabName = "")
    {
        var entityToLocalId = new Dictionary<uint, uint>();
        uint localIdCounter = 1; // 0 is for null

        foreach (var entity in Flatten(root))
            entityToLocalId[entity.EntityId] = localIdCounter++;

        var ctx = new PrefabWriteContext(entityToLocalId);
        var serializedEntities = new List<SerializedEntity>();

        foreach (var entity in Flatten(root))
        {
            var se = new SerializedEntity
            {
                LocalId = entityToLocalId[entity.EntityId],
                TypeName = PrefabTypeRegistry.GetTypeName(entity),
                Name = entity.Name,
            };

            foreach (var child in entity.Children)
                se.ChildLocalIds.Add(entityToLocalId[child.EntityId]);

            se.Properties = EntityPropertySerializer.Serialize(entity, ctx);

            serializedEntities.Add(se);
        }

        var prefab = new PrefabData
        {
            PrefabName = prefabName,
            RootLocalId = entityToLocalId[root.EntityId],
            Entities = serializedEntities,
        };

        return MessagePackSerializer.Serialize(prefab, _options);
    }

    public static ReconEntity? Deserialize(byte[] data)
    {
        var prefab = MessagePackSerializer.Deserialize<PrefabData>(data, _options);

        var localIdToEntity = new Dictionary<uint, ReconEntity>();
        var ctx = new PrefabReadContext(localIdToEntity);
        foreach (var se in prefab.Entities)
        {
            var entity = PrefabTypeRegistry.Create(se.TypeName);
            if (entity == null) continue;

            entity.Name = se.Name;
            localIdToEntity[se.LocalId] = entity;
            if (se.Properties.Length > 0)
                EntityPropertySerializer.Deserialize(entity, se.Properties, ctx);
        }
        foreach (var se in prefab.Entities)
        {
            if (!localIdToEntity.TryGetValue(se.LocalId, out var parent)) continue;
            foreach (var childId in se.ChildLocalIds)
            {
                if (localIdToEntity.TryGetValue(childId, out var child))
                    parent.AddChild(child);
            }
        }
        ctx.FlushReferences();

        localIdToEntity.TryGetValue(prefab.RootLocalId, out var root);
        return root;
    }

    public static void SaveToFile(ReconEntity root, string path, string prefabName = "")
        => File.WriteAllBytes(path, Serialize(root, prefabName));

    public static ReconEntity? LoadFromFile(string path)
        => Deserialize(File.ReadAllBytes(path));

    private static IEnumerable<ReconEntity> Flatten(ReconEntity root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var desc in Flatten(child))
                yield return desc;
    }

    private static byte[] GetWriterBytes(ref MessagePackWriter writer)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var w = new MessagePackWriter(buffer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}
