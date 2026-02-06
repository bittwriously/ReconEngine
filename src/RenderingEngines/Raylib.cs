using System.Collections;
using System.Numerics;
using Raylib_cs;
using ReconEngine.RenderUtils;
using ReconEngine.UISystem;

namespace ReconEngine.RenderingEngines;

public struct RaylibLight
{
    public bool Enabled;
    public RaylibLightType Type;
    public Vector3 Position;
    public Vector3 Target;
    public Color Color;

    public int EnabledLoc;
    public int TypeLoc;
    public int PosLoc;
    public int TargetLoc;
    public int ColorLoc;
}

public enum RaylibLightType
{
    Directional = 0,
    Point = 1
}

public class RaylibRenderer : IRenderer
{
    private readonly Dictionary<uint, Model> _meshRegistry = [];
    private uint _meshRegistryCounter = 0;
    private readonly Dictionary<uint, Texture2D> _textureRegistry = [];
    private uint _textureRegistryCounter = 0;
    private readonly Dictionary<uint, Font> _fontRegistry = [];
    private uint _fontRegistryCounter = 0;
    private RaylibLight _sun;
    private Shader _lightShader;

    public void InitLight(ref RaylibLight light, Shader shader)
    {
        light.EnabledLoc = Raylib.GetShaderLocation(shader, "lights[0].enabled");
        light.TypeLoc = Raylib.GetShaderLocation(shader, "lights[0].type");
        light.PosLoc = Raylib.GetShaderLocation(shader, "lights[0].position");
        light.TargetLoc = Raylib.GetShaderLocation(shader, "lights[0].target");
        light.ColorLoc = Raylib.GetShaderLocation(shader, "lights[0].color");

        UpdateLightValues(light, shader);
    }

    public void UpdateLightValues(RaylibLight light, Shader shader)
    {
        Raylib.SetShaderValue(shader, light.EnabledLoc, light.Enabled ? 1 : 0, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(shader, light.TypeLoc, (int)light.Type, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(shader, light.PosLoc, light.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(shader, light.TargetLoc, light.Target, ShaderUniformDataType.Vec3);
        Vector4 colorVec = new(light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f, light.Color.A / 255f);
        Raylib.SetShaderValue(shader, light.ColorLoc, colorVec, ShaderUniformDataType.Vec4);
    }

    public void InitWindow(int width, int height, string title)
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);

        Raylib.InitWindow(width, height, title);
        //Raylib.DisableCursor();

        _lightShader = Raylib.LoadShader("assets/shaders/lighting.vs", "assets/shaders/lighting.fs");
        unsafe { _lightShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(_lightShader, "viewPos"); }

        int ambientLoc = Raylib.GetShaderLocation(_lightShader, "ambient");
        Raylib.SetShaderValue(_lightShader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, ShaderUniformDataType.Vec4);

        _sun = new()
        {
            Enabled = true,
            Type = RaylibLightType.Directional,
            Position = new Vector3(50, 100, 50),
            Target = Vector3.Zero,
            Color = Color.White
        };
        InitLight(ref _sun, _lightShader);
    }
    public void CloseWindow() => Raylib.CloseWindow();

    public void BeginFrame()
    {
        Raylib.BeginDrawing();
        UpdateLightValues(_sun, _lightShader);
    }
    public void EndFrame() => Raylib.EndDrawing();

    public bool ShouldClose() => Raylib.WindowShouldClose();

    public uint RegisterMesh(string filepath)
    {
        Model model = Raylib.LoadModel(filepath);
        _meshRegistryCounter++;
        _meshRegistry.Add(_meshRegistryCounter, model);
        return _meshRegistryCounter;
    }
    public uint RegisterTexture(string filepath)
    {
        Texture2D texture = Raylib.LoadTexture(filepath);
        Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
        Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
        _textureRegistryCounter++;
        _textureRegistry.Add(_textureRegistryCounter, texture);
        return _textureRegistryCounter;
    }
    public uint RegisterFont(string filepath)
    {
        Font font = Raylib.LoadFont(filepath);
        _fontRegistryCounter++;
        _fontRegistry.Add(_fontRegistryCounter, font);
        return _fontRegistryCounter;
    }

    public void ApplyLightingShader(uint modelId)
    {
        if (!_meshRegistry.ContainsKey(modelId)) return;
        Model model = _meshRegistry[modelId];
        for (int i = 0; i < model.MaterialCount; i++)
            unsafe
            {
                model.Materials[i].Shader = _lightShader;
            }
    }
    public void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode)
    {
        if (!_textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = _textureRegistry[textureId];
        TextureFilter textureFilter = TextureFilter.Point;
        switch (samplingMode)
        {
            case ETextureSamplingMode.Closest:
                textureFilter = TextureFilter.Point;
                break;
            case ETextureSamplingMode.Bilinear:
                textureFilter = TextureFilter.Bilinear;
                break;
            case ETextureSamplingMode.Trilinear:
                textureFilter = TextureFilter.Trilinear;
                break;
        }
        Raylib.SetTextureFilter(texture, textureFilter);
    }

    public void RemoveMesh(uint id)
    {
        if (!_meshRegistry.ContainsKey(id)) return;
        Model model = _meshRegistry[id];
        Raylib.UnloadModel(model);
        _meshRegistry.Remove(id);
    }
    public void RemoveTexture(uint id)
    {
        if (!_textureRegistry.ContainsKey(id)) return;
        Texture2D texture = _textureRegistry[id];
        Raylib.UnloadTexture(texture);
        _textureRegistry.Remove(id);
    }
    public void RemoveFont(uint id)
    {
        if (!_fontRegistry.ContainsKey(id)) return;
        Font font = _fontRegistry[id];
        Raylib.UnloadFont(font);
        _fontRegistry.Remove(id);
    }

    public void BeginMode(ReconCamera3D camera) => Raylib.BeginMode3D(camera.RawCamera);
    public void DrawModel(uint modelId, Vector3 position, float scale)
    {
        if (!_meshRegistry.ContainsKey(modelId)) return;
        Model model = _meshRegistry[modelId];
        Raylib.DrawModel(model, position, scale, Color.White);
    }
    public void EndMode() => Raylib.EndMode3D();

    private Color Color4ToRaylibColor(Color4 color4) => new(color4.Red, color4.Green, color4.Blue, color4.Alpha);

    public void DrawTexture(uint textureId, int x, int y)
    {
        if (!_textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = _textureRegistry[textureId];
        Raylib.DrawTexture(texture, x, y, Color.White);
    }
    public void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode)
    {
        if (!_textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = _textureRegistry[textureId];
        Rectangle source = new();
        switch (scalingMode)
        {
            case TextureLabelScalingMode.Stretch:
                source = new(0, 0, texture.Dimensions);
                break;
            case TextureLabelScalingMode.Fit:
                float scaleFit = Math.Min((float)sx / texture.Width, (float)sy / texture.Height);
                float fitW = sx / scaleFit;
                float fitH = sy / scaleFit;
                source = new((texture.Width - fitW) / 2, (texture.Height - fitH) / 2, fitW, fitH);
                break;
            case TextureLabelScalingMode.Crop:
                float scaleCrop = Math.Max((float)sx / texture.Width, (float)sy / texture.Height);
                float cropW = sx / scaleCrop;
                float cropH = sy / scaleCrop;
                source = new((texture.Width - cropW) / 2, (texture.Height - cropH) / 2, cropW, cropH);
                break;
        }
        Rectangle dest = new(px, py, sx, sy);
        Vector2 origin = new(sx * anchor.X, sy * anchor.Y);
        Raylib.DrawTexturePro(texture, source, dest, origin, rotation, Color4ToRaylibColor(color));
    }
    public void DrawText(string text, int x, int y, byte textsize, Color4 color) => Raylib.DrawText(text, x, y, textsize, Color4ToRaylibColor(color));
    public void DrawText(string text, int x, int y, uint fontid, byte textsize, Color4 color, Vector2 anchor, float rotation)
    {
        bool success = _fontRegistry.TryGetValue(fontid, out Font font);
        if (!success) font = Raylib.GetFontDefault();
        Raylib.DrawTextPro(font, text, new Vector2(x, y), anchor, rotation, textsize, 0, Color4ToRaylibColor(color));
    }
    public void DrawRect(int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color)
    {
        Rectangle dest = new(px, py, sx, sy);
        Vector2 origin = new(sx * anchor.X, sy * anchor.Y);
        Raylib.DrawRectanglePro(dest, origin, rotation, Color4ToRaylibColor(color));
    }

    public void ClearBuffer() => Raylib.ClearBackground(Color.Black);
    public float GetFrameTime() => Raylib.GetFrameTime();

    public Vector2 GetScreenSize() => new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

    public Vector2 GetTextSize(string text, uint fontid, byte fontsize)
    {
        bool success = _fontRegistry.TryGetValue(fontid, out Font font);
        if (!success) font = Raylib.GetFontDefault();
        return Raylib.MeasureTextEx(font, text, fontsize, 0);
    }

    public Vector2 GetMousePosition() => Raylib.GetMousePosition();
}
