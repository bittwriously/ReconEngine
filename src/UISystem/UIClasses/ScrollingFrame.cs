using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem;

public enum ScrollingDirection
{
    Y,
    X,
    XY,
}

public enum AutomaticCanvasSizeMode
{
    Disabled,
    X,
    Y,
    XY,
}

public enum ElasticBehavior
{
    Always,
    WhenScrollable,
    Never,
}

public class ScrollingFrame : Frame {
    public Vector4 CanvasSize
    {
        get => _canvasSize;
        set { _canvasSize = value; _scrollDirty = true; }
    }
    public Vector2 CanvasPosition
    {
        get => _canvasPosition;
        set { _canvasPosition = ClampCanvasPosition(value); _scrollDirty = true; }
    }

    public ScrollingDirection ScrollingDirection = ScrollingDirection.Y;
    public AutomaticCanvasSizeMode AutomaticCanvasSize = AutomaticCanvasSizeMode.Disabled;
    public ElasticBehavior ElasticBehavior = ElasticBehavior.WhenScrollable;
    public bool ScrollingEnabled = true;

    public float ScrollSpeed = 40f;

    public int ScrollBarThickness = 8;
    public int ScrollBarPadding = 2;
    public Color4 ScrollBarColor = new(0.4f, 0.4f, 0.4f, 0.7f);
    public Color4 ScrollBarHoverColor = new(0.6f, 0.6f, 0.6f, 0.85f);
    public Color4 ScrollBarDragColor = new(0.7f, 0.7f, 0.7f, 1.0f);

    public event Action<Vector2>? OnScrolled;

    private Vector4 _canvasSize = new(0, 0, 0, 0);
    private Vector2 _canvasPosition = Vector2.Zero;
    private Vector2 _absoluteCanvasSize = Vector2.Zero;
    private bool _scrollDirty = true;

    private bool _draggingVBar = false;
    private bool _draggingHBar = false;
    private bool _hoveringVBar = false;
    private bool _hoveringHBar = false;
    private float _dragStartOffset = 0f;
    private float _dragStartScroll = 0f;

    public ScrollingFrame()
    {
        ClipDescendants = true;
    }

    private Vector2 ResolveCanvasSize()
    {
        float viewW = TransformCache.SizeX;
        float viewH = TransformCache.SizeY;
        if (AutomaticCanvasSize != AutomaticCanvasSizeMode.Disabled) return ComputeAutomaticCanvasSize(viewW, viewH);
        float canvasW = _canvasSize.X * viewW + _canvasSize.Z;
        float canvasH = _canvasSize.Y * viewH + _canvasSize.W;
        return new Vector2(Math.Max(canvasW, viewW), Math.Max(canvasH, viewH));
    }

    private Vector2 ComputeAutomaticCanvasSize(float viewW, float viewH)
    {
        float maxRight = 0f;
        float maxBottom = 0f;
        foreach (ReconEntity entity in Children)
        {
            if (entity is not GuiObject child) continue;
            float childRight = child.TransformCache.PosX - TransformCache.PosX
                               + _canvasPosition.X + child.TransformCache.SizeX;
            float childBottom = child.TransformCache.PosY - TransformCache.PosY
                                + _canvasPosition.Y + child.TransformCache.SizeY;
            maxRight = Math.Max(maxRight, childRight);
            maxBottom = Math.Max(maxBottom, childBottom);
        }
        float canvasW = AutomaticCanvasSize is AutomaticCanvasSizeMode.X or AutomaticCanvasSizeMode.XY
            ? Math.Max(maxRight, viewW) : viewW;
        float canvasH = AutomaticCanvasSize is AutomaticCanvasSizeMode.Y or AutomaticCanvasSizeMode.XY
            ? Math.Max(maxBottom, viewH) : viewH;
        return new Vector2(canvasW, canvasH);
    }

    private Vector2 ClampCanvasPosition(Vector2 pos)
    {
        float maxX = Math.Max(0, _absoluteCanvasSize.X - TransformCache.SizeX);
        float maxY = Math.Max(0, _absoluteCanvasSize.Y - TransformCache.SizeY);
        bool canScrollX = ScrollingDirection is ScrollingDirection.X or ScrollingDirection.XY;
        bool canScrollY = ScrollingDirection is ScrollingDirection.Y or ScrollingDirection.XY;
        return new Vector2(
            canScrollX ? Math.Clamp(pos.X, 0, maxX) : 0,
            canScrollY ? Math.Clamp(pos.Y, 0, maxY) : 0
        );
    }

    private bool CanScrollX => ScrollingDirection is ScrollingDirection.X or ScrollingDirection.XY
                                && _absoluteCanvasSize.X > TransformCache.SizeX;
    private bool CanScrollY => ScrollingDirection is ScrollingDirection.Y or ScrollingDirection.XY
                                && _absoluteCanvasSize.Y > TransformCache.SizeY;

    private (float trackStart, float trackLength, float thumbStart, float thumbLength) GetVBarGeometry()
    {
        float viewH = TransformCache.SizeY;
        float trackStart = TransformCache.PosY + ScrollBarPadding;
        float trackLength = viewH - ScrollBarPadding * 2;
        if (CanScrollX) trackLength -= ScrollBarThickness; // shrink if hbar present
        float ratio = viewH / _absoluteCanvasSize.Y;
        float thumbLength = Math.Max(20, trackLength * ratio);
        float scrollFraction = _canvasPosition.Y / Math.Max(1, _absoluteCanvasSize.Y - viewH);
        float thumbStart = trackStart + scrollFraction * (trackLength - thumbLength);

        return (trackStart, trackLength, thumbStart, thumbLength);
    }

    private (float trackStart, float trackLength, float thumbStart, float thumbLength) GetHBarGeometry()
    {
        float viewW = TransformCache.SizeX;
        float trackStart = TransformCache.PosX + ScrollBarPadding;
        float trackLength = viewW - ScrollBarPadding * 2;
        if (CanScrollY) trackLength -= ScrollBarThickness; // shrink if vbar present
        float ratio = viewW / _absoluteCanvasSize.X;
        float thumbLength = Math.Max(20, trackLength * ratio);
        float scrollFraction = _canvasPosition.X / Math.Max(1, _absoluteCanvasSize.X - viewW);
        float thumbStart = trackStart + scrollFraction * (trackLength - thumbLength);

        return (trackStart, trackLength, thumbStart, thumbLength);
    }

    private bool IsPointOnVBarThumb(Vector2 point)
    {
        if (!CanScrollY) return false;
        var (_, _, thumbStart, thumbLength) = GetVBarGeometry();
        float barX = TransformCache.PosX + TransformCache.SizeX - ScrollBarThickness - ScrollBarPadding;
        return point.X >= barX && point.X <= barX + ScrollBarThickness
            && point.Y >= thumbStart && point.Y <= thumbStart + thumbLength;
    }

    private bool IsPointOnHBarThumb(Vector2 point)
    {
        if (!CanScrollX) return false;
        var (_, _, thumbStart, thumbLength) = GetHBarGeometry();
        float barY = TransformCache.PosY + TransformCache.SizeY - ScrollBarThickness - ScrollBarPadding;
        return point.X >= thumbStart && point.X <= thumbStart + thumbLength
            && point.Y >= barY && point.Y <= barY + ScrollBarThickness;
    }

    protected override void UpdateTransform(Vector2 screenSize)
    {
        base.UpdateTransform(screenSize);

        _absoluteCanvasSize = ResolveCanvasSize();
        _canvasPosition = ClampCanvasPosition(_canvasPosition);
        _scrollDirty = false;
    }

    public override void Draw(IRenderer renderer)
    {
        Vector2 screenSize = renderer.GetScreenSize();
        if (_scrollDirty) UpdateTransform(screenSize);

        base.Draw(renderer);
    }

    public override void DrawSelfAndChildren(IRenderer renderer)
    {
        base.DrawSelfAndChildren(renderer);
        DrawScrollBars(renderer);
    }

    private void DrawScrollBars(IRenderer renderer)
    {
        if (CanScrollY)
        {
            var (_, _, thumbStart, thumbLength) = GetVBarGeometry();
            float barX = TransformCache.PosX + TransformCache.SizeX - ScrollBarThickness - ScrollBarPadding;

            Color4 color = _draggingVBar ? ScrollBarDragColor
                         : _hoveringVBar ? ScrollBarHoverColor
                         : ScrollBarColor;

            renderer.DrawRect(
                (int)barX, (int)thumbStart,
                ScrollBarThickness, (int)thumbLength,
                0f, Vector2.Zero, color
            );
        }

        if (CanScrollX)
        {
            var (_, _, thumbStart, thumbLength) = GetHBarGeometry();
            float barY = TransformCache.PosY + TransformCache.SizeY - ScrollBarThickness - ScrollBarPadding;

            Color4 color = _draggingHBar ? ScrollBarDragColor
                         : _hoveringHBar ? ScrollBarHoverColor
                         : ScrollBarColor;

            renderer.DrawRect(
                (int)thumbStart, (int)barY,
                (int)thumbLength, ScrollBarThickness,
                0f, Vector2.Zero, color
            );
        }
    }

    public void HandleMouseWheel(float deltaX, float deltaY)
    {
        if (!ScrollingEnabled) return;
        Vector2 newPos = _canvasPosition;
        if (ScrollingDirection is ScrollingDirection.Y or ScrollingDirection.XY)
            newPos.Y -= deltaY * ScrollSpeed;
        if (ScrollingDirection is ScrollingDirection.X or ScrollingDirection.XY)
            newPos.X -= deltaX * ScrollSpeed;
        SetCanvasPositionInternal(ClampCanvasPosition(newPos));
    }

    public bool HandleMouseDown(Vector2 mousePos)
    {
        if (!ScrollingEnabled) return false;
        if (IsPointOnVBarThumb(mousePos))
        {
            _draggingVBar = true;
            var (_, _, thumbStart, _) = GetVBarGeometry();
            _dragStartOffset = mousePos.Y - thumbStart;
            _dragStartScroll = _canvasPosition.Y;
            return true;
        }
        if (IsPointOnHBarThumb(mousePos))
        {
            _draggingHBar = true;
            var (_, _, thumbStart, _) = GetHBarGeometry();
            _dragStartOffset = mousePos.X - thumbStart;
            _dragStartScroll = _canvasPosition.X;
            return true;
        }
        return false;
    }

    public void HandleMouseDrag(Vector2 mousePos)
    {
        if (_draggingVBar)
        {
            var (trackStart, trackLength, _, thumbLength) = GetVBarGeometry();
            float maxScroll = _absoluteCanvasSize.Y - TransformCache.SizeY;
            float trackRange = trackLength - thumbLength;
            if (trackRange <= 0) return;
            float thumbPos = mousePos.Y - _dragStartOffset;
            float fraction = (thumbPos - trackStart) / trackRange;
            fraction = Math.Clamp(fraction, 0f, 1f);
            Vector2 newPos = _canvasPosition;
            newPos.Y = fraction * maxScroll;
            SetCanvasPositionInternal(ClampCanvasPosition(newPos));
        }

        if (_draggingHBar)
        {
            var (trackStart, trackLength, _, thumbLength) = GetHBarGeometry();
            float maxScroll = _absoluteCanvasSize.X - TransformCache.SizeX;
            float trackRange = trackLength - thumbLength;
            if (trackRange <= 0) return;
            float thumbPos = mousePos.X - _dragStartOffset;
            float fraction = (thumbPos - trackStart) / trackRange;
            fraction = Math.Clamp(fraction, 0f, 1f);
            Vector2 newPos = _canvasPosition;
            newPos.X = fraction * maxScroll;
            SetCanvasPositionInternal(ClampCanvasPosition(newPos));
        }
    }

    public void HandleMouseUp()
    {
        _draggingVBar = false;
        _draggingHBar = false;
    }

    public void HandleMouseHover(Vector2 mousePos)
    {
        _hoveringVBar = IsPointOnVBarThumb(mousePos);
        _hoveringHBar = IsPointOnHBarThumb(mousePos);
    }

    public void ScrollToChild(GuiObject child)
    {
        float childTop = child.TransformCache.PosY - TransformCache.PosY + _canvasPosition.Y;
        float childBottom = childTop + child.TransformCache.SizeY;
        float childLeft = child.TransformCache.PosX - TransformCache.PosX + _canvasPosition.X;
        float childRight = childLeft + child.TransformCache.SizeX;

        Vector2 newPos = _canvasPosition;

        if (childBottom > _canvasPosition.Y + TransformCache.SizeY)
            newPos.Y = childBottom - TransformCache.SizeY;
        if (childTop < _canvasPosition.Y)
            newPos.Y = childTop;

        if (childRight > _canvasPosition.X + TransformCache.SizeX)
            newPos.X = childRight - TransformCache.SizeX;
        if (childLeft < _canvasPosition.X)
            newPos.X = childLeft;

        SetCanvasPositionInternal(ClampCanvasPosition(newPos));
    }

    public (Vector2 topLeft, Vector2 bottomRight) GetVisibleRegion()
    {
        return (
            _canvasPosition,
            _canvasPosition + new Vector2(TransformCache.SizeX, TransformCache.SizeY)
        );
    }

    private void SetCanvasPositionInternal(Vector2 newPos)
    {
        if (newPos == _canvasPosition) return;
        _canvasPosition = newPos;
        _scrollDirty = true;
        OnScrolled?.Invoke(_canvasPosition);
    }
}
