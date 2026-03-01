using System.Numerics;
using Raylib_cs;
using ReconEngine.RenderUtils;
using ReconEngine.System3D;
using ReconEngine.UISystem;
using StbImageSharp;

namespace ReconEngine.RenderingEngines;

public class RaylibRenderer : IRenderer
{
    private readonly Dictionary<uint, Model> _meshRegistry = [];
    private readonly Dictionary<uint, string> _meshPaths = [];
    private readonly Dictionary<uint, (Vector3 scale, Vector3 centerOffset)> _modelMetaCache = [];
    private readonly Dictionary<(uint meshId, uint textureId), Model> _materializedModels = [];
    private readonly Dictionary<(ReconShape3D shape, uint textureId), Model> _materializedShapes = [];
    private uint _meshRegistryCounter = 0;
    private readonly Dictionary<uint, Texture2D> _textureRegistry = [];
    private uint _textureRegistryCounter = 0;
    private readonly Dictionary<uint, Font> _fontRegistry = [];
    private uint _fontRegistryCounter = 0;
    private Shader _lightShader;

    private const int _max_lights = 32;
    private readonly int[] _enabledLocs = new int[_max_lights];
    private readonly int[] _typeLocs = new int[_max_lights];
    private readonly int[] _posLocs = new int[_max_lights];
    private readonly int[] _targetLocs = new int[_max_lights];
    private readonly int[] _colorLocs = new int[_max_lights];
    private readonly int[] _distanceLocs = new int[_max_lights];
    private readonly int[] _innerAngleLocs = new int[_max_lights];
    private readonly int[] _outerAngleLocs = new int[_max_lights];
    private int _viewPosLoc;
    private int _lightSpaceMatrixLoc;
    private int _shadowMapLoc;
    private int _ambientLoc;

    private readonly int[] _cascadeMatrixLocs = new int[4];
    private readonly int[] _cascadeShadowMapLocs = new int[4];
    private int _cascadeSplitsLoc;

    private readonly Dictionary<uint, int> _lightSlots = [];
    private readonly LightDefinition?[] _lightSlotData = new LightDefinition?[_max_lights];
    private uint _lightIdCounter = 0;

    private int _debugModeLoc;
    private LightingDebugMode _currentDebugMode = LightingDebugMode.None;

    private readonly RaylibShadowRenderer _shadowRenderer = new();
    private Camera3D _camera;

    public IShadowRenderer GetShadowMapRenderer() => _shadowRenderer;

    public void InitWindow(int width, int height, string title)
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.SetTraceLogLevel(TraceLogLevel.Error);

        Raylib.InitWindow(width, height, title);

        _lightShader = Raylib.LoadShader("assets/shaders/lighting.vert", "assets/shaders/lighting.frag");
        unsafe { _lightShader.Locs[(int)ShaderLocationIndex.VectorView] = Raylib.GetShaderLocation(_lightShader, "viewPos"); }

        _ambientLoc = Raylib.GetShaderLocation(_lightShader, "ambient");
        SetAmbientColor(new Color4(0.2f, 0.2f, 0.2f, 1.0f));

        _debugModeLoc = Raylib.GetShaderLocation(_lightShader, "debugMode");
        Raylib.SetShaderValue(_lightShader, _debugModeLoc, 0, ShaderUniformDataType.Int);

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

        _shadowRenderer.InitShadowMapShaders();
        _shadowRenderer.CreateShadowMap();

        for (int i = 0; i < 4; i++)
        {
            _cascadeMatrixLocs[i] = Raylib.GetShaderLocation(_lightShader, $"lightSpaceMatrices[{i}]");
            _cascadeShadowMapLocs[i] = Raylib.GetShaderLocation(_lightShader, $"shadowMaps[{i}]");
        }
        _cascadeSplitsLoc = Raylib.GetShaderLocation(_lightShader, "cascadeSplits");

        _viewPosLoc = Raylib.GetShaderLocation(_lightShader, "viewPos");
        _lightSpaceMatrixLoc = Raylib.GetShaderLocation(_lightShader, "lightSpaceMatrix");
        _shadowMapLoc = Raylib.GetShaderLocation(_lightShader, "shadowMap");

        LoadSkyboxShaders();
    }
    public void CloseWindow() => Raylib.CloseWindow();

    public void SetDebugMode(LightingDebugMode mode)
    {
        _currentDebugMode = mode;
        Raylib.SetShaderValue(_lightShader, _debugModeLoc, (int)mode, ShaderUniformDataType.Int);
    }
    public LightingDebugMode CycleDebugMode()
    {
        int next = ((int)_currentDebugMode + 1) % 11;
        SetDebugMode((LightingDebugMode)next);
        return _currentDebugMode;
    }

    public void BeginFrame()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F3))
        {
            var mode = CycleDebugMode();
            Console.WriteLine($"Debug mode: {mode}");
        }
        Raylib.BeginDrawing();
        if (_skyboxType == SkyboxType.SolidColor)
        {
            Raylib.ClearBackground(new Color(
                _skyboxColor1.Red * 255.0f,
                _skyboxColor1.Green * 255.0f,
                _skyboxColor1.Blue * 255.0f
            ));
        } else { Raylib.ClearBackground(Color.Black); }
    }

    public void EndFrame()
    {
        DrawDebugOverlay();
        Raylib.EndDrawing();
    }

    public bool ShouldClose() => Raylib.WindowShouldClose();

    private void GenerateModelMetaCache(Model model, uint id)
    {
        BoundingBox bounds = Raylib.GetModelBoundingBox(model);
        Vector3 modelSize = bounds.Max - bounds.Min;
        Vector3 centerOffset = (bounds.Min + bounds.Max) * 0.5f;
        _modelMetaCache.Add(id, (Vector3.One / modelSize, centerOffset)); // we save inverse size
    }
    public uint RegisterMesh(string filepath)
    {
        Model model = Raylib.LoadModel(filepath);
        if (model.MeshCount == 0)
        {
            Console.WriteLine($"FAILED TO LOAD MODEL: {filepath}");
            return 0;
        }
        _meshRegistryCounter++;
        _meshRegistry.Add(_meshRegistryCounter, model);
        _meshPaths.Add(_meshRegistryCounter, filepath);
        GenerateModelMetaCache(model, _meshRegistryCounter);
        return _meshRegistryCounter;
    }
    public uint RegisterTexture(string filepath)
    {
        Texture2D texture = Raylib.LoadTexture(filepath);
        if (texture.Id == 0)
        {
            Console.WriteLine($"FAILED TO LOAD TEXTURE: {filepath}");
            return 0;
        }
        Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
        Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
        _textureRegistryCounter++;
        _textureRegistry.Add(_textureRegistryCounter, texture);
        return _textureRegistryCounter;
    }
    public uint RegisterFont(string filepath)
    {
        Font font = Raylib.LoadFont(filepath);
        if (font.GlyphCount == 0)
        {
            Console.WriteLine($"FAILED TO LOAD FONT: {filepath}");
            return 0;
        }
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
            unsafe
            {
                Texture2D texture = _textureRegistry[textureId];
                Raylib.SetMaterialTexture(ref clone.Materials[0], MaterialMapIndex.Diffuse, texture);
            }
        _materializedModels[key] = clone;
        ApplyLightingShader(clone);
        return clone;
    }
    private Model GetMaterializedShape(ReconShape3D shape, uint textureId)
    {
        var key = (shape, textureId);
        if (_materializedShapes.TryGetValue(key, out Model existing))
            return existing;
        Model clone = Raylib.LoadModelFromMesh(shape switch
        {
            ReconShape3D.Box => Raylib.GenMeshCube(1, 1, 1),
            ReconShape3D.Sphere => Raylib.GenMeshSphere(1, 32, 32),
            ReconShape3D.Cone => Raylib.GenMeshCone(1, 32, 32),
            ReconShape3D.Cylinder => Raylib.GenMeshCylinder(1, 1, 32),
            ReconShape3D.Plane => Raylib.GenMeshPlane(1, 1, 1, 1),
            _ => Raylib.GenMeshCube(1, 1, 1),
        });
        if (textureId != 0)
            unsafe
            {
                Texture2D texture = _textureRegistry[textureId];
                Raylib.SetMaterialTexture(ref clone.Materials[0], MaterialMapIndex.Diffuse, texture);
            }
        _materializedShapes[key] = clone;
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

    public void BeginMode(ReconCamera3D camera)
    {
        Raylib.SetShaderValue(_lightShader, _viewPosLoc, camera.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(_lightShader, _cascadeSplitsLoc, _shadowRenderer.CascadeSplits, ShaderUniformDataType.Vec4);
        for (int i = 0; i < 4; i++)
        {
            Raylib.SetShaderValueMatrix(_lightShader, _cascadeMatrixLocs[i],
                _shadowRenderer.LightSpaceMatrices[i]);

            Rlgl.ActiveTextureSlot(1 + i);
            Rlgl.EnableTexture(_shadowRenderer.ShadowMaps[i].Texture.Id);
            Raylib.SetShaderValue(_lightShader, _cascadeShadowMapLocs[i], 1 + i, ShaderUniformDataType.Int);
        }
        _camera = new(camera.Position, camera.Target, camera.Up, camera.FovY, CameraProjection.Perspective);
        Raylib.BeginMode3D(_camera);

        if (_skyboxType == SkyboxType.Cubemap || _skyboxType == SkyboxType.HDRI)
        {
            Rlgl.DisableDepthMask();
            Rlgl.DisableBackfaceCulling();

            Raylib.DrawModel(_skyboxModel, Vector3.Zero, 1f, Color.White);

            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthMask();
        } else if (_skyboxType == SkyboxType.Gradient)
        {
            Rlgl.DisableDepthTest();
            Raylib.BeginShaderMode(_gradientShader);

            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Vertex2f(-1f, -1f);
            Rlgl.Vertex2f(1f, -1f);
            Rlgl.Vertex2f(1f, 1f);
            Rlgl.Vertex2f(-1f, 1f);
            Rlgl.End();

            Raylib.EndShaderMode();
            Rlgl.EnableDepthTest();
        }
    }
    public void DrawShape(ReconShape3D shape, uint textureId, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Model model = GetMaterializedShape(shape, textureId);
        float angle = 2f * MathF.Acos(rotation.W) * (180f / MathF.PI);
        Vector3 axis = angle == 0
            ? Vector3.UnitY
            : Vector3.Normalize(new Vector3(rotation.X, rotation.Y, rotation.Z));
        Raylib.DrawModelEx(model, position, axis, angle, size, Color.White);
    }
    public void DrawModel(uint modelId, uint textureId, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Model model = GetMaterializedModel(modelId, textureId);
        float angle = 2f * MathF.Acos(rotation.W) * (180f / MathF.PI);
        Vector3 axis = angle == 0
            ? Vector3.UnitY
            : Vector3.Normalize(new Vector3(rotation.X, rotation.Y, rotation.Z));
        _modelMetaCache.TryGetValue(modelId, out (Vector3 scale, Vector3 centerOffset) value);
        Vector3 scale = new(
            size.X * value.scale.X,
            size.Y * value.scale.Y,
            size.Z * value.scale.Z
        );
        position -= value.centerOffset * scale;
        Raylib.DrawModelEx(model, position, axis, angle, scale, Color.White);
    }
    private readonly Shader[] _depthShaderSwapBuffer = new Shader[16];
    public void DrawShapeDepth(ReconShape3D shape, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Model model = GetMaterializedShape(shape, 0);
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe
        {
            _depthShaderSwapBuffer[i] = model.Materials[i].Shader;
            model.Materials[i].Shader = _shadowRenderer.DepthShader;
        }
        DrawShape(shape, 0, position, rotation, size);
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe
        {
            model.Materials[i].Shader = _depthShaderSwapBuffer[i];
        }
    }
    public void DrawModelDepth(uint modelId, Vector3 position, Quaternion rotation, Vector3 size)
    {
        Model model = GetMaterializedModel(modelId, 0);
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe {
            _depthShaderSwapBuffer[i] = model.Materials[i].Shader;
            model.Materials[i].Shader = _shadowRenderer.DepthShader;
        }
        DrawModel(modelId, 0, position, rotation, size);
        for (int i = 0; i < model.MaterialCount; i++)
        unsafe {
            model.Materials[i].Shader = _depthShaderSwapBuffer[i];
        }
    }

    public void DrawLine3D(Vector3 posA, Vector3 posB, Color4 color) => Raylib.DrawLine3D(posA, posB, Color4ToRaylibColor(color));

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

    private Color4 _ambientColor;
    private SkyboxType _skyboxType = SkyboxType.SolidColor;
    private string _skyboxTexture = "";
    private Color4 _skyboxColor1 = Color4.Black;
    private Color4 _skyboxColor2 = Color4.Black; 

    private Shader _gradientShader;
    private int _gradientBottomColorLoc;
    private int _gradientTopColorLoc;

    private Model _skyboxModel;

    private Shader _skyboxShaderHDRI;
    private int _envMapLocHDRI;
    private int _vFlippedLocHDRI;
    private int _doGammaLocHDRI;

    private Shader _skyboxShaderCUBE;
    private int _envMapLocCUBE;
    private int _vFlippedLocCUBE;
    private int _doGammaLocCUBE;

    private Texture2D _cubemap;

    private bool _gammaEnabled = false; // only applies to HDRI skyboxes

    public void SetAmbientColor(Color4 ambient)
    {
        _ambientColor = ambient;
        Raylib.SetShaderValue(_lightShader, _ambientLoc, ambient.AsVec4(), ShaderUniformDataType.Vec4);
    }
    public Color4 GetAmbientColor() => _ambientColor;

    public void EnableHDRIGammaCorrection(bool enabled) => _gammaEnabled = enabled;

    public void LoadTextureSkybox(SkyboxType type, string texture)
    {
        _skyboxType = type;
        _skyboxTexture = texture;
        switch (type)
        {
            case SkyboxType.Cubemap:
                LoadCubemapSkybox();
                break;

            case SkyboxType.HDRI:
                LoadHDRI();
                break;

            default:
                Console.WriteLine("[RAYLIB:Renderer] Tried to load Solid/Gradient skybox with the texture loader");
                break;
        }
    }

    public void LoadSolidSkybox(Color4 color)
    {
        _skyboxColor1 = color;
        _skyboxType = SkyboxType.SolidColor;
    }
    public void LoadGradientSkybox(Color4 top, Color4 bottom)
    {
        _skyboxColor1 = top;
        _skyboxColor2 = bottom;
        _skyboxType = SkyboxType.Gradient;
        Raylib.SetShaderValue(_gradientShader, _gradientTopColorLoc, top.AsVec4(), ShaderUniformDataType.Vec4);
        Raylib.SetShaderValue(_gradientShader, _gradientBottomColorLoc, bottom.AsVec4(), ShaderUniformDataType.Vec4);
    }

    public (SkyboxType type, string texture) GetSkybox() => (_skyboxType, _skyboxTexture);
    public (Color4 top, Color4 bottom) GetSkyboxColors() => (_skyboxColor1, _skyboxColor2);

    private void LoadSkyboxShaders()
    {
        _skyboxShaderHDRI = Raylib.LoadShader("assets/shaders/skybox.vert", "assets/shaders/skybox_hdri.frag");
        _envMapLocHDRI = Raylib.GetShaderLocation(_skyboxShaderHDRI, "envMap");
        _vFlippedLocHDRI = Raylib.GetShaderLocation(_skyboxShaderHDRI, "vFlipped");
        _doGammaLocHDRI = Raylib.GetShaderLocation(_skyboxShaderHDRI, "doGamma");

        _skyboxShaderCUBE = Raylib.LoadShader("assets/shaders/skybox.vert", "assets/shaders/skybox_cubemap.frag");
        _envMapLocCUBE = Raylib.GetShaderLocation(_skyboxShaderCUBE, "envMap");
        _vFlippedLocCUBE = Raylib.GetShaderLocation(_skyboxShaderCUBE, "vFlipped");
        _doGammaLocCUBE = Raylib.GetShaderLocation(_skyboxShaderCUBE, "doGamma");

        const string gradVs = @"
            #version 330
            in vec2 vertexPosition;
            out vec2 fragTexCoord;
            void main() {
                fragTexCoord = vertexPosition * 0.5 + 0.5;
                gl_Position = vec4(vertexPosition, 0.0, 1.0);
            }";
        const string gradFs = @"
            #version 330
            in vec2 fragTexCoord;
            uniform vec4 topColor;
            uniform vec4 bottomColor;
            out vec4 finalColor;
            void main() {
                finalColor = mix(bottomColor, topColor, fragTexCoord.y);
            }";
        _gradientShader = Raylib.LoadShaderFromMemory(gradVs, gradFs);
        _gradientBottomColorLoc = Raylib.GetShaderLocation(_gradientShader, "bottomColor");
        _gradientTopColorLoc = Raylib.GetShaderLocation(_gradientShader, "topColor");
    }

    private void LoadCubemapSkybox()
    {
        if (_skyboxTexture == "") return; 
        int envMap = (int)MaterialMapIndex.Cubemap;
        Raylib.SetShaderValue(_skyboxShaderCUBE, _envMapLocCUBE, envMap, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(_skyboxShaderCUBE, _vFlippedLocCUBE, 0, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(_skyboxShaderCUBE, _doGammaLocCUBE, 0, ShaderUniformDataType.Int);

        Image img = Raylib.LoadImage(_skyboxTexture);
        _cubemap = Raylib.LoadTextureCubemap(img, CubemapLayout.AutoDetect);
        Raylib.UnloadImage(img);

        _skyboxModel = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(1, 1, 1));
        Raylib.SetMaterialShader(ref _skyboxModel, 0, ref _skyboxShaderCUBE);
        Raylib.SetMaterialTexture(ref _skyboxModel, 0, MaterialMapIndex.Cubemap, ref _cubemap);
    }

    private unsafe void LoadHDRI()
    {
        if (_skyboxTexture == "") return;

        using var stream = File.OpenRead(_skyboxTexture);
        ImageResultFloat hdrImage = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlue);

        Texture2D panorama = new();
        fixed (float* ptr = hdrImage.Data)
        {
            panorama.Id = Rlgl.LoadTexture(ptr, hdrImage.Width, hdrImage.Height,
                                            PixelFormat.UncompressedR32G32B32, 1);
        }
        panorama.Width = hdrImage.Width;
        panorama.Height = hdrImage.Height;
        panorama.Format = PixelFormat.UncompressedR32G32B32;
        panorama.Mipmaps = 1;

        int envMap = 0;
        Raylib.SetShaderValue(_skyboxShaderHDRI, _envMapLocHDRI, envMap, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(_skyboxShaderHDRI, _vFlippedLocHDRI, 1, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(_skyboxShaderHDRI, _doGammaLocHDRI, _gammaEnabled ? 1 : 0, ShaderUniformDataType.Int);

        _skyboxModel = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(1f, 1f, 1f));
        Raylib.SetMaterialShader(ref _skyboxModel, 0, ref _skyboxShaderHDRI);

        Raylib.SetMaterialTexture(ref _skyboxModel, 0, MaterialMapIndex.Diffuse, ref panorama);

        _cubemap = panorama;
    }

    public void DrawDebugOverlay()
    {
        if (_currentDebugMode == LightingDebugMode.None) return;
        string label = _currentDebugMode switch
        {
            LightingDebugMode.Normals => "DEBUG: Normals",
            LightingDebugMode.UVs => "DEBUG: UVs",
            LightingDebugMode.BaseTexture => "DEBUG: Base Texture (unlit)",
            LightingDebugMode.ShadowProjectedUVs => "DEBUG: Shadow Projected UVs (R=U, G=V)",
            LightingDebugMode.LightOnly => "DEBUG: Light Contribution",
            LightingDebugMode.ShadowFactor => "DEBUG: Shadow Factor",
            LightingDebugMode.RawLightSpacePos => "DEBUG: Raw LightSpace Pos (fract)",
            LightingDebugMode.LightSpaceW => "DEBUG: LightSpace W component",
            LightingDebugMode.ShadowMapDepth => "DEBUG: Shadow Map Depth at projected UV",
            LightingDebugMode.CascadeDebug => "DEBUG: Cascade distance",
            _ => "DEBUG: Unknown"
        };
        Raylib.DrawText(label, 10, 30, 20, Color.Yellow);
    }
}
