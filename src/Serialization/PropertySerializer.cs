using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReconEngine.WorldSystem;

namespace ReconEngine.Serialization;

public static class EntityPropertySerializer
{
    private static readonly HashSet<Type> _supportedTypes =
    [
        typeof(bool),
        typeof(int), typeof(uint), typeof(float), typeof(double),
        typeof(string),
        typeof(Vector2), typeof(Vector3), typeof(Quaternion), typeof(Matrix4x4),
        typeof(Color4)
    ];

    public static byte[] Serialize(ReconEntity entity, PrefabWriteContext ctx)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var field in GetSerializableFields(entity.GetType()))
        {
            var value = field.GetValue(entity);

            if (value is ReconEntity refEntity)
            {
                dict[field.Name] = new EntityRef { LocalId = ctx.GetLocalId(refEntity) };
                continue;
            }

            dict[field.Name] = value;
        }

        foreach (var prop in GetSerializableProps(entity.GetType()))
        {
            var value = prop.GetValue(entity);

            if (value is ReconEntity refEntity)
            {
                dict[prop.Name] = new EntityRef { LocalId = ctx.GetLocalId(refEntity) };
                continue;
            }

            dict[prop.Name] = value;
        }

        return JsonSerializer.SerializeToUtf8Bytes(dict, _jsonOptions);
    }

    public static void Deserialize(ReconEntity entity, byte[] data, PrefabReadContext ctx)
    {
        if (data.Length == 0) return;

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data, _jsonOptions);
        if (dict == null) return;

        foreach (var field in GetSerializableFields(entity.GetType()))
        {
            if (!dict.TryGetValue(field.Name, out var element)) continue;

            if (field.FieldType.IsSubclassOf(typeof(ReconEntity)) || field.FieldType == typeof(ReconEntity))
            {
                var refObj = element.Deserialize<EntityRef>(_jsonOptions);
                if (refObj != null)
                    ctx.ResolveRef<ReconEntity>(refObj.LocalId, e => field.SetValue(entity, e));
                continue;
            }

            var value = element.Deserialize(field.FieldType, _jsonOptions);
            if (value != null) field.SetValue(entity, value);
        }

        foreach (var prop in GetSerializableProps(entity.GetType()))
        {
            if (!prop.CanWrite) continue;
            if (!dict.TryGetValue(prop.Name, out var element)) continue;

            if (prop.PropertyType.IsSubclassOf(typeof(ReconEntity)) || prop.PropertyType == typeof(ReconEntity))
            {
                var refObj = element.Deserialize<EntityRef>(_jsonOptions);
                if (refObj != null)
                    ctx.ResolveRef<ReconEntity>(refObj.LocalId, e => prop.SetValue(entity, e));
                continue;
            }

            var value = element.Deserialize(prop.PropertyType, _jsonOptions);
            if (value != null) prop.SetValue(entity, value);
        }
    }

    private static IEnumerable<FieldInfo> GetSerializableFields(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !f.IsDefined(typeof(PrefabIgnoreAttribute))
                     && IsSupportedOrEntityRef(f.FieldType));

    private static IEnumerable<PropertyInfo> GetSerializableProps(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                     && p.CanWrite
                     && !p.IsDefined(typeof(PrefabIgnoreAttribute))
                     && IsSupportedOrEntityRef(p.PropertyType));

    private static bool IsSupportedOrEntityRef(Type t) =>
        _supportedTypes.Contains(t)
        || t.IsEnum
        || t.IsSubclassOf(typeof(ReconEntity))
        || t == typeof(ReconEntity);

    private static readonly JsonSerializerOptions _jsonOptions = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new Vector2Converter());
        opts.Converters.Add(new Vector3Converter());
        opts.Converters.Add(new QuaternionConverter());
        opts.Converters.Add(new Color4Converter());
        return opts;
    }

    private class EntityRef
    {
        public uint LocalId { get; set; }
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PrefabIgnoreAttribute : Attribute { }

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        float x = 0, y = 0, z = 0;
        while (r.Read() && r.TokenType != JsonTokenType.EndObject)
        {
            string key = r.GetString()!; r.Read();
            if (key == "x") x = r.GetSingle();
            else if (key == "y") y = r.GetSingle();
            else if (key == "z") z = r.GetSingle();
        }
        return new Vector3(x, y, z);
    }
    public override void Write(Utf8JsonWriter w, Vector3 v, JsonSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteNumber("x", v.X);
        w.WriteNumber("y", v.Y);
        w.WriteNumber("z", v.Z);
        w.WriteEndObject();
    }
}

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        float x = 0, y = 0;
        while (r.Read() && r.TokenType != JsonTokenType.EndObject)
        {
            string key = r.GetString()!; r.Read();
            if (key == "x") x = r.GetSingle();
            else if (key == "y") y = r.GetSingle();
        }
        return new Vector2(x, y);
    }
    public override void Write(Utf8JsonWriter w, Vector2 v, JsonSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteNumber("x", v.X);
        w.WriteNumber("y", v.Y);
        w.WriteEndObject();
    }
}

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        float x = 0, y = 0, z = 0, w = 0;
        while (r.Read() && r.TokenType != JsonTokenType.EndObject)
        {
            string key = r.GetString()!; r.Read();
            if (key == "x") x = r.GetSingle();
            else if (key == "y") y = r.GetSingle();
            else if (key == "z") z = r.GetSingle();
            else if (key == "w") w = r.GetSingle();
        }
        return new Quaternion(x, y, z, w);
    }
    public override void Write(Utf8JsonWriter w, Quaternion v, JsonSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteNumber("x", v.X);
        w.WriteNumber("y", v.Y);
        w.WriteNumber("z", v.Z);
        w.WriteNumber("w", v.W);
        w.WriteEndObject();
    }
}

public class Color4Converter : JsonConverter<Color4>
{
    public override Color4 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        float x = 0, y = 0, z = 0, w = 0;
        while (r.Read() && r.TokenType != JsonTokenType.EndObject)
        {
            string key = r.GetString()!; r.Read();
            if (key == "r") x = r.GetSingle();
            else if (key == "g") y = r.GetSingle();
            else if (key == "b") z = r.GetSingle();
            else if (key == "a") w = r.GetSingle();
        }
        return new Color4(x, y, z, w);
    }
    public override void Write(Utf8JsonWriter w, Color4 v, JsonSerializerOptions o)
    {
        w.WriteStartObject();
        w.WriteNumber("r", v.Red);
        w.WriteNumber("g", v.Green);
        w.WriteNumber("b", v.Blue);
        w.WriteNumber("a", v.Alpha);
        w.WriteEndObject();
    }
}
