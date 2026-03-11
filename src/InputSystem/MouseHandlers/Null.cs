using System.Numerics;

namespace ReconEngine.InputSystem;

public class NullMouseHandler : IMouseHandler
{
    public event EventHandler<Vector2>? MouseMoved;
    public event EventHandler<Vector2>? MouseScroll;
    public event EventHandler<MouseButtonEventArgs>? MouseDown;
    public event EventHandler<MouseButtonEventArgs>? MouseUp;

    public Vector2 GetMousePosition() => Vector2.Zero;
    public void SetMousePosition(Vector2 pos) { }
    public void SetMouseCursorVisible(bool visible) { }
    public Vector2 GetMouseMovement() => Vector2.Zero;
    public bool IsMouseDown(int button) => false;
    public void Update() { }
}
