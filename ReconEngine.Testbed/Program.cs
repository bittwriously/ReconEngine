using System.Numerics;
using ReconEngine;
using ReconEngine.MeshSystem;
using ReconEngine.RenderUtils;
using ReconEngine.System3D;

internal static class Testbed
{
    [STAThread]
    public static void Main()
    {

        ReconCore.Ready += () =>
        {
            var camera = new ReconCamera3D(new Vector3(5f, 0f, 0f), Vector3.Zero);
            ReconCore.SetCamera(camera);
            _ = new ReconMesh()
            {
                MeshId = "assets/models/utah_teapot_new.obj",
                TextureId = "assets/textures/utahgrid.png",
                Size = new(6.43f, 3.15f, 4.0f),
                Position = Vector3.Zero,
                Static = false,
                Parent = ReconCore.MainWorld.Root,
            };
            _ = new ReconMesh()
            {
                MeshId = "assets/models/cube.obj",
                TextureId = "assets/textures/utahgrid.png",
                Size = new(16, 1, 16),
                Position = new(0, -4, 0),
                Static = true,
                Parent = ReconCore.MainWorld.Root
            };
            var sun = new SunLight()
            {
                Direction = new(1, -1, 0),
                Enabled = true,
                Parent = ReconCore.MainWorld.Root
            };
            ReconCore.SetSun(sun);
        };

        ReconCore.Run();
    }
}
