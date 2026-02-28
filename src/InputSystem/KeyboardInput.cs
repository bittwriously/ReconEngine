namespace ReconEngine.InputSystem;

public interface IKeyboardHandler
{
    public bool IsKeyDown(ReconKey key);
    public bool IsKeyHeld(ReconKey key);

    public event EventHandler<ReconKeyEventArgs>? KeyDown;
    public event EventHandler<ReconKeyEventArgs>? KeyUp;

    public void Update();
}

public record struct ReconKeyEventArgs(ReconKey Key);
