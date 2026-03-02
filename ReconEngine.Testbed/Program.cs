using System.Numerics;
using ReconEngine;
using ReconEngine.Entities;
using ReconEngine.Entities.Constraints;
using ReconEngine.MeshSystem;
using ReconEngine.Serialization;
using ReconEngine.System3D;
using ReconEngine.UISystem;

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
            var gui = PrefabSerializer.LoadFromFile("assets/tinygui.inst") as GuiContainer;
            gui?.Parent = ReconCore.MainWorld.Root;
            TreePrinter.PrintTree(gui);
        };

        ReconCore.Run();
    }
}
