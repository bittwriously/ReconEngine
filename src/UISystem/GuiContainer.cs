using System.Numerics;
using ReconEngine.InputSystem;
using ReconEngine.UISystem.UIGrid;
using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem;

public abstract class GuiContainer: ReconEntity
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

    private List<GuiObject> sortedChildren = [];
    public void UpdateChildrenOrder() => sortedChildren = [.. Children.OfType<GuiObject>().OrderBy(c => c.ZIndex)];
    public void DrawElements(IRenderer renderer)
    {
        foreach (GuiObject obj in sortedChildren) obj.DrawSelfAndChildren(renderer);
    }

    public GuiObject? GetElementAt(Vector2 point)
    {
        foreach (GuiObject obj in sortedChildren)
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
        ParentChanged += (sender, oldParent) =>
        {
            oldParent?.CurrentWorld?.WorldGuiRegistry.RemoveContainer(this);
            CurrentWorld?.WorldGuiRegistry.RegisterContainer(this);
        };

        /// HANDLES UI ELEMENT HOVER ///
        ReconInputSystem.MouseHandler.MouseMoved += (sender, delta) => ContainerGrid.HoverAt(ReconInputSystem.MouseHandler.GetMousePosition());
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