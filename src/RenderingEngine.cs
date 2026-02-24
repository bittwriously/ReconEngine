using System.Numerics;
using ReconEngine.RenderUtils;
using ReconEngine.UISystem;

namespace ReconEngine;

public enum ETextureSamplingMode
{
    Closest,
    Bilinear,
    Trilinear,
}

// renderer interface for swappable renderers
public interface IRenderer
{
    // generic functions
    public void BeginFrame();
    public void EndFrame();
    public void InitWindow(int width, int height, string title);
    public void CloseWindow();

    public bool ShouldClose(); // if the program should end

    // mesh / texture storage
    public uint RegisterMesh(string filepath);
    public uint RegisterTexture(string filepath);
    public uint RegisterFont(string filepath);

    public void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode);

    public void RemoveMesh(uint id);
    public void RemoveTexture(uint id);
    public void RemoveFont(uint id);

    // switch to 3d manager (used for game world)
    public void BeginMode(ReconCamera3D camera);
    public void DrawModel(uint modelId, uint textureId, Vector3 position, Quaternion rotation, Vector3 size);
    public void EndMode();

    // 2d methods (used for guis)
    public void DrawTexture(uint textureId, int x, int y);
    public void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode);
    public void DrawText(string text, int x, int y, byte textsize, Color4 color);
    public void DrawText(string text, int x, int y, uint fontid, byte textsize, Color4 color, Vector2 anchor, float rotation);
    public void DrawRect(int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color);

    // misc methods
    public void ClearBuffer();
    public float GetFrameTime();
    public Vector2 GetScreenSize();
    public Vector2 GetTextSize(string text, uint fontid, byte fontsize);

    // light methods
    public uint AddLight(LightDefinition light);
    public void UpdateLight(uint lightId, LightDefinition light);
    public void RemoveLight(uint lightId);

    // input methods
    public Vector2 GetMousePosition();
}
