using System.Numerics;
using ReconEngine;
using ReconEngine.Entities;
using ReconEngine.Entities.Constraints;
using ReconEngine.MeshSystem;
using ReconEngine.System3D;

internal static class Testbed
{
    [STAThread]
    public static void Main()
    {
        ReconMesh? mesh1 = new ReconMesh();
        ReconCore.Ready += () =>
        {
            var env = new WorldEnvironment();
            var sky = new HDRISky("assets/skies/citrus_orchard_road_puresky_2k.hdr", 1024);
            env.Sky = sky;
            env.Activate();
            var camera = new ReconCamera3D
            {
                Mode = CameraMode.Freecam,
                Parent = ReconCore.MainWorld.Root,
            };
            mesh1 = new()
            {
                MeshId = "assets/models/utah_teapot_new.obj",
                TextureId = "assets/textures/utahgrid.png",
                ShapeType = MeshShapeType.FileMesh,
                Size = new(6.43f, 3.15f, 4.0f),
                Position = Vector3.Zero,
                Static = false
            };
            mesh1.Parent = ReconCore.MainWorld.Root;
            var mesh2 = new ReconMesh()
            {
                MeshId = "assets/models/utah_teapot_new.obj",
                TextureId = "assets/textures/utahgrid.png",
                ShapeType = MeshShapeType.FileMesh,
                Size = new(6.43f, 3.15f, 4.0f),
                Position = Vector3.Zero,
                Static = false,
                Parent = ReconCore.MainWorld.Root,
            };
            _ = new PhysicsWeldConstraint()
            {
                EntityA = mesh1,
                EntityB = mesh2,
                MatrixB = Matrix4x4.CreateTranslation(new Vector3(0, 5, 0)),
                Enabled = true,
                Parent = ReconCore.MainWorld.Root,
                DrawConstraint = true,
            };
            _ = new ReconMesh()
            {
                TextureId = "assets/textures/utahgrid.png",
                Size = new(16, 1, 16),
                Position = new(0, -4, 0),
                Static = true,
                Parent = ReconCore.MainWorld.Root
            };
            var sun = new SunLight()
            {
                Direction = new(-1, -0.4f, -0.57f),
                Enabled = true,
                Parent = ReconCore.MainWorld.Root
            };
            ReconCore.SetSun(sun);
        };

        ReconCore.Run();
    }
}
