using System.Numerics;
using ReconEngine.InputSystem;
using ReconEngine.UISystem.UIGrid;
using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem;

public abstract class GuiContainer : ReconEntity
{
    public bool Enabled { get; set; } = true;
    public Vector2 ScreenInsets { get; set; } = Vector2.Zero;
    public int DisplayOrder
    {
        get => _displayOrder;
        set
        {
            _displayOrder = value;
        }
    }

    public GuiGrid2D ContainerGrid = null!;

    private int _displayOrder = 0;

    private List<GuiObject> _sortedChildren = [];
    public void UpdateChildrenOrder() => _sortedChildren = [.. Children.OfType<GuiObject>().OrderBy(c => c.ZIndex)];
    public void DrawElements(IRenderer renderer)
    {
        foreach (GuiObject obj in _sortedChildren) obj.DrawSelfAndChildren(renderer);
    }

    public GuiObject? GetElementAt(Vector2 point)
    {
        foreach (GuiObject obj in _sortedChildren)
        {
            GuiObject? element = obj.GetElementAt(point);
            if (element != null) return element;
        }
        return null;
    }

    public override void Ready()
    {
        base.Ready();

        ContainerGrid = new(ReconCore.Renderer.GetScreenSize(), 128);
        AncestryChanged += (sender, oldWorld) =>
        {
            oldWorld?.WorldGuiRegistry.RemoveContainer(this);
            CurrentWorld?.WorldGuiRegistry.RegisterContainer(this);
        };

        /// HANDLES UI ELEMENT INPUTS ///
        ReconInputSystem.MouseHandler.MouseMoved += (sender, delta) =>
        {
            GuiObject? hovered = ContainerGrid.HoverAt(ReconInputSystem.MouseHandler.GetMousePosition());
            hovered?.IMove(ReconInputSystem.MouseHandler.GetMouseMovement());
        };

        IGuiButton? lastPressed = null;
        ReconInputSystem.MouseHandler.MouseDown += (sender, args) =>
        {
            Vector2 pos = args.Position;
            int btnid = args.Button;

            GridCell? cell = ContainerGrid.GetCellAt(pos.X, pos.Y);
            if (cell == null) return;

            GuiObject? obj = cell.GetObjectAt(pos);
            if (obj == null) return;

            if (obj is IGuiButton btn)
            {
                btn.OnPointerPress(btnid);
                lastPressed = btn;
            }
        };
        ReconInputSystem.MouseHandler.MouseUp += (sender, args) =>
        {
            int btnid = args.Button;
            lastPressed?.OnPointerRelease(btnid);
            lastPressed = null;
        };

        UpdateChildrenOrder();
    }

    public override void Destroy()
    {
        base.Destroy();
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
}
