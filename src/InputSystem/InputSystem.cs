namespace ReconEngine.InputSystem;

public static class ReconInputSystem
{
    public static readonly IMouseHandler MouseHandler = new RaylibMouseHandler();
    public static readonly IKeyboardHandler KeyboardHandler = new RaylibKeyboardHandler();
    public static readonly ITextInputHandler TextInputHandler = new RaylibTextInputHandler();

    public static void UpdateAll()
    {
        MouseHandler.Update();
        KeyboardHandler.Update();
        TextInputHandler.Update();
    }

    public static bool IsKeyHeld(ReconKey key) => KeyboardHandler.IsKeyHeld(key);
    public static bool IsKeyDown(ReconKey key) => KeyboardHandler.IsKeyDown(key);
}
