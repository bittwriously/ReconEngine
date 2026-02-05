using System.Numerics;
using ReconEngine.InputSystem;
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

    protected Color4? _overwriteBgColor = null;

    private void ScaledSizeToAbsoluteSize(Vector4 scaled, Vector2 screensize, out int X, out int Y)
    {
        X = (int)Math.Round(scaled.X * screensize.X + scaled.Z);
        Y = (int)Math.Round(scaled.Y * screensize.Y + scaled.W);
    }

    protected virtual void UpdateTransform(Vector2 screenSize)
    {
        Coords2 parentCoords = Coords2.Identity;
        {
            if (Parent is GuiObject parent)
            {
                parentCoords = parent.Transform;
                screenSize = new Vector2(parent.TransformCache.SizeX, parent.TransformCache.SizeY);
            }
            if (Parent is GuiContainer container)
            {
                parentCoords += container.ScreenInsets;
                screenSize -= container.ScreenInsets * 2;
            }
        }

        ScaledSizeToAbsoluteSize(_position, screenSize, out int PosX, out int PosY);
        ScaledSizeToAbsoluteSize(_size, screenSize, out int SizeX, out int SizeY);

        Vector2 position = new(PosX, PosY);
        Coords2 modifiedCoords = new Coords2(position, ReconMath.Deg2Rad(_rotation)) + (parentCoords.Position * 0.5f);

        TransformCache.Rotation = ReconMath.Rad2Deg(modifiedCoords.ToRotation());
        TransformCache.PosX = (int)Math.Floor(modifiedCoords.Position.X);
        TransformCache.PosY = (int)Math.Floor(modifiedCoords.Position.Y);
        TransformCache.SizeX = SizeX; TransformCache.SizeY = SizeY;
        TransformCache.ScreenSize = screenSize;

        Vector2 extents = new Vector2(SizeX, SizeY) * 0.5f;
        GlobalBounds = new((modifiedCoords * new Coords2(extents)).Position, extents, modifiedCoords.ToRotation());

        Transform = modifiedCoords;
        _lastScreenSize = screenSize;
        _transformdirty = false;

        AssignedContainer?.ContainerGrid.UpdateObject(this);

        foreach (ReconEntity entity in Children) if (entity is GuiObject obj) obj.UpdateTransform(screenSize);
    }

    public virtual void Draw(IRenderer renderer)
    {
        Vector2 screenSize = renderer.GetScreenSize();
        if (_transformdirty || _lastScreenSize != screenSize) UpdateTransform(screenSize);
        renderer.DrawRect(
            TransformCache.PosX, TransformCache.PosY,
            TransformCache.SizeX, TransformCache.SizeY,
            TransformCache.Rotation, _anchorpoint, _overwriteBgColor != null ? _overwriteBgColor.Value : BackgroundColor
        );
    }

    private List<GuiObject> sortedChildren = [];
    public void DrawSelfAndChildren(IRenderer renderer)
    {
        Draw(renderer);
        foreach (GuiObject obj in sortedChildren) obj.Draw(renderer);
    }

    private void UpdateChildrenOrder() => sortedChildren = [.. Children.OfType<GuiObject>().OrderBy(c => c.ZIndex)];

    public override void Ready()
    {
        base.Ready();
        UpdateChildrenOrder();
        ParentChanged += (sender, oldParent) =>
        {
            AssignedContainer?.ContainerGrid.UnregisterObject(this);

            if (Parent is GuiContainer container) AssignedContainer = container;
            else if (Parent is GuiObject obj) AssignedContainer = obj.AssignedContainer;
            else AssignedContainer = null;
            
            AssignedContainer?.ContainerGrid.RegisterObject(this);
        };
    }

    public override void AddChild(ReconEntity entity)
    {
        base.AddChild(entity);
        UpdateChildrenOrder();
    }
    public override void RemoveChild(ReconEntity entity)
    {
        base.RemoveChild(entity);
        UpdateChildrenOrder();
    }

    public GuiObject? GetElementAt(Vector2 point)
    {
        if (!this.GlobalBounds.Contains(point)) return null;
        if (!this.Interactable) return null;
        foreach (GuiObject entity in sortedChildren)
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