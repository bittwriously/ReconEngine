namespace ReconEngine.InputSystem;

public class ReconInputSystem
{
    public IMouseHandler MouseHandler = new RaylibMouseHandler();

    public void UpdateAll()
    {
        MouseHandler.Update();
    }
}