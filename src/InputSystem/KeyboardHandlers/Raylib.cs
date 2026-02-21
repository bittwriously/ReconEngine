using Raylib_cs;

namespace ReconEngine.InputSystem;

public class RaylibKeyboardHandler : IKeyboardHandler
{
    private readonly Dictionary<KeyboardKey, bool> _previousKeyStates = [];
    private readonly KeyboardKey[] _allKeys;

    public event EventHandler<KeyboardButtonEventArgs>? ButtonDown;
    public event EventHandler<KeyboardButtonEventArgs>? ButtonUp;

    public RaylibKeyboardHandler() => _allKeys = Enum.GetValues<KeyboardKey>();

    public bool IsKeyDown(string keycode)
    {
        if (Enum.TryParse<KeyboardKey>(keycode, true, out var key)) return Raylib.IsKeyDown(key);
        return false;
    }

    public void Update()
    {
        foreach (var key in _allKeys)
        {
            bool isCurrentlyDown = Raylib.IsKeyDown(key);
            bool wasPreviouslyDown = _previousKeyStates.TryGetValue(key, out var prev) && prev;
            if (isCurrentlyDown && !wasPreviouslyDown) ButtonDown?.Invoke(this, new KeyboardButtonEventArgs { Keycode = key.ToString() });
            if (!isCurrentlyDown && wasPreviouslyDown) ButtonUp?.Invoke(this, new KeyboardButtonEventArgs { Keycode = key.ToString() });
            _previousKeyStates[key] = isCurrentlyDown;
        }
    }
}