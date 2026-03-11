using System.Diagnostics;
using System.Numerics;
using ReconEngine.RenderUtils;
using ReconEngine.System3D;
using ReconEngine.UISystem;

namespace ReconEngine.RenderingEngines;

// renderer for headless runtimes
public class NullRenderer : IRenderer
{
    private IShadowRenderer _shadowRenderer => new NullShadowRenderer();
    public IShadowRenderer GetShadowMapRenderer() => _shadowRenderer;

    private bool _windowShouldClose = true;

    public void InitWindow(int width, int height, string title)
    {
        SetAmbientColor(new Color4(0.2f, 0.2f, 0.2f, 1.0f));
        _shadowRenderer.InitShadowMapShaders();
        _shadowRenderer.CreateShadowMap();
        _windowShouldClose = false;

        _stopwatch.Start();
        _lastTicks = _stopwatch.ElapsedTicks;
    }
    public void CloseWindow() => _windowShouldClose = true;

    public void SetDebugMode(LightingDebugMode mode) { }
    public LightingDebugMode CycleDebugMode() => LightingDebugMode.None;

    public void BeginFrame() { }
    public void EndFrame()
    {
        long currentTicks = _stopwatch.ElapsedTicks;
        _deltaTime = (float)(currentTicks - _lastTicks) / Stopwatch.Frequency;
        _lastTicks = currentTicks;
    }
    public bool ShouldClose() => _windowShouldClose;

    public uint RegisterMesh(string filepath) => 0;
    public uint RegisterTexture(string filepath) => 0;
    public uint RegisterFont(string filepath) => 0;

    public void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode) { }

    public void RemoveMesh(uint id) { }
    public void RemoveTexture(uint id) { }
    public void RemoveFont(uint id) { }

    public void BeginMode(ReconCamera3D camera) { }
    public void DrawShape(ReconShape3D shape, uint textureId, Vector3 position, Quaternion rotation, Vector3 size) { }
    public void DrawModel(uint modelId, uint textureId, Vector3 position, Quaternion rotation, Vector3 size) { }
    public void DrawShapeDepth(ReconShape3D shape, Vector3 position, Quaternion rotation, Vector3 size) { }
    public void DrawModelDepth(uint modelId, Vector3 position, Quaternion rotation, Vector3 size) { }
    public void DrawLine3D(Vector3 posA, Vector3 posB, Color4 color) { }
    public void EndMode() { }

    public void DrawTexture(uint textureId, int x, int y) { }
    public void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode) { }
    public void DrawText(string text, int x, int y, byte textsize, Color4 color) { }
    public void DrawText(string text, int x, int y, uint fontid, byte textsize, Color4 color, Vector2 anchor, float rotation) { }
    public void DrawRect(int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color) { }
    public void DrawRectOutline(int px, int py, int sx, int sy, float rotation, Color4 color, float thickness) { }
    public void DrawLine(Vector2 posA, Vector2 posB, Color4 color) { }
    public void DrawLine(int px, int py, int sx, int sy, Color4 color, float thickness) { }
    public void PushClipRect(int x, int y, int w, int h) { }
    public void PopClipRect() { }

    public void ClearBuffer() { }

    private readonly Stopwatch _stopwatch = new();
    private long _lastTicks = 0;
    private float _deltaTime = 1f;
    public float GetFrameTime() => _deltaTime;

    public Vector2 GetScreenSize() => Vector2.One;
    public Vector2 GetTextSize(string text, uint fontid, byte fontsize) => Vector2.One;

    public uint AddLight(LightDefinition def) => 0;
    public void UpdateLight(uint lightId, LightDefinition def) { }
    public void RemoveLight(uint lightId) { }

    public Vector2 GetMousePosition() => Vector2.Zero;

    private Color4 _ambientColor = Color4.White;
    public void SetAmbientColor(Color4 ambient) => _ambientColor = ambient;
    public Color4 GetAmbientColor() => _ambientColor;

    public void EnableHDRIGammaCorrection(bool enabled) { }

    public void LoadTextureSkybox(SkyboxType type, string texture) { }
    public void LoadSolidSkybox(Color4 color) { }
    public void LoadGradientSkybox(Color4 top, Color4 bottom) { }

    public (SkyboxType type, string texture) GetSkybox() => (SkyboxType.SolidColor, "");
    public (Color4 top, Color4 bottom) GetSkyboxColors() => (Color4.White, Color4.Black);

    public void DrawDebugOverlay() { }
}


public class NullShadowRenderer : IShadowRenderer
{
    public const int CASCADE_COUNT = 4;
    public int CascadeCount => CASCADE_COUNT;
    public float[] CascadeSplits => [16f, 64f, 128f, 256f];

    public Matrix4x4[] LightSpaceMatrices => _lightSpaceMatrices;
    private readonly Matrix4x4[] _lightSpaceMatrices = new Matrix4x4[CASCADE_COUNT];

    public void InitShadowMapShaders() { }
    public void CreateShadowMap() { }
    public void UpdateSun(LightDefinition? light) { }
    public void UpdateMatrices(Vector3 cameraPos) { }
    public void BeginCascade(int index) { }
    public void EndCascade() { }
    public void DrawDebugQuad(int x = 10, int y = 40, int size = 128) { }
}
