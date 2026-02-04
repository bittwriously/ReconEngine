using System.Numerics;

namespace ReconEngine.UISystem;

public struct TextScaleCache {} // will be implemented when TextAutoscale is added

public enum TextLabelHAlign { Left, Right, Middle }
public enum TextLabelVAlign { Top, Bottom, Middle }

public class TextLabel: GuiObject
{
    public string Font
    {
        get => _fontName;
        set
        {
            uint fontId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Font);
            _font = fontId;
            _fontName = value;
        }
    }
    public string Text = "";
    public float LineHeight = 1.0f;
    public int MaxVisibleChars = -1;
    public Color4 TextColor = new(1,1,1,1);
    public byte TextSize = 16;
    public TextLabelHAlign HorizontalAlignment = TextLabelHAlign.Middle;
    public TextLabelVAlign VertialAlignment = TextLabelVAlign.Middle;

    private string _fontName = "";
    private uint _font = 1;

    public override void Draw(IRenderer renderer)
    {
        base.Draw(renderer);
        if (Text == "") return;
        
        Vector2 textSize = renderer.GetTextSize(Text, _font, TextSize);
        float horizontalShift = HorizontalAlignment switch {
            TextLabelHAlign.Left => 0,
            TextLabelHAlign.Middle => 0.5f,
            TextLabelHAlign.Right => 1.0f,
            _ => 0.5f
        };
        float verticalShift = TextLabelVAlign.Middle switch {
            TextLabelVAlign.Top => 0,
            TextLabelVAlign.Middle => 0.5f,
            TextLabelVAlign.Bottom => 1.0f,
            _ => 0.5f
        };
        Vector2 textPivot = new(
            textSize.X * horizontalShift, 
            textSize.Y * verticalShift
        );
        float drawX = TransformCache.PosX + (TransformCache.SizeX * (horizontalShift - AnchorPoint.X));
        float drawY = TransformCache.PosY + (TransformCache.SizeY * (verticalShift - AnchorPoint.Y));

        renderer.DrawText(
            Text, 
            (int)drawX, 
            (int)drawY, 
            _font, 
            TextSize, 
            TextColor, 
            textPivot, 
            TransformCache.Rotation
        );
    }

    public override void Ready()
    {
        base.Ready();
        Font = "assets/fonts/pixellari.ttf";
    }
}