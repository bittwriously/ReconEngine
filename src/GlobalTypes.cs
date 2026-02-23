using System.Numerics;
using ReconEngine.WorldSystem;

namespace ReconEngine;

public static class ReconMath
{
    public static float PI = 3.14159265359f;
    private static readonly float _deg2rad = PI / 180;
    private static readonly float _rad2deg = 180 / PI;

    public static float Deg2Rad(float degrees) { return degrees * _deg2rad; }
    public static float Rad2Deg(float radians) { return radians * _rad2deg; }

    public static float LerpFloat(float first, float second, float alpha) { return first + (second - first) * alpha; }
    public static float OffsetMidWay(float value, float offset)
    {
        if (value > .5f) return value - offset;
        else return value + offset;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }

    public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        if (dot < 0f)
            b = new Quaternion(-b.X, -b.Y, -b.Z, -b.W);
        Quaternion result = new(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t
        );
        return Quaternion.Normalize(result);
    }
}

public struct Color4(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
{
    public float Red = r;
    public float Green = g;
    public float Blue = b;
    public float Alpha = a;

    public readonly Color4 Lerp(Color4 goal, float alpha)
    {
        return new(
            ReconMath.LerpFloat(Red, goal.Red, alpha),
            ReconMath.LerpFloat(Green, goal.Green, alpha),
            ReconMath.LerpFloat(Blue, goal.Blue, alpha),
            ReconMath.LerpFloat(Alpha, goal.Alpha, alpha)
        );
    }
}

public struct Coords2
{
    public Vector2 Position;

    public Vector2 Up { get; private set; }
    public Vector2 Right { get; private set; }

    public static Coords2 Identity => new(Vector2.Zero, 0);

    public Coords2 Rotation
    {
        get
        {
            return new Coords2(Vector2.Zero, Right, Up);
        }
    }

    public Coords2(Vector2 position, float rotation)
    {
        Position = position;
        float cos = (float)Math.Cos(rotation);
        float sin = (float)Math.Sin(rotation);
        Right = new Vector2(cos, sin);
        Up = new Vector2(-sin, cos);
    }
    public Coords2(Vector2 position)
    {
        Position = position;
        Right = Vector2.UnitX;
        Up = Vector2.UnitY;
    }
    private Coords2(Vector2 position, Vector2 right, Vector2 up)
    {
        Position = position;
        Right = right;
        Up = up;
    }
    public static Coords2 LookAt(Vector2 eye, Vector2 target)
    {
        Vector2 diff = Vector2.Normalize(target - eye);
        float angle = (float)Math.Atan2(diff.Y, diff.X);
        return new Coords2(eye, angle);
    }

    public static Coords2 operator *(Coords2 a, Coords2 b)
    {
        Vector2 newPos = a.Position + (a.Right * b.Position.X) + (a.Up * b.Position.Y);
        Vector2 newRight = (a.Right * b.Right.X) + (a.Up * b.Right.Y);
        Vector2 newUp = (a.Right * b.Up.X) + (a.Up * b.Up.Y);
        return new Coords2(newPos, newRight, newUp);
    }
    public static Coords2 operator +(Coords2 a, Vector2 b)
    {
        Vector2 newPos = a.Position + (a.Right * b.X) + (a.Up * b.Y);
        return new Coords2(newPos, a.Right, a.Up);
    }
    public readonly Vector2 PointToWorldSpace(Vector2 localPoint)
    {
        return Position + (Right * localPoint.X) + (Up * localPoint.Y);
    }
    public readonly Coords2 Inverse()
    {
        Vector2 invRight = new(Right.X, Up.X);
        Vector2 invUp = new(Right.Y, Up.Y);
        float invPosX = -(invRight.X * Position.X + invRight.Y * Position.Y);
        float invPosY = -(invUp.X * Position.X + invUp.Y * Position.Y);
        return new Coords2(new Vector2(invPosX, invPosY), invRight, invUp);
    }
    public static Coords2 Lerp(Coords2 start, Coords2 end, float alpha)
    {
        Vector2 p = Vector2.Lerp(start.Position, end.Position, alpha);
        float startAngle = (float)Math.Atan2(start.Right.Y, start.Right.X);
        float endAngle = (float)Math.Atan2(end.Right.Y, end.Right.X);
        float shortestAngle = ((endAngle - startAngle + (float)Math.PI) % ((float)Math.PI * 2)) - (float)Math.PI;
        float lerpedAngle = startAngle + shortestAngle * alpha;
        return new Coords2(p, lerpedAngle);
    }
    public readonly float ToRotation()
    {
        return (float)Math.Atan2(Right.Y, Right.X);
    }
}

public struct OOBB2
{
    public Vector2 Center;
    public Vector2 Extents;
    public Vector2 AxisX;
    public Vector2 AxisY;

    public OOBB2(Vector2 center, Vector2 extents, float angle)
    {
        Center = center;
        Extents = extents;
        float CosAngle = MathF.Cos(angle);
        float SinAngle = MathF.Sin(angle);
        AxisX = new(CosAngle, SinAngle);
        AxisY = new(-SinAngle, CosAngle);
    }

    public readonly bool Contains(Vector2 point)
    {
        Vector2 d = point - Center;
        float projX = MathF.Abs(d.X * AxisX.X + d.Y * AxisX.Y);
        float projY = MathF.Abs(d.X * AxisY.X + d.Y * AxisY.Y);
        return projX <= Extents.X && projY <= Extents.Y;
    }
}

public static class TreePrinter
{
    public static void PrintTree(ReconEntity node, string indent = "", bool isLast = true)
    {
        var marker = isLast ? "└── " : "├── ";
        Console.WriteLine($"{indent}{marker}{node.Name} ({node.ClassName})");
        indent += isLast ? "    " : "│   ";
        var children = node.Children.ToList();
        for (int i = 0; i < children.Count; i++)
        {
            PrintTree(children[i], indent, i == children.Count - 1);
        }
    }
}
