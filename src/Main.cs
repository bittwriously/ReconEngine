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

public sealed class ReconEngineConfig
{
    public int WindowWidth = 800;
    public int WindowHeight = 600;
    public string WindowTitle = "ReconEngine";
    public double PhysicsFramerate = 20.0;
    public IRenderer? Renderer;
    public ISoundProvider? SoundProvider;
}

public static class ReconCore
{
    public static IRenderer Renderer { get; private set; } = null!;
    public static ISoundProvider SoundProvider { get; private set; } = null!;
    public static ReconWorld MainWorld { get; private set; } = null!;
    public static double RunningTime { get; private set; } = 0.0;
    public static double PhysicsFrametime { get; private set; } = 1.0 / 20.0;

    public static event Action? Ready;
    public static event Action<float>? Update;
    public static event Action<float>? PhysicsUpdate;
    public static event Action<float>? Render;
    public static event Action? Shutdown;

    
    public static void Run(ReconEngineConfig? config = null)
    {
        config ??= new ReconEngineConfig();
        PhysicsFrametime = 1.0 / config.PhysicsFramerate;

        Renderer      = config.Renderer      ?? new RaylibRenderer();
        SoundProvider = config.SoundProvider ?? new MiniAudioSoundProvider();

        Renderer.InitWindow(config.WindowWidth, config.WindowHeight, config.WindowTitle);
        SoundProvider.Initialize();

        IShadowRenderer shadowMapRenderer = Renderer.GetShadowMapRenderer();

        MainWorld = new ReconWorld("MainWorld");

        Ready?.Invoke();

        ReconCamera3D camera = _camera ?? new ReconCamera3D(new Vector3(5f, 5f, 5f), Vector3.Zero);

        double physicsAccumulator = 0.0;

        while (!Renderer.ShouldClose())
        {
            camera.Update();

            float deltaTime = Renderer.GetFrameTime();
            RunningTime += deltaTime;

            // --- PHYSICS ---
            physicsAccumulator += MathF.Min(deltaTime, 0.25f);
            if (physicsAccumulator >= PhysicsFrametime)
            {
                physicsAccumulator -= PhysicsFrametime;
                float physDt = (float)PhysicsFrametime;
                MainWorld.Root.PhysicsStep(physDt);
                MainWorld.PhysicsEngine.Update(physDt);
                PhysicsUpdate?.Invoke(physDt);
            }

            // --- UPDATE ---
            Update?.Invoke(deltaTime);

            // --- AUDIO ---
            SoundProvider.Update(deltaTime, camera.Position, Vector3.Normalize(camera.Target - camera.Position));

            // --- RENDER ---
            Renderer.BeginFrame();
            Renderer.ClearBuffer();

            MainWorld.Root.RenderStep(deltaTime, Renderer);

            Renderer.GetShadowMapRenderer().UpdateSun(_sun.Definition);
            shadowMapRenderer.UpdateMatrices(camera.Position);
            for (int i = 0; i < 4; i++)
            {
                shadowMapRenderer.BeginCascade(i);
                MainWorld.WorldMeshRegistry.DrawAllMeshes(Renderer, true);
                shadowMapRenderer.EndCascade();
            }

            Renderer.BeginMode(camera);
            MainWorld.WorldMeshRegistry.DrawAllMeshes(Renderer);
            Renderer.EndMode();

            ReconInputSystem.UpdateAll();
            MainWorld.WorldGuiRegistry.DrawContainers(Renderer);

            Render?.Invoke(deltaTime);

            Renderer.EndFrame();
        }

        Shutdown?.Invoke();
        Renderer.CloseWindow();
        SoundProvider.Deinitialize();
    }

    private static ReconCamera3D? _camera;
    public static void SetCamera(ReconCamera3D camera) => _camera = camera;

    private static SunLight? _sun;
    public static void SetSun(SunLight sun)
    {
        _sun = sun;
        Renderer.GetShadowMapRenderer().UpdateSun(sun.Definition);
    }
}
