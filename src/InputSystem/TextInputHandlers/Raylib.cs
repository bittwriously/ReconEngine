using Raylib_cs;
namespace ReconEngine.InputSystem;

public class RaylibTextInputHandler : ITextInputHandler
{
    public event EventHandler<TextInputEventArgs>? CharacterTyped;

    public void Update()
    {
        int codepoint;
        while ((codepoint = Raylib.GetCharPressed()) != 0)
        {
            char c = (char)codepoint;
            CharacterTyped?.Invoke(this, new TextInputEventArgs(c));
        }
    }
}
