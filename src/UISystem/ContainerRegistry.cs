
namespace ReconEngine.UISystem;

public class GuiContainerRegistry
{
    private readonly List<GuiContainer> _registeredContainers = [];
    private List<GuiContainer> _orderedContainers = [];

    public void UpdateRenderOrder() => _orderedContainers = [.. _registeredContainers.OfType<GuiContainer>().OrderBy(c => c.DisplayOrder)];

    public void RegisterContainer(GuiContainer container)
    {
        if (_registeredContainers.Contains(container)) return;
        _registeredContainers.Add(container);
        UpdateRenderOrder();
    }
    public void RemoveContainer(GuiContainer container)
    {
        if (!_registeredContainers.Contains(container)) return;
        _registeredContainers.Remove(container);
        UpdateRenderOrder();
    }

    public void DrawContainers(IRenderer renderer)
    {
        foreach (GuiContainer gui in _orderedContainers) if (gui.Enabled) gui.DrawElements(renderer);
    }

    private GuiObject? _lastHovered = null;
    public void ProcessMouse(IRenderer renderer)
    {
        foreach (GuiContainer gui in _orderedContainers)
        {
            if (!gui.Enabled) continue;
            GuiObject? obj = gui.GetElementAt(renderer.GetMousePosition());
            if (obj != null && obj != _lastHovered)
            {
                _lastHovered?.IUnhover();
                obj.IHover();
                _lastHovered = obj;
                return;
            }
        }
        _lastHovered?.IUnhover();
        _lastHovered = null;
    }
}
