namespace ReconEngine.InputSystem;

public interface ITextInputHandler
{
    event EventHandler<TextInputEventArgs>? CharacterTyped;
    void Update();
}

public record struct TextInputEventArgs(char Character);
