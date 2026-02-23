using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine.System3D;

public class ReconEntity3D : ReconEntity
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;

    public Vector3 RotationEuler
    {
        get => QuaternionToEuler(Rotation);
        set => Rotation = EulerToQuaternion(value);
    }

    public Matrix4x4 LocalTransform =>
        Matrix4x4.CreateFromQuaternion(Rotation) *
        Matrix4x4.CreateTranslation(Position);

    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);

    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        float sinr = 2f * (q.W * q.X + q.Y * q.Z);
        float cosr = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        float roll = MathF.Atan2(sinr, cosr);

        float sinp = 2f * (q.W * q.Y - q.Z * q.X);
        float pitch = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

        float siny = 2f * (q.W * q.Z + q.X * q.Y);
        float cosy = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
        float yaw = MathF.Atan2(siny, cosy);

        const float toDeg = 180f / MathF.PI;
        return new Vector3(roll * toDeg, pitch * toDeg, yaw * toDeg);
    }

    private static Quaternion EulerToQuaternion(Vector3 eulerDeg)
    {
        const float toRad = MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(
            eulerDeg.Z * toRad, eulerDeg.Y * toRad, eulerDeg.X * toRad);
    }
}