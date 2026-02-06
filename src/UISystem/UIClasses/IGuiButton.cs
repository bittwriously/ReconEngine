using System.Numerics;

namespace ReconEngine.UISystem;

public delegate void MouseEvent(Vector2 MousePosition);

public interface IGuiButton
{
    public bool AutoBackgroundColor { get; set; }
    public bool Pressable { get; set; }
    public bool Modal { get; set; }
    public bool IsDown { get; }

    public event MouseEvent OnMouse1Down;
    public event MouseEvent OnMouse2Down;
    public event MouseEvent OnMouse3Down;
    public event MouseEvent OnMouse1Up;
    public event MouseEvent OnMouse2Up;
    public event MouseEvent OnMouse3Up;

    public void OnPointerPress(int buttonIndex);
    public void OnPointerRelease(int buttonIndex);
}
