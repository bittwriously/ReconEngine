using System.Numerics;
using ReconEngine.InputSystem;
using ReconEngine.UISystem.Components;
using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem;

public struct GuiTransformCache
{
    public int PosX;
    public int PosY;
    public int SizeX;
    public int SizeY;

    public float Rotation;

    public Vector2 ScreenSize;
}

public enum GuiMouseState
{
    None,     // not hovered / clicked
    Hovered,  // mouse is hovering over
}

public abstract class GuiObject : ReconEntity
{
    public bool Interactable = true;
    public bool Visible = true;
    public bool ClipDescendants = false;
    public GuiTransformCache TransformCache;

    public Vector4 Position // just like roblox UDim2s in order: X Scale, Y Scale, X Offset, Y Offset
    {
        get { return _position; }
        set
        {
            _transformdirty = true;
            _position = value;
        }
    }
    public Vector4 Size
    {
        get { return _size; }
        set
        {
            _transformdirty = true;
            _size = value;
        }
    }
    public float Rotation
    {
        get { return _rotation; }
        set
        {
            _transformdirty = true;
            _rotation = value;
        }
    }
    public int ZIndex
    {
        get { return _zindex; }
        set
        {
            _transformdirty = true;
            _zindex = value;
            if (Parent != null)
            {
                if (Parent is GuiObject parent) parent.UpdateChildrenOrder();
                else if (Parent is GuiContainer container) container.UpdateChildrenOrder();
            }
        }
    }
    public Vector2 AnchorPoint
    {
        get { return _anchorpoint; }
        set
        {
            _transformdirty = true;
            _anchorpoint = value;
        }
    }
    public Coords2 Transform { get; private set; }
    public OOBB2 GlobalBounds { get; private set; }
    public GuiMouseState MouseState { get; protected set; }
    public GuiContainer? AssignedContainer { get; protected set; }
    public bool Active => Interactable && Visible;

    public event MouseEvent? OnMouseEnter;
    public event MouseEvent? OnMouseLeave;
    public event MouseEvent? OnMouseMove;

    public Color4 BackgroundColor = new();

    private Vector4 _position = Vector4.Zero;
    private Vector4 _size = Vector4.Zero;
    private float _rotation = 0.0f;
    private int _zindex = 1;
    private Vector2 _anchorpoint = Vector2.Zero;
    private bool _transformdirty = true;
    private Vector2 _lastScreenSize = Vector2.Zero;
    private Vector2 _lastParentSize = Vector2.Zero;

    protected Color4? _overwriteBgColor = null;

    private void ScaledSizeToAbsoluteSize(Vector4 scaled, Vector2 screensize, out int X, out int Y)
    {
        X = (int)Math.Round(scaled.X * screensize.X + scaled.Z);
        Y = (int)Math.Round(scaled.Y * screensize.Y + scaled.W);
    }

    protected virtual void UpdateTransform(Vector2 screenSize, Vector2 parentSize)
    {
        if (screenSize == _lastScreenSize) return;
        if (parentSize == _lastParentSize) return;

        Vector2 parentGlobalPos = Vector2.Zero;
        float parentGlobalRot = 0f;

        if (Parent is GuiObject parent)
        {
            parentGlobalPos = parent.Transform.Position;
            parentGlobalRot = parent.Transform.ToRotation();
            //screenSize = new Vector2(parent.TransformCache.SizeX, parent.TransformCache.SizeY);
            if (parent is ScrollingFrame scroll) parentGlobalPos -= scroll.CanvasPosition;
        }
        if (Parent is GuiContainer container)
        {
            parentGlobalPos += container.ScreenInsets;
            //screenSize -= container.ScreenInsets * 2;
        }

        ScaledSizeToAbsoluteSize(_position, parentSize, out int PosX, out int PosY);
        ScaledSizeToAbsoluteSize(_size, parentSize, out int SizeX, out int SizeY);

        Vector2 localPos = new(PosX, PosY);
        Vector2 size = new(SizeX, SizeY);

        Vector2 anchorOffset = _anchorpoint * size;

        Vector2 pivotPos = localPos;

        float globalRot = parentGlobalRot + ReconMath.Deg2Rad(_rotation);
        Vector2 rotatedLocalPos = ReconMath.RotatePoint(pivotPos, parentGlobalRot);
        Vector2 globalPivot = parentGlobalPos + rotatedLocalPos;

        Vector2 globalTopLeft = globalPivot - ReconMath.RotatePoint(anchorOffset, globalRot);

        TransformCache.Rotation = ReconMath.Rad2Deg(globalRot);
        TransformCache.PosX = (int)Math.Floor(globalTopLeft.X);
        TransformCache.PosY = (int)Math.Floor(globalTopLeft.Y);
        TransformCache.SizeX = SizeX;
        TransformCache.SizeY = SizeY;
        TransformCache.ScreenSize = screenSize;

        foreach (GuiComponent comp in _components) comp.PostTransform(ref TransformCache);

        Vector2 extents = size * 0.5f;
        Vector2 globalCenter = globalPivot + ReconMath.RotatePoint(extents - anchorOffset, globalRot);
        GlobalBounds = new OOBB2(globalCenter, extents, globalRot);

        Transform = new Coords2(globalTopLeft, globalRot);
        _lastScreenSize = screenSize;
        _lastParentSize = parentSize;
        _transformdirty = false;

        AssignedContainer?.ContainerGrid.UpdateObject(this);    
    }

    public virtual void Draw(IRenderer renderer, Vector2 parentSize, Vector2 posOffset)
    {
        UpdateTransform(renderer.GetScreenSize(), parentSize);
        foreach (GuiComponent comp in _components) comp.BeforeDraw(renderer, ref TransformCache);
        renderer.DrawRect(
            TransformCache.PosX + (int)posOffset.X, TransformCache.PosY + (int)posOffset.Y,
            TransformCache.SizeX, TransformCache.SizeY,
            TransformCache.Rotation, Vector2.Zero, _overwriteBgColor != null ? _overwriteBgColor.Value : BackgroundColor
        );
        foreach (GuiComponent comp in _components) comp.AfterDraw(renderer, ref TransformCache);
    }

    protected List<GuiObject> _sortedChildren = [];
    protected List<GuiComponent> _components = [];
    public virtual void DrawSelfAndChildren(IRenderer renderer, Vector2 parentSize, Vector2 posOffset)
    {
        Draw(renderer, parentSize, posOffset);

        if (ClipDescendants) renderer.PushClipRect(
            TransformCache.PosX, TransformCache.PosY,
            TransformCache.SizeX, TransformCache.SizeY
        );

        Vector2 objectSize = new(TransformCache.SizeX, TransformCache.SizeY);

        foreach (GuiComponent comp in _components) comp.BeforeChildrenDraw(renderer, ref TransformCache, ref objectSize, ref posOffset);
        foreach (GuiObject obj in _sortedChildren) obj.DrawSelfAndChildren(renderer, objectSize, posOffset);
        foreach (GuiComponent comp in _components) comp.AfterChildrenDraw(renderer, ref TransformCache, ref objectSize, ref posOffset);

        if (ClipDescendants) renderer.PopClipRect();
    }

    internal void RegisterComponent(GuiComponent c)
    {
        if (_components.Contains(c)) return;
        _components.Add(c);
    }
    internal void UnregisterComponent(GuiComponent c)
    {
        if (!_components.Contains(c)) return;
        _components.Remove(c);
    }

    private void UpdateChildrenOrder() => _sortedChildren = [.. Children.OfType<GuiObject>().OrderBy(c => c.ZIndex)];

    public override void Ready()
    {
        base.Ready();
        UpdateChildrenOrder();
        AncestryChanged += (sender, oldWorld) =>
        {
            AssignedContainer?.ContainerGrid.UnregisterObject(this);

            if (Parent is GuiContainer container) AssignedContainer = container;
            else if (Parent is GuiObject obj) AssignedContainer = obj.AssignedContainer;
            else AssignedContainer = null;

            AssignedContainer?.ContainerGrid.RegisterObject(this);
            _transformdirty = true;
        };
    }

    protected override void AddChild(ReconEntity entity)
    {
        base.AddChild(entity);
        UpdateChildrenOrder();
    }
    protected override void RemoveChild(ReconEntity entity)
    {
        base.RemoveChild(entity);
        UpdateChildrenOrder();
    }

    public GuiObject? GetElementAt(Vector2 point)
    {
        if (!this.GlobalBounds.Contains(point)) return null;
        if (!this.Interactable) return null;
        foreach (GuiObject entity in _sortedChildren)
        {
            var hit = entity.GetElementAt(point);
            if (hit != null) return hit;
        }
        return BackgroundColor.Alpha > 0 ? this : null; // only return self if we are not transparent
    }

    public virtual void IHover()
    {
        MouseState = GuiMouseState.Hovered;
        OnMouseEnter?.Invoke(ReconInputSystem.MouseHandler.GetMousePosition());
    }
    public virtual void IUnhover()
    {
        MouseState = GuiMouseState.None;
        OnMouseLeave?.Invoke(ReconInputSystem.MouseHandler.GetMousePosition());
    }
    public virtual void IMove(Vector2 delta) => OnMouseMove?.Invoke(delta);
}
