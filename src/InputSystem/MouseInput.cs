using System.Numerics;

namespace ReconEngine.InputSystem;

public interface IMouseHandler
{
    Vector2 GetMousePosition();
    Vector2 GetMouseMovement();
    
    bool IsMouseDown(int button);

    event EventHandler<Vector2>? MouseMoved;
    event EventHandler<float>? MouseScroll;
    
    event EventHandler<MouseButtonEventArgs>? MouseDown;
    event EventHandler<MouseButtonEventArgs>? MouseUp;

    void Update();
}

public record struct MouseButtonEventArgs(int Button, Vector2 Position);