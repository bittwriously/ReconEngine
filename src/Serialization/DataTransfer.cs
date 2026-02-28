using MessagePack;

namespace ReconEngine.Serialization;

[MessagePackObject]
public class SerializedEntity
{
    [Key(0)] public uint LocalId;
    [Key(1)] public string TypeName = "";
    [Key(2)] public string Name = "";
    [Key(3)] public byte[] Properties = [];
    [Key(4)] public List<uint> ChildLocalIds = [];
}

[MessagePackObject]
public class PrefabData
{
    [Key(0)] public string PrefabName = "";
    [Key(1)] public uint RootLocalId;
    [Key(2)] public List<SerializedEntity> Entities = [];
}
