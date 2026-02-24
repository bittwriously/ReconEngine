namespace ReconEngine.System3D;
using ReconEngine.RenderUtils;

public class PointLight : ReconLight3D { }
public class SpotLight : ReconLight3D
{
    public float InnerAngle
    {
        get => _definition.InnerAngle;
        set => _definition.InnerAngle = value;
    }
    public float OuterAngle
    {
        get => _definition.OuterAngle;
        set => _definition.OuterAngle = value;
    }
    
    public override void Ready()
    {
        base.Ready();
        _definition.Type = LightType.Spot;
    }
}
public class SunLight : ReconLight3D
{
    public override void Ready()
    {
        base.Ready();
        _definition.Type = LightType.Directional;
    }
}