using System.Numerics;
using ReconEngine.InputSystem;
using ReconEngine.MeshSystem;
using ReconEngine.NetworkingServer;
using ReconEngine.RenderingEngines;
using ReconEngine.RenderUtils;
using ReconEngine.SoundSystem;
using ReconEngine.SoundSystem.SoundProviders;
using ReconEngine.System3D;
using ReconEngine.UISystem;
using ReconEngine.WorldSystem;

namespace ReconEngine;

public static class ReconCore
{
    public static IRenderer Renderer = new RaylibRenderer();
    public static ISoundProvider SoundProvider = new MiniAudioSoundProvider();
    public static ReconWorld MainWorld = null!;
    public static ReconNetCatServer? CurrentServer = null;
    public static double RunningTime { get; private set; } = 0.0;
    public static readonly double PhysicsFrametime = 1.0 / 20.0;

    [STAThread]
    public static void Main()
    {
        ReconCamera3D camera = new(new Vector3(5.0f, 0.0f, 0.0f), Vector3.Zero);

        Renderer.InitWindow(800, 600, "ReconEngine");
        SoundProvider.Initialize();
        IShadowRenderer shadowMapRenderer = Renderer.GetShadowMapRenderer();

        MainWorld = new("MainWorld");

        double physicsAccumulator = 0.0;
        double serverAccumulator = 0.0;

        // create a new server for testing!
        CurrentServer = new();
        CurrentServer.Start(18100);

        _ = new ReconMesh()
        {
            MeshId = "assets/models/utah_teapot_new.obj",
            TextureId = "assets/textures/utahgrid.png",
            Size = new(6.43f, 3.15f, 4.0f),
            Position = new(0, 0, 0),
            //Rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI*.5f, 0, 0),
            Static = false,
            Parent = MainWorld.Root,
        };
        _ = new ReconMesh()
        {
            MeshId = "assets/models/cube.obj",
            TextureId = "assets/textures/utahgrid.png",
            Size = new(16, 1, 16),
            Position = new(0, -4, 0),
            Static = true,
            Parent = MainWorld.Root
        };
        (new ReconSound3D()
        {
            Sound = "assets/sounds/danteh.mp3",
            IsLooped = true,
            Parent = MainWorld.Root,
        }).Play();
        var sun = new SunLight()
        {
            Direction = new(1, -1, 0),
            Enabled = true,
            Parent = MainWorld.Root
        };

        while (!Renderer.ShouldClose())
        {
            camera.Update();

            float deltaTime = Renderer.GetFrameTime();
            RunningTime += deltaTime;

            // PHYSICS //
            physicsAccumulator += MathF.Min(deltaTime, 0.25f);
            if (physicsAccumulator >= PhysicsFrametime)
            {
                // do physics here
                physicsAccumulator -= PhysicsFrametime;
                MainWorld.Root.PhysicsStep((float)PhysicsFrametime);
                MainWorld.PhysicsEngine.Update((float)PhysicsFrametime);
            }

            // SERVER //
            if (CurrentServer != null)
            {
                serverAccumulator += deltaTime;
                if (serverAccumulator > CurrentServer.UPDATE_TIME)
                {
                    serverAccumulator -= CurrentServer.UPDATE_TIME;
                    CurrentServer.Update();
                }
            }

            // AUDIO UPDATE //
            SoundProvider.Update(deltaTime, camera.Position, Vector3.Normalize(camera.Target - camera.Position));

            Renderer.BeginFrame();
            Renderer.ClearBuffer();

            // RENDER CALL //
            MainWorld.Root.RenderStep(deltaTime, Renderer);

            // SHADOWMAP //
            shadowMapRenderer.UpdateSun(sun.Definition);
            shadowMapRenderer.UpdateMatrices(camera.Position);
            for (int i = 0; i < 4; i++)
            {
                shadowMapRenderer.BeginCascade(i);
                MainWorld.WorldMeshRegistry.DrawAllMeshes(Renderer, true);
                shadowMapRenderer.EndCascade();
            }

            // 3D //
            Renderer.BeginMode(camera);
            MainWorld.WorldMeshRegistry.DrawAllMeshes(Renderer);
            Renderer.EndMode();

            // INPUT SYSTEM //
            ReconInputSystem.UpdateAll();

            // UI DRAW CALLS //
            MainWorld.WorldGuiRegistry.DrawContainers(Renderer);

            Renderer.EndFrame();
        }

        Renderer.CloseWindow();
        SoundProvider.Deinitialize();
    }
}
