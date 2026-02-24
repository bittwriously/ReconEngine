using System.Numerics;

namespace ReconEngine.RenderUtils;

public enum LightType
{
    Directional = 0,
    Point = 1,
    Spot = 2,
}

public struct LightDefinition()
{
    public LightType Type = LightType.Point;
    public Vector3 Position = Vector3.Zero;
    public Vector3 Direction = Vector3.Zero;
    public Color4 Color = new();
    public bool Enabled = true;
    public float Distance = 16f;
    public float InnerAngle = 15;
    public float OuterAngle = 45;
}