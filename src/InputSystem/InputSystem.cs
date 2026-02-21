namespace ReconEngine.InputSystem;

public static class ReconInputSystem
{
    public static readonly IMouseHandler MouseHandler = new RaylibMouseHandler();
    public static readonly IKeyboardHandler KeyboardHandler = new RaylibKeyboardHandler();

    public static void UpdateAll()
    {
        MouseHandler.Update();
        KeyboardHandler.Update();
    }
}
