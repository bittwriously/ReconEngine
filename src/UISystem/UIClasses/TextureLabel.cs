using System.Numerics;

namespace ReconEngine.UISystem;

public enum TextureLabelScalingMode
{
    Stretch,
    Fit,
    Crop,
}

public class TextureLabel: GuiObject
{
    public string Image
    {
        get => _imageName;
        set
        {
            uint imageId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Texture);
            _imageId = imageId;
            _imageName = value;
        }
    }
    public Color4 ImageColor = new(1,1,1,1);
    public TextureLabelScalingMode ScalingMode = TextureLabelScalingMode.Stretch;

    private string _imageName = "";
    private uint _imageId = 0;

    public override void Draw(IRenderer renderer)
    {
        base.Draw(renderer);
        if (_imageName == "") return;
        renderer.DrawTexture(_imageId,
            TransformCache.PosX, TransformCache.PosY,
            TransformCache.SizeX, TransformCache.SizeY,
            TransformCache.Rotation, AnchorPoint, ImageColor,
            ScalingMode
        );
    }

    public override void Ready()
    {
        base.Ready();
        Image = "assets/textures/cpp_colors_thumb.png";
    }
}