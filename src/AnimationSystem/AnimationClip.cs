using System.Numerics;

namespace ReconEngine.AnimationSystem;

// transform for a single motor
public struct MotorKeyframe
{
    public string MotorName;
    public Vector3 Position;
    public Quaternion Rotation;
}

public enum EasingStyle
{
    Constant,
    Linear,
    Cubic,
    Bounce,
    Elastic,
}

// a snapshot of all motors in a point of time
public class Keyframe
{
    public float Time;
    public EasingStyle Easing = EasingStyle.Linear;
    public Dictionary<string, MotorKeyframe> Motors = [];
}

public class AnimationClip
{
    public string Name = "";
    public float Length;
    public bool Looped;
    public List<Keyframe> Keyframes = [];

    public (Keyframe? prev, Keyframe? next, float t) SampleAt(float time)
    {
        if (Keyframes.Count == 0) return (null, null, 0f);
        if (Keyframes.Count == 1) return (Keyframes[0], Keyframes[0], 0f);

        for (int i = 0; i < Keyframes.Count - 1; i++)
        {
            var a = Keyframes[i];
            var b = Keyframes[i + 1];
            if (time >= a.Time && time <= b.Time)
            {
                float t = (time - a.Time) / (b.Time - a.Time);
                return (a, b, ApplyEasing(t, b.Easing));
            }
        }

        return (Keyframes[^1], Keyframes[^1], 0f);
    }

    private static float ApplyEasing(float t, EasingStyle style) => style switch
    {
        EasingStyle.Cubic => t * t * (3f - 2f * t),
        EasingStyle.Bounce => BounceEase(t),
        EasingStyle.Elastic => ElasticEase(t),
        _ => t
    };

    private const float _Inv2_75 = 1f / 2.75f;       // ≈ 0.36364
    private const float _Inv2_75a = 1.5f / 2.75f;    // ≈ 0.54545
    private const float _Inv2_75b = 2.25f / 2.75f;   // ≈ 0.81818
    private const float _Inv2_75c = 2.625f / 2.75f;  // ≈ 0.95455

    private static float BounceEase(float t)
    {
        t = 1f - t;
        if (t < _Inv2_75) return 1f - 7.5625f * t * t;
        if (t < _Inv2_75a) { t -= _Inv2_75a; return 1f - (7.5625f * t * t + 0.75f); }
        if (t < _Inv2_75b) { t -= _Inv2_75b; return 1f - (7.5625f * t * t + 0.9375f); }
        t -= _Inv2_75c;
        return 1f - (7.5625f * t * t + 0.984375f);
    }

    private static float ElasticEase(float t) =>
        t == 0f || t == 1f ? t :
        -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * (2f * MathF.PI / 3f));
}
