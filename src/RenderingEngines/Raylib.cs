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
    private readonly Dictionary<uint, string> _meshPaths = [];
    private readonly Dictionary<(uint meshId, uint textureId), Model> _materializedModels = new();
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
        Raylib.DisableCursor();

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
        _meshPaths.Add(_meshRegistryCounter, filepath);
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

    private Model GetMaterializedModel(uint modelId, uint textureId)
    {
        var key = (modelId, textureId);
        if (_materializedModels.TryGetValue(key, out Model existing))
            return existing;
        string path = _meshPaths[modelId];
        Model clone = Raylib.LoadModel(path);
        if (textureId != 0)
        unsafe {
            Texture2D texture = _textureRegistry[textureId];
            Raylib.SetMaterialTexture(ref clone.Materials[0], MaterialMapIndex.Diffuse, texture);
        }
        _materializedModels[key] = clone;
        ApplyLightingShader(clone);
        return clone;
    }

    private void ApplyLightingShader(Model model)
    {
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe
        {
            model.Materials[i].Shader = _lightShader;
        }
    }
    
    public void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode)
    {
        if (!_textureRegistry.TryGetValue(textureId, out Texture2D texture)) return;
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
        if (!_meshRegistry.TryGetValue(id, out Model model)) return;
        Raylib.UnloadModel(model);
        _meshRegistry.Remove(id);
    }
    public void RemoveTexture(uint id)
    {
        if (!_textureRegistry.TryGetValue(id, out Texture2D texture)) return;
        Raylib.UnloadTexture(texture);
        _textureRegistry.Remove(id);
    }
    public void RemoveFont(uint id)
    {
        if (!_fontRegistry.TryGetValue(id, out Font font)) return;
        Raylib.UnloadFont(font);
        _fontRegistry.Remove(id);
    }

    public void BeginMode(ReconCamera3D camera) => Raylib.BeginMode3D(camera.RawCamera);
    public void DrawModel(uint modelId, uint textureId, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Model model = GetMaterializedModel(modelId, textureId);
        float angle = 2f * MathF.Acos(rotation.W) * (180f / MathF.PI);
        Vector3 axis = angle == 0
            ? Vector3.UnitY
            : Vector3.Normalize(new Vector3(rotation.X, rotation.Y, rotation.Z));
        Raylib.DrawModelEx(model, position, axis, angle, scale, Color.White);
    }
    public void EndMode() => Raylib.EndMode3D();

    private Color Color4ToRaylibColor(Color4 color4) => new(color4.Red, color4.Green, color4.Blue, color4.Alpha);

    public void DrawTexture(uint textureId, int x, int y)
    {
        if (!_textureRegistry.TryGetValue(textureId, out Texture2D texture)) return;
        Raylib.DrawTexture(texture, x, y, Color.White);
    }
    public void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode)
    {
        if (!_textureRegistry.TryGetValue(textureId, out Texture2D texture)) return;
        Rectangle source = new();
        Rectangle dest = new(px, py, sx, sy);
        Vector2 origin = new(sx * anchor.X, sy * anchor.Y);
        switch (scalingMode)
        {
            case TextureLabelScalingMode.Stretch:
                source = new(0, 0, texture.Dimensions);
                break;
            case TextureLabelScalingMode.Fit:
                float scaleFit = Math.Min((float)sx / texture.Width, (float)sy / texture.Height);
                float fitW = texture.Width * scaleFit;
                float fitH = texture.Height * scaleFit;
                source = new(0, 0, texture.Width, texture.Height);
                dest = new(px + (sx - fitW) * .5f, py + (sy - fitH) * .5f, fitW, fitH);
                break;
            case TextureLabelScalingMode.Crop:
                float scaleCrop = 1 / Math.Max((float)sx / texture.Width, (float)sy / texture.Height);
                float cropW = sx * scaleCrop;
                float cropH = sy * scaleCrop;
                source = new((texture.Width - cropW) * .5f, (texture.Height - cropH) * .5f, cropW, cropH);
                break;
        }
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
