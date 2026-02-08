using System.Numerics;
using ReconEngine.InputSystem;
using ReconEngine.RenderUtils;
using ReconEngine.UISystem;
using ReconEngine.WorldSystem;

namespace ReconEngine;

internal static class ReconCore
{
    public static IRenderer Renderer = new RenderingEngines.RaylibRenderer();
    public static ReconWorld MainWorld = null!;

    [STAThread]
    public static void Main()
    {
        ReconCamera3D camera = new(new Vector3(5.0f, 0.0f, 0.0f), Vector3.Zero);

        Renderer.InitWindow(800, 600, "ReconEngine");

        MainWorld = new("MainWorld");

        double physicsAccumulator = 0.0;
        const double physicsFrametime = 1.0 / 20.0;

        var maingui = new ScreenGui()
        {
            Parent = MainWorld.Root,
            Name = "MAINTESTGUI",
        };
        var fpslabel = new TextLabel()
        {
            Parent = maingui,
            Text = "FPS",
            TextColor = new Color4(1, 1, 1, 1),
            BackgroundColor = new Color4(1, .5f, .5f, 1),
            Size = new Vector4(.7f, .5f, 0, 0),
            TextSize = 32,
            Position = new(.15f, .25f, 0, 0),
            Name = "FPSGRAPH"
        };
        var testButton1 = new TextButton()
        {
            Parent = maingui,
            Text = "TEST BUTTON 1",
            BackgroundColor = new Color4(.7f, .7f, .7f, 1),
            Size = new Vector4(0, 0, 150, 50),
            Name = "TESTBUTTON"
        };
        var testButton2 = new TextureButton()
        {
            Parent = maingui,
            Image = "assets/textures/cpp_colors_thumb.png",
            HoverImage = "assets/textures/OSAGE.png",
            PressedImage = "assets/textures/heavy.jpg",
            ScalingMode = TextureLabelScalingMode.Fit,
            BackgroundColor = new Color4(.3f, .3f, .3f, 1),
            Size = new Vector4(0, 0, 150, 50),
            Position = new Vector4(0, 0, 150, 0),
            Name = "TESTBUTTON2"
        };
        Console.WriteLine("CURRENTWORLD");
        TreePrinter.PrintTree(MainWorld.Root);

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
                fpslabel.Text = $"FPS: {Math.Round(1 / deltaTime)}\nTPS: {Math.Floor(1 / physicsFrametime)}";

                testButton1.Position += new Vector4(0, 0, 0, 1);
                testButton2.Position += new Vector4(0, 0, 0, 2);
            }
            /// PHYSICS ///

            Renderer.BeginFrame();

            Renderer.ClearBuffer();

            /// 3D ///
            Renderer.BeginMode(camera);
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

            Renderer.EndFrame();
        }

        Renderer.CloseWindow();
    }
}
