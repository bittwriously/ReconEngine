using System.Numerics;

namespace ReconEngine.UISystem;

public enum TextureLabelScalingMode
{
    Stretch,
    Fit,
    Crop,
}

public class TextureLabel : GuiObject
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
    public Color4 ImageColor = new(1, 1, 1, 1);
    public TextureLabelScalingMode ScalingMode = TextureLabelScalingMode.Stretch;

    protected string _imageName = "";
    protected uint _imageId = 0;

    public override void Draw(IRenderer renderer)
    {
        base.Draw(renderer);
        uint id = GetCurrentImageId();
        if (id == 0) return;
        renderer.DrawTexture(id,
            TransformCache.PosX, TransformCache.PosY,
            TransformCache.SizeX, TransformCache.SizeY,
            TransformCache.Rotation, Vector2.Zero, ImageColor,
            ScalingMode
        );
    }

    protected virtual uint GetCurrentImageId() => _imageId;
}
