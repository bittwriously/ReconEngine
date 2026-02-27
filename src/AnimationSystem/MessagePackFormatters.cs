using System.Numerics;
using MessagePack;
using MessagePack.Formatters;

namespace ReconEngine.AnimationSystem;

public class Vector3Formatter : IMessagePackFormatter<Vector3>
{
    public static readonly Vector3Formatter Instance = new();
    public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }
    public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        reader.ReadArrayHeader();
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}

public class QuaternionFormatter : IMessagePackFormatter<Quaternion>
{
    public static readonly QuaternionFormatter Instance = new();
    public void Serialize(ref MessagePackWriter writer, Quaternion value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }
    public Quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        reader.ReadArrayHeader();
        return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}

public class ReconAnimationResolver : IFormatterResolver
{
    public static readonly ReconAnimationResolver Instance = new();
    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        if (typeof(T) == typeof(Vector3)) return (IMessagePackFormatter<T>)Vector3Formatter.Instance;
        if (typeof(T) == typeof(Quaternion)) return (IMessagePackFormatter<T>)QuaternionFormatter.Instance;
        return null;
    }
}
