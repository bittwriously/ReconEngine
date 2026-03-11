namespace ReconEngine.InputSystem;

public class NullKeyboardHandler : IKeyboardHandler
{
    public event EventHandler<ReconKeyEventArgs>? KeyDown;
    public event EventHandler<ReconKeyEventArgs>? KeyUp;

    public bool IsKeyDown(ReconKey key) => false;
    public bool IsKeyHeld(ReconKey key) => false;
    public void Update() { }
}
