using Raylib_cs;
namespace ReconEngine.InputSystem;

public class RaylibKeyboardHandler : IKeyboardHandler
{
    public event EventHandler<ReconKeyEventArgs>? KeyDown;
    public event EventHandler<ReconKeyEventArgs>? KeyUp;

    private readonly Dictionary<ReconKey, bool> _previousStates = new();

    public bool IsKeyDown(ReconKey key) =>
        Raylib.IsKeyPressed(ToRaylib(key));

    public bool IsKeyHeld(ReconKey key) =>
        Raylib.IsKeyDown(ToRaylib(key));

    public void Update()
    {
        foreach (ReconKey key in Enum.GetValues<ReconKey>())
        {
            bool isDown = Raylib.IsKeyDown(ToRaylib(key));
            bool wasDown = _previousStates.TryGetValue(key, out bool prev) && prev;

            if (isDown && !wasDown) KeyDown?.Invoke(this, new ReconKeyEventArgs(key));
            if (!isDown && wasDown) KeyUp?.Invoke(this, new ReconKeyEventArgs(key));

            _previousStates[key] = isDown;
        }
    }

    // BIG FUCKING SWITCH STATEMENT
    private static KeyboardKey ToRaylib(ReconKey key) => key switch
    {
        ReconKey.A => KeyboardKey.A,
        ReconKey.B => KeyboardKey.B,
        ReconKey.C => KeyboardKey.C,
        ReconKey.D => KeyboardKey.D,
        ReconKey.E => KeyboardKey.E,
        ReconKey.F => KeyboardKey.F,
        ReconKey.G => KeyboardKey.G,
        ReconKey.H => KeyboardKey.H,
        ReconKey.I => KeyboardKey.I,
        ReconKey.J => KeyboardKey.J,
        ReconKey.K => KeyboardKey.K,
        ReconKey.L => KeyboardKey.L,
        ReconKey.M => KeyboardKey.M,
        ReconKey.N => KeyboardKey.N,
        ReconKey.O => KeyboardKey.O,
        ReconKey.P => KeyboardKey.P,
        ReconKey.Q => KeyboardKey.Q,
        ReconKey.R => KeyboardKey.R,
        ReconKey.S => KeyboardKey.S,
        ReconKey.T => KeyboardKey.T,
        ReconKey.U => KeyboardKey.U,
        ReconKey.V => KeyboardKey.V,
        ReconKey.W => KeyboardKey.W,
        ReconKey.X => KeyboardKey.X,
        ReconKey.Y => KeyboardKey.Y,
        ReconKey.Z => KeyboardKey.Z,
        ReconKey.Num0 => KeyboardKey.Zero,
        ReconKey.Num1 => KeyboardKey.One,
        ReconKey.Num2 => KeyboardKey.Two,
        ReconKey.Num3 => KeyboardKey.Three,
        ReconKey.Num4 => KeyboardKey.Four,
        ReconKey.Num5 => KeyboardKey.Five,
        ReconKey.Num6 => KeyboardKey.Six,
        ReconKey.Num7 => KeyboardKey.Seven,
        ReconKey.Num8 => KeyboardKey.Eight,
        ReconKey.Num9 => KeyboardKey.Nine,
        ReconKey.Space => KeyboardKey.Space,
        ReconKey.Enter => KeyboardKey.Enter,
        ReconKey.Escape => KeyboardKey.Escape,
        ReconKey.Tab => KeyboardKey.Tab,
        ReconKey.Backspace => KeyboardKey.Backspace,
        ReconKey.Delete => KeyboardKey.Delete,
        ReconKey.Insert => KeyboardKey.Insert,
        ReconKey.LeftShift => KeyboardKey.LeftShift,
        ReconKey.RightShift => KeyboardKey.RightShift,
        ReconKey.LeftControl => KeyboardKey.LeftControl,
        ReconKey.RightControl => KeyboardKey.RightControl,
        ReconKey.LeftAlt => KeyboardKey.LeftAlt,
        ReconKey.RightAlt => KeyboardKey.RightAlt,
        ReconKey.Up => KeyboardKey.Up,
        ReconKey.Down => KeyboardKey.Down,
        ReconKey.Left => KeyboardKey.Left,
        ReconKey.Right => KeyboardKey.Right,
        ReconKey.F1 => KeyboardKey.F1,
        ReconKey.F2 => KeyboardKey.F2,
        ReconKey.F3 => KeyboardKey.F3,
        ReconKey.F4 => KeyboardKey.F4,
        ReconKey.F5 => KeyboardKey.F5,
        ReconKey.F6 => KeyboardKey.F6,
        ReconKey.F7 => KeyboardKey.F7,
        ReconKey.F8 => KeyboardKey.F8,
        ReconKey.F9 => KeyboardKey.F9,
        ReconKey.F10 => KeyboardKey.F10,
        ReconKey.F11 => KeyboardKey.F11,
        ReconKey.F12 => KeyboardKey.F12,
        ReconKey.CapsLock => KeyboardKey.CapsLock,
        ReconKey.Home => KeyboardKey.Home,
        ReconKey.End => KeyboardKey.End,
        ReconKey.PageUp => KeyboardKey.PageUp,
        ReconKey.PageDown => KeyboardKey.PageDown,
        ReconKey.Kp0 => KeyboardKey.Kp0,
        ReconKey.Kp1 => KeyboardKey.Kp1,
        ReconKey.Kp2 => KeyboardKey.Kp2,
        ReconKey.Kp3 => KeyboardKey.Kp3,
        ReconKey.Kp4 => KeyboardKey.Kp4,
        ReconKey.Kp5 => KeyboardKey.Kp5,
        ReconKey.Kp6 => KeyboardKey.Kp6,
        ReconKey.Kp7 => KeyboardKey.Kp7,
        ReconKey.Kp8 => KeyboardKey.Kp8,
        ReconKey.Kp9 => KeyboardKey.Kp9,
        ReconKey.KpEnter => KeyboardKey.KpEnter,
        ReconKey.Comma => KeyboardKey.Comma,
        ReconKey.Period => KeyboardKey.Period,
        ReconKey.Slash => KeyboardKey.Slash,
        ReconKey.Semicolon => KeyboardKey.Semicolon,
        ReconKey.Minus => KeyboardKey.Minus,
        ReconKey.Equal => KeyboardKey.Equal,
        ReconKey.LeftBracket => KeyboardKey.LeftBracket,
        ReconKey.RightBracket => KeyboardKey.RightBracket,
        ReconKey.Backslash => KeyboardKey.Backslash,
        ReconKey.Grave => KeyboardKey.Grave,
        ReconKey.Apostrophe => KeyboardKey.Apostrophe,
        _ => KeyboardKey.Null
    };
}
