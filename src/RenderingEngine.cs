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
    void BeginFrame();
    void EndFrame();
    void InitWindow(int width, int height, string title);
    void CloseWindow();

    bool ShouldClose(); // if the program should end

    // mesh / texture storage
    uint RegisterMesh(string filepath);
    uint RegisterTexture(string filepath);
    uint RegisterFont(string filepath);

    void ApplyLightingShader(uint modelId);
    void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode);

    void RemoveMesh(uint id);
    void RemoveTexture(uint id);
    void RemoveFont(uint id);
    
    // switch to 3d manager (used for game world)
    void BeginMode(ReconCamera3D camera);
    void DrawModel(uint modelId, Vector3 position, float scale);
    void EndMode();

    // 2d methods (used for guis)
    void DrawTexture(uint textureId, int x, int y);
    void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode);
    void DrawText(string text, int x, int y, byte textsize, Color4 color);
    void DrawText(string text, int x, int y, uint fontid, byte textsize, Color4 color, Vector2 anchor, float rotation);
    void DrawRect(int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color);

    // misc methods
    void ClearBuffer();
    float GetFrameTime();
    Vector2 GetScreenSize();
    Vector2 GetTextSize(string text, uint fontid, byte fontsize);

    // input methods
    Vector2 GetMousePosition();
}