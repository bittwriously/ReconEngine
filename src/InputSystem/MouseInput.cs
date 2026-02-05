using System.Numerics;

namespace ReconEngine.InputSystem;

public interface IMouseHandler
{
    Vector2 GetMousePosition();
    Vector2 GetMouseMovement();
    
    bool IsMouse1Down();
    bool IsMouse2Down();
    bool IsMouse3Down();

    event EventHandler<Vector2>? MouseMoved;
    
    event EventHandler<float>? MouseScroll;

    event EventHandler<Vector2>? Mouse1Down;
    event EventHandler<Vector2>? Mouse2Down;
    event EventHandler<Vector2>? Mouse3Down;
    event EventHandler<Vector2>? Mouse1Up;
    event EventHandler<Vector2>? Mouse2Up;
    event EventHandler<Vector2>? Mouse3Up;

    void Update(); // main method that is called each frame
}