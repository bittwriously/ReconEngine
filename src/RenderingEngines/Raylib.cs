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
    private readonly Dictionary<(uint meshId, uint textureId), Model> _materializedModels = [];
    private uint _meshRegistryCounter = 0;
    private readonly Dictionary<uint, Texture2D> _textureRegistry = [];
    private uint _textureRegistryCounter = 0;
    private readonly Dictionary<uint, Font> _fontRegistry = [];
    private uint _fontRegistryCounter = 0;
    private Shader _lightShader;

    private const int _max_lights = 256;
    private readonly int[] _enabledLocs = new int[_max_lights];
    private readonly int[] _typeLocs = new int[_max_lights];
    private readonly int[] _posLocs = new int[_max_lights];
    private readonly int[] _targetLocs = new int[_max_lights];
    private readonly int[] _colorLocs = new int[_max_lights];
    private readonly int[] _distanceLocs = new int[_max_lights];
    private readonly int[] _innerAngleLocs = new int[_max_lights];
    private readonly int[] _outerAngleLocs = new int[_max_lights];

    private readonly Dictionary<uint, int> _lightSlots = [];
    private readonly LightDefinition?[] _lightSlotData = new LightDefinition?[_max_lights];
    private uint _lightIdCounter = 0;

    public void InitWindow(int width, int height, string title)
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);

        Raylib.InitWindow(width, height, title);
        Raylib.DisableCursor();

        _lightShader = Raylib.LoadShader("assets/shaders/lighting.vs", "assets/shaders/lighting.fs");
        unsafe { _lightShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(_lightShader, "viewPos"); }

        int ambientLoc = Raylib.GetShaderLocation(_lightShader, "ambient");
        Raylib.SetShaderValue(_lightShader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, ShaderUniformDataType.Vec4);
        
        for (int i = 0; i < _max_lights; i++)
        {
            _enabledLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].enabled");
            _typeLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].type");
            _posLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].position");
            _targetLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].target");
            _colorLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].color");
            _distanceLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].distance");
            _innerAngleLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].innerAngle");
            _outerAngleLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lights[{i}].outerAngle");
        }

        for (int i = 0; i < _max_lights; i++) UploadLightSlot(i, null);
    }
    public void CloseWindow() => Raylib.CloseWindow();

    public void BeginFrame()
    {
        Raylib.BeginDrawing();
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

    private static Vector3 GetScaleForSize(Model model, Vector3 targetSize, bool uniform)
    {
        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 modelSize = bounds.Max - bounds.Min;
        if (uniform)
        {
            float scaleX = targetSize.X / modelSize.X;
            float scaleY = targetSize.Y / modelSize.Y;
            float scaleZ = targetSize.Z / modelSize.Z;
            float uniformScale = MathF.Min(scaleX, MathF.Min(scaleY, scaleZ));
            return new Vector3(uniformScale);
        }
        else
        {
            return new Vector3(
                targetSize.X / modelSize.X,
                targetSize.Y / modelSize.Y,
                targetSize.Z / modelSize.Z
            );
        }
    }
    private static Vector3 GetModelCenter(Model model)
    {
        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        return (bounds.Min + bounds.Max) * 0.5f;
    }

    public void BeginMode(ReconCamera3D camera)
    {
        unsafe { _lightShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(_lightShader, "viewPos"); }
        Raylib.SetShaderValue(_lightShader, Raylib.GetShaderLocation(_lightShader, "viewPos"), camera.RawCamera.Position, ShaderUniformDataType.Vec3);
        Raylib.BeginMode3D(camera.RawCamera);
    }
    public void DrawModel(uint modelId, uint textureId, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Model model = GetMaterializedModel(modelId, textureId);
        float angle = 2f * MathF.Acos(rotation.W) * (180f / MathF.PI);
        Vector3 axis = angle == 0
            ? Vector3.UnitY
            : Vector3.Normalize(new Vector3(rotation.X, rotation.Y, rotation.Z));
        Vector3 scale = GetScaleForSize(model, size, false);
        position -= GetModelCenter(model) * scale;
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

    public uint AddLight(LightDefinition def)
    {
        int slot = Array.IndexOf(_lightSlotData, null);
        if (slot == -1) throw new InvalidOperationException($"maximum light count '{_max_lights}' reached");
        _lightIdCounter++;
        _lightSlots[_lightIdCounter] = slot;
        _lightSlotData[slot] = def;
        UploadLightSlot(slot, def);
        return _lightIdCounter;
    }

    public void UpdateLight(uint lightId, LightDefinition def)
    {
        if (!_lightSlots.TryGetValue(lightId, out int slot)) return;
        _lightSlotData[slot] = def;
        UploadLightSlot(slot, def);
    }

    public void RemoveLight(uint lightId)
    {
        if (!_lightSlots.TryGetValue(lightId, out int slot)) return;
        _lightSlotData[slot] = null;
        _lightSlots.Remove(lightId);
        UploadLightSlot(slot, null);
    }

    private void UploadLightSlot(int slot, LightDefinition? def)
    {
        bool enabled = def is { Enabled: true };
        Raylib.SetShaderValue(_lightShader, _enabledLocs[slot], enabled ? 1 : 0, ShaderUniformDataType.Int);
        if (!enabled) return;
        var d = def!.Value;
        Raylib.SetShaderValue(_lightShader, _typeLocs[slot], (int)d.Type, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(_lightShader, _posLocs[slot], d.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(_lightShader, _targetLocs[slot], d.Direction, ShaderUniformDataType.Vec3);
        Vector4 c = new(d.Color.Red, d.Color.Green, d.Color.Blue, d.Color.Alpha);
        Raylib.SetShaderValue(_lightShader, _colorLocs[slot], c, ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(_lightShader, _distanceLocs[slot], d.Distance, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_lightShader, _innerAngleLocs[slot], float.DegreesToRadians(d.InnerAngle), ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_lightShader, _outerAngleLocs[slot], float.DegreesToRadians(d.OuterAngle), ShaderUniformDataType.Float);
    }

    public Vector2 GetMousePosition() => Raylib.GetMousePosition();
}
