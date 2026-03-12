using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine.UISystem.Components;

public class PaddingComponent : GuiComponent
{
    public Vector2 PaddingLeft = Vector2.Zero;
    public Vector2 PaddingRight = Vector2.Zero;
    public Vector2 PaddingTop = Vector2.Zero;
    public Vector2 PaddingBottom = Vector2.Zero;

    private static int GetScaledSize(int size, Vector2 value) => (int)(value.Y + (value.X * size));
    public override void BeforeChildrenDraw(IRenderer renderer, ref GuiTransformCache transform, ref Vector2 parentSize, ref Vector2 posOffset)
    {
        base.BeforeChildrenDraw(renderer, ref transform, ref parentSize, ref posOffset);
        int left = GetScaledSize((int)parentSize.X, PaddingLeft);
        int right = GetScaledSize((int)parentSize.X, PaddingRight);
        int top = GetScaledSize((int)parentSize.X, PaddingTop);
        int bottom = GetScaledSize((int)parentSize.X, PaddingBottom);

        parentSize = new(parentSize.X-(left+right), parentSize.Y-(top-bottom));
        posOffset += new Vector2(left, top);
    }
}
