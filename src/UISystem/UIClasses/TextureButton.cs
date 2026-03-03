using ReconEngine.InputSystem;

namespace ReconEngine.UISystem;

public class TextureButton : TextureLabel, IGuiButton
{
    public bool AutoBackgroundColor { get; set; } = true;
    public bool Pressable { get; set; } = true;
    public bool Modal { get; set; } = false;
    private int _buttonMask = 0;
    public bool IsDown => _buttonMask != 0;

    public event MouseEvent? OnMouse1Down;
    public event MouseEvent? OnMouse2Down;
    public event MouseEvent? OnMouse3Down;
    public event MouseEvent? OnMouse1Up;
    public event MouseEvent? OnMouse2Up;
    public event MouseEvent? OnMouse3Up;

    private enum ButtonImageState { Normal, Hovered, Pressed }
    private ButtonImageState _imageState = ButtonImageState.Normal;

    public string HoverImage
    {
        get => _hoverImageName;
        set
        {
            uint imageId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Texture);
            _hoverImageId = imageId;
            _hoverImageName = value;
        }
    }
    public string PressedImage
    {
        get => _pressImageName;
        set
        {
            uint imageId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Texture);
            _pressImageId = imageId;
            _pressImageName = value;
        }
    }

    private string _hoverImageName = "";
    private uint _hoverImageId = 0;
    private string _pressImageName = "";
    private uint _pressImageId = 0;

    public new bool Active => Interactable && Pressable && Visible;

    public override void IHover()
    {
        base.IHover();
        if (Active && AutoBackgroundColor && !IsDown)
        {
            _overwriteBgColor = _hoverColor;
            _imageState = ButtonImageState.Hovered;
        }
    }
    public override void IUnhover()
    {
        base.IUnhover();
        if (!IsDown) {
            _overwriteBgColor = null;
            _imageState = ButtonImageState.Normal;
        }
    }

    private Color4 _hoverColor => new(
        ReconMath.OffsetMidWay(BackgroundColor.Red, .2f),
        ReconMath.OffsetMidWay(BackgroundColor.Green, .2f),
        ReconMath.OffsetMidWay(BackgroundColor.Blue, .2f),
        BackgroundColor.Alpha
    );
    private Color4 _pressedColor => new(
        ReconMath.OffsetMidWay(BackgroundColor.Red, .35f),
        ReconMath.OffsetMidWay(BackgroundColor.Green, .35f),
        ReconMath.OffsetMidWay(BackgroundColor.Blue, .35f),
        BackgroundColor.Alpha
    );


    public void OnPointerPress(int buttonIndex)
    {
        _buttonMask |= 1 << buttonIndex;
        MouseEvent? downEvent = buttonIndex switch
        {
            1 => OnMouse1Down,
            2 => OnMouse2Down,
            3 => OnMouse3Down,
            _ => OnMouse1Down
        };
        if (Active)
        {
            downEvent?.Invoke(ReconInputSystem.MouseHandler.GetMousePosition());
            if (buttonIndex == 1)
            {
                _overwriteBgColor = _pressedColor;
                _imageState = ButtonImageState.Pressed;
            }
        }
    }
    public void OnPointerRelease(int buttonIndex)
    {
        _buttonMask &= ~(1 << buttonIndex);
        MouseEvent? upEvent = buttonIndex switch
        {
            1 => OnMouse1Up,
            2 => OnMouse2Up,
            3 => OnMouse3Up,
            _ => OnMouse1Up
        };
        if (Active)
        {
            upEvent?.Invoke(ReconInputSystem.MouseHandler.GetMousePosition());
            _imageState = MouseState == GuiMouseState.Hovered
                ? ButtonImageState.Hovered
                : ButtonImageState.Normal;
            _overwriteBgColor = MouseState == GuiMouseState.Hovered ? _hoverColor : null;
        }
    }

    protected override uint GetCurrentImageId() => _imageState switch
    {
        ButtonImageState.Hovered => _hoverImageId != 0 ? _hoverImageId : _imageId,
        ButtonImageState.Pressed => _pressImageId != 0 ? _pressImageId : _imageId,
        _ => _imageId
    };
}
