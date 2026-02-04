
namespace ReconEngine.UISystem;

public class GuiContainerRegistry {
    private readonly List<GuiContainer> registeredContainers = [];
    private List<GuiContainer> orderedContainers = [];

    public void UpdateRenderOrder() => orderedContainers = [.. registeredContainers.OfType<GuiContainer>().OrderBy(c => c.DisplayOrder)];
    
    public void RegisterContainer(GuiContainer container)
    {
        if (registeredContainers.Contains(container)) return;
        registeredContainers.Add(container);
        UpdateRenderOrder();
    }
    public void RemoveContainer(GuiContainer container)
    {
        if (!registeredContainers.Contains(container)) return;
        registeredContainers.Remove(container);
        UpdateRenderOrder();
    }

    public void DrawContainers(IRenderer renderer)
    {
        foreach (GuiContainer gui in orderedContainers) if (gui.Enabled) gui.DrawElements(renderer);
    }

    private IGuiButton? lastHovered = null;
    public void ProcessMouse(IRenderer renderer)
    {
        foreach (GuiContainer gui in orderedContainers)
        {
            if (!gui.Enabled) continue;
            GuiObject? obj = gui.GetElementAt(renderer.GetMousePosition());
            if (obj != null && obj is IGuiButton btn && btn != lastHovered)
            {
                lastHovered?.ILeave();
                btn.IHover();
                lastHovered = btn;
                return;
            }
        }
        lastHovered?.ILeave();
        lastHovered = null;
    }
}