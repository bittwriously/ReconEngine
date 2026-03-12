using System.Numerics;
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

    public virtual void BeforeDraw(IRenderer renderer, ref GuiTransformCache transform) { }
    public virtual void AfterDraw(IRenderer renderer, ref GuiTransformCache transform) { }
    public virtual void BeforeChildrenDraw(IRenderer renderer, ref GuiTransformCache transform, ref Vector2 parentSize, ref Vector2 posOffset) { }
    public virtual void AfterChildrenDraw(IRenderer renderer, ref GuiTransformCache transform, ref Vector2 parentSize, ref Vector2 posOffset) { }

    public virtual void PostTransform(ref GuiTransformCache transform) { }
}
