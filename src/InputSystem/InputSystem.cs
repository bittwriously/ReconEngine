using ReconEngine.RenderingEngines;

namespace ReconEngine.InputSystem;

public static class ReconInputSystem
{
    public static IMouseHandler MouseHandler = new NullMouseHandler();
    public static IKeyboardHandler KeyboardHandler = new NullKeyboardHandler();
    public static ITextInputHandler TextInputHandler = new NullTextInputHandler();

    public static void Initialize(IRenderer renderer)
    {
        if (renderer is RaylibRenderer)
        {
            MouseHandler = new RaylibMouseHandler();
            KeyboardHandler = new RaylibKeyboardHandler();
            TextInputHandler = new RaylibTextInputHandler();
        }
    }

    public static void UpdateAll()
    {
        MouseHandler.Update();
        KeyboardHandler.Update();
        TextInputHandler.Update();
    }

    public static bool IsKeyHeld(ReconKey key) => KeyboardHandler.IsKeyHeld(key);
    public static bool IsKeyDown(ReconKey key) => KeyboardHandler.IsKeyDown(key);
}
