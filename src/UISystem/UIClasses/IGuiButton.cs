using System.Numerics;

namespace ReconEngine.UISystem;

public delegate void MouseEvent(Vector2 MousePosition);

public interface IGuiButton
{
    bool AutoBackgroundColor { get; set; }
    bool Pressable { get; set; }
    bool Modal { get; set; }
    bool IsDown { get; }
    
    event MouseEvent OnMouse1Down;
    event MouseEvent OnMouse2Down;
    event MouseEvent OnMouse1Up;
    event MouseEvent OnMouse2Up;
}