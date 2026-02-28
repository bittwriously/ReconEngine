using System.Numerics;
using Raylib_cs;
using ReconEngine.RenderUtils;

namespace ReconEngine.RenderingEngines;

public class RaylibShadowRenderer : IShadowRenderer
{
    internal Shader DepthShader;
    private int _lightSpaceLoc;

    public static readonly int[] CascadeResolutions = [2048, 2048, 1024, 512];

    public const int CASCADE_COUNT = 4;
    public int CascadeCount => CASCADE_COUNT;
    public float[] CascadeSplits => [16f, 64f, 128f, 256f];
    public Matrix4x4[] LightSpaceMatrices => _lightSpaceMatrices;
    public RenderTexture2D[] ShadowMaps => _shadowMaps;

    internal readonly RenderTexture2D[] _shadowMaps = new RenderTexture2D[CASCADE_COUNT];
    internal readonly Matrix4x4[] _lightSpaceMatrices = new Matrix4x4[CASCADE_COUNT];

    private Vector3 _sunDir;

    public void InitShadowMapShaders()
    {
        DepthShader = Raylib.LoadShader(
            "assets/shaders/depth.vert",
            "assets/shaders/depth.frag"
        );
        _lightSpaceLoc = Raylib.GetShaderLocation(DepthShader, "lightSpaceMatrix");
    }

    private static RenderTexture2D CreateDepthMap(int width, int height)
    {
        RenderTexture2D map = new();
        uint fboId = Rlgl.LoadFramebuffer();
        Rlgl.EnableFramebuffer(fboId);
        uint depthTexId = Rlgl.LoadTextureDepth(width, height, false);
        unsafe
        {
            map.Id = fboId;
            map.Texture.Id = depthTexId;
            map.Texture.Width = width;
            map.Texture.Height = height;
        }
        Rlgl.FramebufferAttach(fboId, depthTexId, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
        Rlgl.DisableFramebuffer();
        return map;
    }
    public void CreateShadowMap()
    {
        for (int i = 0; i < CascadeCount; i++)
            ShadowMaps[i] = CreateDepthMap(CascadeResolutions[i], CascadeResolutions[i]);
    }

    public void UpdateSun(LightDefinition? light) => _sunDir = Vector3.Normalize(light?.Direction ?? -Vector3.UnitY);

    private Matrix4x4 ComputeLightSpaceMatrix(float nearSplit, float farSplit, Vector3 cameraPos)
    {
        float pullback = farSplit + 50f;
        Vector3 sunPos = cameraPos - _sunDir * pullback;

        Camera3D sunCam = new();
        sunCam.Position = sunPos;
        sunCam.Target = cameraPos;
        sunCam.Up = MathF.Abs(_sunDir.Y) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        sunCam.Projection = CameraProjection.Orthographic;

        Matrix4x4 lightView = Raylib.GetCameraMatrix(sunCam);

        float orthoSize = farSplit * 2f;

        Matrix4x4 lightProj = Matrix4x4.CreateOrthographic(
            orthoSize,
            orthoSize,
            0.1f,
            pullback + farSplit
        );

        return lightProj * lightView;
    }

    public void UpdateMatrices(Vector3 cameraPos)
    {
        float nearSplit = 0f;
        for (int i = 0; i < CascadeCount; i++)
        {
            LightSpaceMatrices[i] = ComputeLightSpaceMatrix(nearSplit, CascadeSplits[i], cameraPos);
            nearSplit = CascadeSplits[i];
        }
    }

    public void BeginCascade(int index)
    {
        Raylib.SetShaderValueMatrix(DepthShader, _lightSpaceLoc, LightSpaceMatrices[index]);
        Raylib.BeginTextureMode(ShadowMaps[index]);
        Raylib.ClearBackground(Color.White);
        Rlgl.SetCullFace(0);

        Camera3D dummyCam = new();
        dummyCam.Position = -_sunDir * 100f;
        dummyCam.Target = Vector3.Zero;
        dummyCam.Up = MathF.Abs(_sunDir.Y) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
        dummyCam.Projection = CameraProjection.Orthographic;
        Raylib.BeginMode3D(dummyCam);
    }

    public void EndCascade()
    {
        Rlgl.SetCullFace(1);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
    }

    public void DrawDebugQuad(int x = 10, int y = 40, int size = 128)
    {
        for (int i = 0; i < CascadeCount; i++)
        {
            int xOffset = x + i * (size + 10);
            Rectangle source = new(0, 0, ShadowMaps[i].Texture.Width, -ShadowMaps[i].Texture.Height);
            Rectangle dest = new(xOffset, y, size, size);
            Raylib.DrawTexturePro(ShadowMaps[i].Texture, source, dest, Vector2.Zero, 0f, Color.White);
            Raylib.DrawRectangleLines(xOffset, y, size, size, Color.Green);
            Raylib.DrawText($"C{i} ({CascadeSplits[i]}u)", xOffset, y - 16, 14, Color.Green);
        }
    }
}
