using Raylib_cs;
namespace ReconEngine.InputSystem;

public class NullTextInputHandler : ITextInputHandler
{
    public event EventHandler<TextInputEventArgs>? CharacterTyped;
    public void Update() { }
}
