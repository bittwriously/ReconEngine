using System.Numerics;
using Raylib_cs;
using ReconEngine.RenderUtils;

namespace ReconEngine;

public interface IShadowRenderer
{
    public void InitShadowMapShaders();
    public void CreateShadowMap();
    public void UpdateSun(LightDefinition? light);
    public void UpdateMatrices(Vector3 cameraPos);
    public void BeginCascade(int index);
    public void EndCascade();
    public void DrawDebugQuad(int x = 10, int y = 40, int size = 128);

    public int CascadeCount { get; }
    public float[] CascadeSplits { get; }
    public Matrix4x4[] LightSpaceMatrices { get; }
    public RenderTexture2D[] ShadowMaps { get; }
}
