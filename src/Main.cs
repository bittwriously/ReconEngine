using System.Numerics;
using Raylib_cs;
using ReconEngine.InputSystem;
using ReconEngine.MeshSystem;
using ReconEngine.NetworkingServer;
using ReconEngine.RenderUtils;
using ReconEngine.UISystem;
using ReconEngine.WorldSystem;

namespace ReconEngine;

internal static class ReconCore
{
    public static IRenderer Renderer = new RenderingEngines.RaylibRenderer();
    public static ReconWorld MainWorld = null!;
    public static ReconNetCatServer? CurrentServer = null;

    [STAThread]
    public static void Main()
    {
        ReconCamera3D camera = new(new Vector3(5.0f, 0.0f, 0.0f), Vector3.Zero);

        Renderer.InitWindow(800, 600, "ReconEngine");

        MainWorld = new("MainWorld");

        double physicsAccumulator = 0.0;
        const double physicsFrametime = 1.0 / 20.0;

        double serverAccumulator = 0.0;
        // create a new server for testing!
        CurrentServer = new();
        CurrentServer.Start(18100);

        ReconMesh mesh = new();
        mesh.MeshId = "assets/models/utah_teapot_new.obj";
        mesh.TextureId = "assets/textures/utahgrid.png";
        mesh.Parent = MainWorld.Root;
        ReconMesh floor = new();
        floor.MeshId = "assets/models/cube.obj";
        floor.TextureId = "assets/textures/utahgrid.png";
        floor.Size = new(16, 1, 16);
        floor.Position = new(0, -4, 0);
        floor.Static = true;
        floor.Parent = MainWorld.Root;

        while (!Renderer.ShouldClose())
        {
            camera.Update();

            float deltaTime = Renderer.GetFrameTime();

            /// PHYSICS ///
            physicsAccumulator += MathF.Min(deltaTime, 0.25f);
            if (physicsAccumulator >= physicsFrametime)
            {
                // do physics here
                physicsAccumulator -= physicsFrametime;
                MainWorld.Root.PhysicsStep((float)physicsFrametime);
                MainWorld.PhysicsEngine.Update((float)physicsFrametime);
            }
            /// PHYSICS ///
            
            /// SERVER ///
            if (CurrentServer != null)
            {
                serverAccumulator += deltaTime;
                if (serverAccumulator > CurrentServer.UPDATE_TIME)
                {
                    serverAccumulator -= CurrentServer.UPDATE_TIME;
                    CurrentServer.Update();
                }
            }
            /// SERVER ///

            Renderer.BeginFrame();

            Renderer.ClearBuffer();

            /// 3D ///
            Renderer.BeginMode(camera);
            MainWorld.WorldMeshRegistry.DrawAllMeshes(Renderer);
            Renderer.EndMode();
            /// 3D ///

            /// INPUT SYSTEM ///
            ReconInputSystem.UpdateAll();
            /// INPUT SYSTEM ///

            /// RENDER CALL ///
            MainWorld.Root.RenderStep(deltaTime, Renderer);
            /// RENDER CALL ///

            /// UI DRAW CALLS ///
            MainWorld.WorldGuiRegistry.DrawContainers(Renderer);
            /// UI DRAW CALLS ///

            Raylib.DrawFPS(16, 16);

            Renderer.EndFrame();
        }

        Renderer.CloseWindow();
    }
}
