namespace ReconEngine;

public enum ResourceAssetType
{
    Texture,
    Model,
    Font,
    Sound,
}

public static class DynamicResouceLoader
{
    private static readonly Dictionary<string, uint> _resourceList = [];

    public static uint LoadAsset(string path, ResourceAssetType type)
    {
        bool exists = _resourceList.TryGetValue(path, out uint existingId);
        if (exists) return existingId;
        uint newId = type switch
        {
            ResourceAssetType.Texture => ReconCore.Renderer.RegisterTexture(path),
            ResourceAssetType.Model => ReconCore.Renderer.RegisterMesh(path),
            ResourceAssetType.Font => ReconCore.Renderer.RegisterFont(path),
            ResourceAssetType.Sound => 0, //TODO: implement
            _ => 0,
        };
        _resourceList[path] = newId;
        return newId;
    }

    public static bool IsLoaded(string path)
    {
        return _resourceList.ContainsKey(path);
    }
}
