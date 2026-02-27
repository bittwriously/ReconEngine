using ReconEngine.WorldSystem;

namespace ReconEngine.Entities;

public interface IEnvSky {}

public readonly struct SolidSky(Color4 skyColor) : IEnvSky
{
    public readonly Color4 SkyColor = skyColor;
}
public readonly struct GradientSky(Color4 top, Color4 bottom) : IEnvSky
{
    public readonly Color4 SkyTopColor = top;
    public readonly Color4 SkyBottomColor = bottom;
}
public readonly struct CubemapSky(string texturePath) : IEnvSky
{
    public readonly string SkyTexture = texturePath;
}
public readonly struct HDRISky(string texturePath, int cubemapSize) : IEnvSky
{
    public readonly string SkyTexture = texturePath;
    public readonly int CubemapSize = cubemapSize;
}

public class WorldEnvironment : ReconEntity
{
    private bool _active => ReconCore.MainWorld.Environment == this;

    public Color4 AmbientColor
    {
        get => ReconCore.Renderer.GetAmbientColor();
        set => ReconCore.Renderer.SetAmbientColor(value);
    }

    private IEnvSky? _sky;
    public IEnvSky? Sky
    {
        get => _sky;
        set
        {
            _sky = value;
            if (!_active) return;
            ApplySky();
        }
    }

    private void ApplySky()
    {
        IRenderer renderer = ReconCore.Renderer;
        if (_sky == null)
        {
            renderer.LoadSolidSkybox(Color4.Black);
            return;
        }
        renderer.EnableHDRIGammaCorrection(false);
        if (_sky is SolidSky solid) renderer.LoadSolidSkybox(solid.SkyColor);
        else if (_sky is GradientSky grad) renderer.LoadGradientSkybox(grad.SkyTopColor, grad.SkyBottomColor);
        else if (_sky is CubemapSky cube) renderer.LoadTextureSkybox(SkyboxType.Cubemap, cube.SkyTexture);
        else if (_sky is HDRISky hdri)
        {
            renderer.EnableHDRIGammaCorrection(true);
            renderer.LoadTextureSkybox(SkyboxType.HDRI, hdri.SkyTexture);
        }
    }

    public void Activate()
    {
        ReconCore.MainWorld.Environment = this;
        ApplySky();
    }
}
