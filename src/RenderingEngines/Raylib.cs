using Raylib_cs;
using ReconEngine.RenderUtils;
using System.Numerics;
using System.Collections;
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
    private readonly Dictionary<uint, Model> meshRegistry = [];
    private uint meshRegistryCounter = 0;
    private readonly Dictionary<uint, Texture2D> textureRegistry = [];
    private uint textureRegistryCounter = 0;
    private readonly Dictionary<uint, Font> fontRegistry = [];
    private uint fontRegistryCounter = 0;
    private RaylibLight sun;
    private Shader lightShader;

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

    public void InitWindow(int width, int height, string title) {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);

        Raylib.InitWindow(width, height, title);
        //Raylib.DisableCursor();

        lightShader = Raylib.LoadShader("assets/shaders/lighting.vs", "assets/shaders/lighting.fs");
        unsafe { lightShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(lightShader, "viewPos"); }

        int ambientLoc = Raylib.GetShaderLocation(lightShader, "ambient");
        Raylib.SetShaderValue(lightShader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, ShaderUniformDataType.Vec4);

        sun = new() {
            Enabled = true,
            Type = RaylibLightType.Directional,
            Position = new Vector3(50, 100, 50),
            Target = Vector3.Zero,
            Color = Color.White
        };
        InitLight(ref sun, lightShader);
    }
    public void CloseWindow() => Raylib.CloseWindow();
    
    public void BeginFrame() {
        Raylib.BeginDrawing();
        UpdateLightValues(sun, lightShader);
    }
    public void EndFrame() => Raylib.EndDrawing();

    public bool ShouldClose() => Raylib.WindowShouldClose();

    public uint RegisterMesh(string filepath)
    {
        Model model = Raylib.LoadModel(filepath);
        meshRegistryCounter++;
        meshRegistry.Add(meshRegistryCounter, model);
        return meshRegistryCounter;
    }
    public uint RegisterTexture(string filepath)
    {
        Texture2D texture = Raylib.LoadTexture(filepath);
        Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
        Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
        textureRegistryCounter++;
        textureRegistry.Add(textureRegistryCounter, texture);
        return textureRegistryCounter;
    }
    public uint RegisterFont(string filepath)
    {
        Font font = Raylib.LoadFont(filepath);
        fontRegistryCounter++;
        fontRegistry.Add(fontRegistryCounter, font);
        return fontRegistryCounter;
    }

    public void ApplyLightingShader(uint modelId)
    {
        if (!meshRegistry.ContainsKey(modelId)) return;
        Model model = meshRegistry[modelId];
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe {
            model.Materials[i].Shader = lightShader;
        }
    }
    public void SetTextureSamplingMode(uint textureId, ETextureSamplingMode samplingMode)
    {
        if (!textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = textureRegistry[textureId];
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
        if (!meshRegistry.ContainsKey(id)) return;
        Model model = meshRegistry[id];
        Raylib.UnloadModel(model);
        meshRegistry.Remove(id);
    }
    public void RemoveTexture(uint id)
    {
        if (!textureRegistry.ContainsKey(id)) return;
        Texture2D texture = textureRegistry[id];
        Raylib.UnloadTexture(texture);
        textureRegistry.Remove(id);
    }
    public void RemoveFont(uint id)
    {
        if (!fontRegistry.ContainsKey(id)) return;
        Font font = fontRegistry[id];
        Raylib.UnloadFont(font);
        fontRegistry.Remove(id);
    }

    public void BeginMode(ReconCamera3D camera) => Raylib.BeginMode3D(camera.RawCamera);
    public void DrawModel(uint modelId, Vector3 position, float scale)
    {
        if (!meshRegistry.ContainsKey(modelId)) return;
        Model model = meshRegistry[modelId];
        Raylib.DrawModel(model, position, scale, Color.White);
    }
    public void EndMode() => Raylib.EndMode3D();

    private Color Color4ToRaylibColor(Color4 color4) => new(color4.Red, color4.Green, color4.Blue, color4.Alpha);

    public void DrawTexture(uint textureId, int x, int y)
    {
        if (!textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = textureRegistry[textureId];
        Raylib.DrawTexture(texture, x, y, Color.White);
    }
    public void DrawTexture(uint textureId, int px, int py, int sx, int sy, float rotation, Vector2 anchor, Color4 color, TextureLabelScalingMode scalingMode)
    {
        if (!textureRegistry.ContainsKey(textureId)) return;
        Texture2D texture = textureRegistry[textureId];
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
        bool success = fontRegistry.TryGetValue(fontid, out Font font);
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
        bool success = fontRegistry.TryGetValue(fontid, out Font font);
        if (!success) font = Raylib.GetFontDefault();
        return Raylib.MeasureTextEx(font, text, fontsize, 0);
    }

    public Vector2 GetMousePosition() => Raylib.GetMousePosition();
}