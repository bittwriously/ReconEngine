using System.Numerics;
using ReconEngine.System3D;

namespace ReconEngine.MeshSystem;

public class ReconMesh : ReconEntity3D
{
    public Vector3 Size = Vector3.One;
    public string MeshId
    {
        get => _meshPath;
        set
        {
            _meshId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Model);;
            _meshPath = value;
        }
    }

    public uint _meshId = 1;
    public string _meshPath = "";

    public void Draw(IRenderer renderer)
    {
        renderer.DrawModel(_meshId, GlobalPosition, 1);
    }

    public override void Ready()
    {
        base.Ready();

        ParentChanged += (sender, oldParent) =>
        {
            oldParent?.CurrentWorld?.WorldMeshRegistry.RemoveMesh(this);
            CurrentWorld?.WorldMeshRegistry.RegisterMesh(this);
        };
    }
}