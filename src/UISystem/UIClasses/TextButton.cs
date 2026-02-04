namespace ReconEngine.UISystem;

public class TextButton: TextLabel, IGuiButton
{
    public bool AutoBackgroundColor { get; set; } = true;
    public bool Pressable { get; set; } = true;
    public bool Modal { get; set; } = false;
    public bool IsDown { get; private set; } = false;

    public event MouseEvent? OnMouseEnter;
    public event MouseEvent? OnMouseLeave;
    public event MouseEvent? OnMouseMove;
    public event MouseEvent? OnMouse1Down;
    public event MouseEvent? OnMouse2Down;
    public event MouseEvent? OnMouse1Up;
    public event MouseEvent? OnMouse2Up;

    public void IHover() 
    {
        if (Interactable && AutoBackgroundColor && Pressable)
        {
            Color4 targetColor = new(
                ReconMath.OffsetMidWay(BackgroundColor.Red, .2f),
                ReconMath.OffsetMidWay(BackgroundColor.Green, .2f),
                ReconMath.OffsetMidWay(BackgroundColor.Blue, .2f),
                BackgroundColor.Alpha
            );
            _overwriteBgColor = targetColor;
        }
        
    }
    public void ILeave() => _overwriteBgColor = null;
}