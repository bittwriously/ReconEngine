
namespace ReconEngine.MeshSystem;

public class ReconMeshRegistry
{
    private readonly List<ReconMesh> _meshes = [];

    public void RegisterMesh(ReconMesh mesh)
    {
        if (_meshes.Contains(mesh)) return;
        _meshes.Add(mesh);
    }
    public void RemoveMesh(ReconMesh mesh)
    {
        if (!_meshes.Contains(mesh)) return;
        _meshes.Remove(mesh);
    }

    public void DrawAllMeshes(IRenderer renderer)
    {
        foreach (ReconMesh mesh in _meshes) mesh.Draw(renderer);
    }
}