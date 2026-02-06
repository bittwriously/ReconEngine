using System.Numerics;
using Raylib_cs;

namespace ReconEngine.InputSystem;

public class RaylibMouseHandler : IMouseHandler
{
    private static readonly MouseButton[] _buttonsToTrack =
    [
        MouseButton.Left,
        MouseButton.Right,
        MouseButton.Middle,
        MouseButton.Side,
        MouseButton.Extra
    ];
    private readonly Dictionary<MouseButton, bool> _buttonStates = new();
    public event EventHandler<Vector2>? MouseMoved;
    public event EventHandler<float>? MouseScroll;
    public event EventHandler<MouseButtonEventArgs>? MouseDown;
    public event EventHandler<MouseButtonEventArgs>? MouseUp;
    public RaylibMouseHandler()
    {
        foreach (var btn in _buttonsToTrack) _buttonStates[btn] = false;
    }
    public Vector2 GetMousePosition() => Raylib.GetMousePosition();
    public Vector2 GetMouseMovement() => Raylib.GetMouseDelta();
    public bool IsMouseDown(int button) => Raylib.IsMouseButtonDown((MouseButton)button);
    public void Update()
    {
        Vector2 mPos = Raylib.GetMousePosition();
        foreach (var btn in _buttonsToTrack)
        {
            bool isCurrentlyDown = Raylib.IsMouseButtonDown(btn);
            bool wasDown = _buttonStates[btn];
            if (isCurrentlyDown && !wasDown)
            {
                MouseDown?.Invoke(this, new((int)btn + 1, mPos));
            }
            else if (!isCurrentlyDown && wasDown)
            {
                MouseUp?.Invoke(this, new((int)btn + 1, mPos));
            }
            _buttonStates[btn] = isCurrentlyDown;
        }
        Vector2 mDelta = Raylib.GetMouseDelta();
        if (mDelta != Vector2.Zero) MouseMoved?.Invoke(this, mPos);
        float wheelDelta = Raylib.GetMouseWheelMove();
        if (wheelDelta != 0) MouseScroll?.Invoke(this, wheelDelta);
    }
}
