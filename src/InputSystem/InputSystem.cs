namespace ReconEngine.InputSystem;

public static class ReconInputSystem
{
    public static readonly IMouseHandler MouseHandler = new RaylibMouseHandler();

    public static void UpdateAll()
    {
        MouseHandler.Update();
    }
}
