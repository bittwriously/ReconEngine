using System.Numerics;
using Raylib_cs;

namespace ReconEngine.InputSystem;

public class RaylibMouseHandler : IMouseHandler
{
    public Vector2 GetMousePosition() => Raylib.GetMousePosition();
    public Vector2 GetMouseMovement() => Raylib.GetMouseDelta();

    public bool IsMouse1Down() => Raylib.IsMouseButtonDown(MouseButton.Left);
    public bool IsMouse2Down() => Raylib.IsMouseButtonDown(MouseButton.Right);
    public bool IsMouse3Down() => Raylib.IsMouseButtonDown(MouseButton.Middle);

    public event EventHandler<Vector2>? MouseMoved;
    
    public event EventHandler<float>? MouseScroll;
    
    public event EventHandler<Vector2>? Mouse1Down;
    public event EventHandler<Vector2>? Mouse2Down;
    public event EventHandler<Vector2>? Mouse3Down;
    public event EventHandler<Vector2>? Mouse1Up;
    public event EventHandler<Vector2>? Mouse2Up;
    public event EventHandler<Vector2>? Mouse3Up;

    private bool _wasM1Down = false;
    private bool _wasM2Down = false;
    private bool _wasM3Down = false;

    public void Update()
    {
        bool m1Down = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool m2Down = Raylib.IsMouseButtonDown(MouseButton.Right);
        bool m3Down = Raylib.IsMouseButtonDown(MouseButton.Middle);

        Vector2 mPos = Raylib.GetMousePosition();

        if (_wasM1Down && !m1Down) Mouse1Up?.Invoke(this, mPos);
        else if (!_wasM1Down && m1Down) Mouse1Down?.Invoke(this, mPos);
        _wasM1Down = m1Down;

        if (_wasM2Down && !m2Down) Mouse2Up?.Invoke(this, mPos);
        else if (!_wasM2Down && m2Down) Mouse2Down?.Invoke(this, mPos);
        _wasM2Down = m2Down;

        if (_wasM3Down && !m3Down) Mouse3Up?.Invoke(this, mPos);
        else if (!_wasM3Down && m3Down) Mouse3Down?.Invoke(this, mPos);
        _wasM3Down = m3Down;

        Vector2 mDelta = Raylib.GetMouseDelta();
        if (mDelta != Vector2.Zero) MouseMoved?.Invoke(this, mDelta);
        
        float wheelDelta = Raylib.GetMouseWheelMove();
        if (wheelDelta != 0) MouseScroll?.Invoke(this, wheelDelta);
    }
}