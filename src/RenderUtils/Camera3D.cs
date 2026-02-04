using System.Numerics;
using Raylib_cs;

namespace ReconEngine.RenderUtils;

public class ReconCamera3D
{
    public Vector3 Position
    {
        get => RawCamera.Position;
        set => RawCamera.Position = value;
    }
    public Vector3 Target
    {
        get => RawCamera.Target;
        set => RawCamera.Target = value;
    }
    public Vector3 UpDir
    {
        get => RawCamera.Up;
        set => RawCamera.Up = value;
    }
    public float FovY
    {
        get => RawCamera.FovY;
        set => RawCamera.FovY = value;
    }
    public bool Perpective
    {
        get => RawCamera.Projection == CameraProjection.Perspective;
        set => RawCamera.Projection = value ? CameraProjection.Perspective : CameraProjection.Orthographic;
    }

    public Camera3D RawCamera;

    public ReconCamera3D(Vector3 position, Vector3 target)
    {
        RawCamera = new Camera3D
        {
            Position = position,
            Target = target,
            Up = Vector3.UnitY,
            FovY = 75.0f,
            Projection = CameraProjection.Perspective
        };
    }

    public void Update()
    {
        Raylib.UpdateCamera(ref RawCamera, CameraMode.Free);
    }
}