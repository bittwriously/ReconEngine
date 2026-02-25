using System.Numerics;
using ReconEngine.RenderUtils;

namespace ReconEngine.System3D;

public abstract class ReconLight3D : ReconEntity3D
{
    protected LightDefinition _definition = new();
    protected uint _lightId = uint.MaxValue;

    public Color4 Color
    {
        get => _definition.Color;
        set => _definition.Color = value;
    }

    public bool Enabled
    {
        get => _definition.Enabled;
        set => _definition.Enabled = value;
    }

    public float Distance
    {
        get => _definition.Distance;
        set => _definition.Distance = value;
    }

    private Vector3 _baseDirection = Vector3.Zero;
    public Vector3 Direction
    {
        get => _baseDirection;
        set => _baseDirection = value;
    }

    public LightDefinition Definition => _definition;

    public new Vector3 Position
    {
        get => _definition.Position;
        set => _definition.Position = value;
    }

    public override void RenderStep(float deltaTime, IRenderer renderer)
    {
        base.RenderStep(deltaTime, renderer);
        _definition.Direction = Direction = Vector3.Transform(_baseDirection, Rotation);
        renderer.UpdateLight(_lightId, _definition);
    }

    public override void Ready()
    {
        base.Ready();
        _definition.Type = LightType.Point;
        _definition.Enabled = true;
        _definition.Color = new Color4(1,1,1,1);

        AncestryChanged += (sender, oldWorld) =>
        {
            if (oldWorld == ReconCore.MainWorld && oldWorld != CurrentWorld && _lightId != uint.MaxValue) ReconCore.Renderer.RemoveLight(_lightId);
            else if (CurrentWorld != oldWorld && CurrentWorld == ReconCore.MainWorld && _lightId == uint.MaxValue)
                _lightId = ReconCore.Renderer.AddLight(_definition);
        };
    }
}
