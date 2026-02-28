using System.Drawing;
using System.Numerics;
using Jitter2.Collision.Shapes;
using ReconEngine.InputSystem;
using ReconEngine.WorldSystem;

namespace ReconEngine.System3D;

public enum CameraMode
{
    Fixed = 0, // uses Position and Orientation values to set camera position
    Track = 1, // tracks Subject with Position / Orientation offset
    Freecam = 2, // activates the built in freecam
}

public class ReconCamera3D : ReconEntity
{
    public Vector3 Position { get; private set; } = new(0, 0, 0);
    public Vector3 Target { get; private set; } = Vector3.Zero;
    public Vector3 Up { get; private set; } = Vector3.UnitY;
    public float FovY = 70f;
    public float NearPlane = 0.1f;
    public float FarPlane = 1000f;
    public Vector2 ViewportSize = new(800f, 600f);

    public Vector3 PositionOffset = Vector3.Zero;
    public Vector3 EulerAngles = Vector3.Zero;

    public ReconEntity3D? Subject;

    // freecam state
    public float FreecamMoveSpeed = 10f;
    public float FreecamLookSpeed = 0.2f;
    public float FreecamSprintMultiplier = 3f;

    private float _freecamYaw = 0f;
    private float _freecamPitch = 0f;
    private Vector3 _freecamPosition = Vector3.Zero;

    public CameraMode Mode = CameraMode.Fixed;

    public override void RenderStep(float deltaTime, IRenderer renderer)
    {
        switch (Mode)
        {
            case CameraMode.Fixed:
                UpdateFixed();
                break;
            case CameraMode.Track:
                UpdateTrack();
                break;
            case CameraMode.Freecam:
                UpdateFreecam(deltaTime);
                break;
        }
    }
    public override void Ready()
    {
        base.Ready();

        AncestryChanged += (sender, oldWorld) =>
        {
            oldWorld?.CurrentCamera = null;
            CurrentWorld?.CurrentCamera = this;
        };
    }

    private void UpdateFixed()
    {
        Position = PositionOffset;
        Up = ComputeUp(EulerAngles);
        Target = Position + ComputeForward(EulerAngles);
    }

    private void UpdateTrack()
    {
        if (Subject == null)
        {
            // no subject so we fall back to Fixed
            UpdateFixed();
            return;
        }
        Quaternion orientation = EulerToQuaternion(EulerAngles);
        Vector3 rotatedOffset = Vector3.Transform(PositionOffset, orientation);
        Position = Subject.Position + rotatedOffset;
        Target = Subject.Position;
        Up = ComputeUp(EulerAngles);
    }

    private void UpdateFreecam(float deltaTime)
    {
        Vector2 mouseDelta = ReconInputSystem.MouseHandler.GetMouseMovement();
        _freecamYaw -= mouseDelta.X * FreecamLookSpeed;
        _freecamPitch -= mouseDelta.Y * FreecamLookSpeed;
        _freecamPitch = Math.Clamp(_freecamPitch, -89f, 89f); // prevent flipping

        Vector3 forward = FreecamForward();
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        Vector3 move = Vector3.Zero;

        if (ReconInputSystem.IsKeyHeld(ReconKey.W)) move += forward;
        if (ReconInputSystem.IsKeyHeld(ReconKey.S)) move -= forward;
        if (ReconInputSystem.IsKeyHeld(ReconKey.D)) move += right;
        if (ReconInputSystem.IsKeyHeld(ReconKey.A)) move -= right;
        if (ReconInputSystem.IsKeyHeld(ReconKey.E)) move += up;
        if (ReconInputSystem.IsKeyHeld(ReconKey.Q)) move -= up;

        if (move != Vector3.Zero)
            move = Vector3.Normalize(move);

        float speed = FreecamMoveSpeed;
        if (ReconInputSystem.IsKeyHeld(ReconKey.LeftShift))
            speed *= FreecamSprintMultiplier;

        _freecamPosition += move * speed * deltaTime;

        Position = _freecamPosition;
        Target = _freecamPosition + forward;
        Up = Vector3.UnitY;
    }

    private static Quaternion EulerToQuaternion(Vector3 eulerDeg)
    {
        const float toRad = MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(
            eulerDeg.Z * toRad, eulerDeg.Y * toRad, eulerDeg.X * toRad);
    }


    private Vector3 FreecamForward()
    {
        float yawRad = _freecamYaw * (MathF.PI / 180f);
        float pitchRad = _freecamPitch * (MathF.PI / 180f);

        return Vector3.Normalize(new Vector3(
            MathF.Cos(pitchRad) * MathF.Sin(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Cos(yawRad)
        ));
    }

    private static Vector3 ComputeForward(Vector3 eulerDeg)
    {
        float yawRad = eulerDeg.Y * (MathF.PI / 180f);
        float pitchRad = eulerDeg.X * (MathF.PI / 180f);

        return Vector3.Normalize(new Vector3(
            MathF.Cos(pitchRad) * MathF.Sin(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Cos(yawRad)
        ));
    }

    private static Vector3 ComputeUp(Vector3 eulerDeg)
    {
        float rollRad = eulerDeg.Z * (MathF.PI / 180f);
        return Vector3.Normalize(new Vector3(
            -MathF.Sin(rollRad),
             MathF.Cos(rollRad),
             0f
        ));
    }

}
