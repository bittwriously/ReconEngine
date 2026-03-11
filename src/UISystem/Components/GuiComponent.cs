using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem.Components;

public abstract class GuiComponent : ReconEntity
{
    private GuiObject? _connected;

    public override void Ready()
    {
        base.Ready();
        ParentChanged += (sender, oldParent) =>
        {
            _connected?.UnregisterComponent(this);
            _connected = Parent as GuiObject;
            _connected?.RegisterComponent(this);
        };
    }

    public virtual void Layout() { }

    public virtual void BeforeDraw() { }
    public virtual void BeforeChildrenDraw() { }
    public virtual void AfterChildrenDraw() { }
    public virtual void AfterDraw() { }

    public virtual void PostTransform() { }
}
