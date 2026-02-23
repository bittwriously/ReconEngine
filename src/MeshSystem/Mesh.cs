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
    public string TextureId
    {
        get => _texturePath;
        set
        {
            _textureId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Texture);;
            _texturePath = value;
        }
    }

    public uint _meshId = 0;
    public string _meshPath = "";

    public uint _textureId = 0;
    public string _texturePath = "";

    public void Draw(IRenderer renderer)
    {
        renderer.DrawModel(_meshId, _textureId, GlobalPosition, GlobalRotation, Size);
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