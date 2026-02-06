using System.Numerics;

namespace ReconEngine.InputSystem;

public interface IMouseHandler
{
    public Vector2 GetMousePosition();
    public Vector2 GetMouseMovement();

    public bool IsMouseDown(int button);

    public event EventHandler<Vector2>? MouseMoved;
    public event EventHandler<float>? MouseScroll;

    public event EventHandler<MouseButtonEventArgs>? MouseDown;
    public event EventHandler<MouseButtonEventArgs>? MouseUp;

    public void Update();
}

public record struct MouseButtonEventArgs(int Button, Vector2 Position);
