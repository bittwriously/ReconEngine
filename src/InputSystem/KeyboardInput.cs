
namespace ReconEngine.InputSystem;

public interface IKeyboardHandler
{
    public bool IsKeyDown(string Keycode);

    public event EventHandler<KeyboardButtonEventArgs>? ButtonDown;
    public event EventHandler<KeyboardButtonEventArgs>? ButtonUp;

    public void Update();
}

public record struct KeyboardButtonEventArgs(string Keycode);