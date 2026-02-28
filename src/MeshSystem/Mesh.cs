using System.Numerics;
using ReconEngine.System3D;

namespace ReconEngine.MeshSystem;

/*

    Box = 0,
    Sphere = 1,
    Cylinder = 2,
    Plane = 3,
    Cone = 4,
*/
public enum MeshShapeType
{
    Box = 0,
    Sphere = 1,
    Cylinder = 2,
    Plane = 3,
    Cone = 4,

    FileMesh = 100,
}

public class ReconMesh : PhysicsEntity
{
    private Vector3 _size = Vector3.One;
    public Vector3 Size
    {
        get => _size;
        set
        {
            _size = value;
            Shape = CurrentWorld?.PhysicsEngine.GetBoxShape(value);
        }
    }
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

    public MeshShapeType ShapeType = MeshShapeType.Box;

    private Vector3 _lastPhysPos = Vector3.Zero;
    private Quaternion _lastPhysRot = Quaternion.Identity;
    private double _lastPhysTime = ReconCore.RunningTime;
    private readonly double _invPhysTime = 1 / ReconCore.PhysicsFrametime;

    public uint _meshId = 0;
    public string _meshPath = "";

    public uint _textureId = 0;
    public string _texturePath = "";

    public void Draw(IRenderer renderer, bool depth = false)
    {
        float alpha = (float)((ReconCore.RunningTime - _lastPhysTime) * _invPhysTime);
        Vector3 lerpedPos = ReconMath.Lerp(_lastPhysPos, Position, alpha);
        Quaternion lerpedRot = ReconMath.Lerp(_lastPhysRot, Rotation, alpha);
        if (ShapeType == MeshShapeType.FileMesh)
        {
            if (depth) renderer.DrawModelDepth(_meshId, lerpedPos, lerpedRot, Size);
            else renderer.DrawModel(_meshId, _textureId, lerpedPos, lerpedRot, Size);
        } else
        {
            if (depth) renderer.DrawShapeDepth((ReconShape3D)ShapeType, lerpedPos, lerpedRot, Size);
            else renderer.DrawShape((ReconShape3D)ShapeType, _textureId, lerpedPos, lerpedRot, Size);
        }
    }

    public override void PhysicsStep(float deltaTime)
    {
        base.PhysicsStep(deltaTime);

        _lastPhysTime = ReconCore.RunningTime;
        _lastPhysPos = Position;
        _lastPhysRot = Rotation;
    }

    public override void Ready()
    {
        base.Ready();

        AncestryChanged += (sender, oldWorld) =>
        {
            oldWorld?.WorldMeshRegistry.RemoveMesh(this);
            CurrentWorld?.WorldMeshRegistry.RegisterMesh(this);
            Shape = CurrentWorld?.PhysicsEngine.GetBoxShape(Size);
        };
    }
}
