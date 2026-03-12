using System.Numerics;

namespace ReconEngine.UISystem;

public struct TextScaleCache
{
    public string? CachedText;
    public byte CachedMaxSize;
    public int CachedBoundsW;
    public int CachedBoundsH;
    public byte ComputedSize;

    public readonly bool IsValid(string text, byte maxSize, int w, int h)
        => CachedText == text
        && CachedMaxSize == maxSize
        && CachedBoundsW == w
        && CachedBoundsH == h;
}

public enum TextLabelHAlign { Left, Right, Middle }
public enum TextLabelVAlign { Top, Bottom, Middle }

public class TextLabel : GuiObject
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

    public float LineHeight = 1.0f;
    public int MaxVisibleChars = -1;
    public Color4 TextColor = new(1, 1, 1, 1);
    public TextLabelHAlign HorizontalAlignment = TextLabelHAlign.Middle;
    public TextLabelVAlign VerticalAlignment = TextLabelVAlign.Middle;

    public bool TextAutoScale = false;
    public byte TextSize
    {
        get => _textSize;
        set { _textSize = value; InvalidateScaleCache(); }
    }
    public string Text
    {
        get => _text;
        set { _text = value; InvalidateScaleCache(); }
    }

    private string _fontName = "";
    private uint _font = 1;
    private string _text = "";
    private byte _textSize = 16;

    private TextScaleCache _scaleCache;
    private void InvalidateScaleCache() => _scaleCache.CachedText = null;

    private byte ResolveTextSize(IRenderer renderer)
    {
        if (!TextAutoScale) return _textSize;
        int w = TransformCache.SizeX;
        int h = TransformCache.SizeY;
        if (_scaleCache.IsValid(_text, _textSize, w, h)) return _scaleCache.ComputedSize;
        if (w <= 0 || h <= 0) return 1;
        byte lo = 1, hi = _textSize, best = 1;
        while (lo <= hi)
        {
            byte mid = (byte)((lo + hi) / 2);
            Vector2 measured = renderer.GetTextSize(_text, _font, mid);
            if (measured.X <= w && measured.Y <= h)
            {
                best = mid;
                lo = (byte)(mid + 1);
            }
            else
            {
                hi = (byte)(mid - 1);
            }
        }
        _scaleCache = new TextScaleCache
        {
            CachedText = _text,
            CachedMaxSize = _textSize,
            CachedBoundsW = w,
            CachedBoundsH = h,
            ComputedSize = best,
        };
        return best;
    }

    public override void Draw(IRenderer renderer, Vector2 parentSize, Vector2 posOffset)
    {
        base.Draw(renderer, parentSize, posOffset);
        if (_text == "") return;

        byte resolvedSize = ResolveTextSize(renderer);
        Vector2 textSize = renderer.GetTextSize(_text, _font, resolvedSize);

        float horizontalShift = HorizontalAlignment switch
        {
            TextLabelHAlign.Left => 0f,
            TextLabelHAlign.Middle => 0.5f,
            TextLabelHAlign.Right => 1.0f,
            _ => 0.5f
        };
        float verticalShift = VerticalAlignment switch
        {
            TextLabelVAlign.Top => 0f,
            TextLabelVAlign.Middle => 0.5f,
            TextLabelVAlign.Bottom => 1.0f,
            _ => 0.5f
        };

        Vector2 textPivot = new(
            textSize.X * horizontalShift,
            textSize.Y * verticalShift
        );

        float drawX = TransformCache.PosX + (TransformCache.SizeX * horizontalShift);
        float drawY = TransformCache.PosY + (TransformCache.SizeY * verticalShift);

        renderer.DrawText(
            _text,
            (int)drawX + (int)posOffset.Y,
            (int)drawY + (int)posOffset.Y,
            _font,
            resolvedSize,
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
