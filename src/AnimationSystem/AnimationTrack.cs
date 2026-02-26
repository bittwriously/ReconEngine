using System.Numerics;

namespace ReconEngine.AnimationSystem;

public class AnimationTrack
{
    public readonly AnimationClip Clip;
    public bool IsPlaying { get; private set; } = false;
    public float TimePosition { get; private set; } = 0f;
    public float Speed = 1f;

    public int Priority = 0;
    public float Weight { get; private set; } = 0f;
    public event Action? Ended;

    private float _weightTarget = 0f;
    private float _fadeSpeed = float.MaxValue;

    public AnimationTrack(AnimationClip clip)
    {
        Clip = clip;
    }

    public void Play(float fadeInTime = 0f)
    {
        IsPlaying = true;
        TimePosition = 0f;
        _weightTarget = 1f;
        if (fadeInTime > 0f)
        {
            Weight = 0f;
            _fadeSpeed = 1f / fadeInTime;
        }
        else
        {
            Weight = 1f;
            _fadeSpeed = float.MaxValue;
        }
    }
    public void Stop(float fadeOutTime = 0f)
    {
        _weightTarget = 0f;
        if (fadeOutTime > 0f)
            _fadeSpeed = 1f / fadeOutTime;
        else
        {
            Weight = 0f;
            IsPlaying = false;
            Ended?.Invoke();
        }
    }

    public Dictionary<string, (Vector3 pos, Quaternion rot)>? Advance(float dt)
    {
        if (!IsPlaying) return null;
        Weight = MoveToward(Weight, _weightTarget, _fadeSpeed * dt);
        if (_weightTarget == 0f && Weight <= 0.001f)
        {
            Weight = 0f;
            IsPlaying = false;
            Ended?.Invoke();
            return null;
        }
        TimePosition += dt * Speed;
        if (TimePosition >= Clip.Length)
        {
            if (Clip.Looped) TimePosition %= Clip.Length;
            else
            {
                TimePosition = Clip.Length;
                Stop();
                Ended?.Invoke();
            }
        }

        return SamplePose();
    }

    private Dictionary<string, (Vector3 pos, Quaternion rot)> SamplePose()
    {
        var (prev, next, t) = Clip.SampleAt(TimePosition);
        var result = new Dictionary<string, (Vector3, Quaternion)>();
        if (prev == null) return result;
        foreach (var (name, kfA) in prev.Motors)
        {
            if (next != null && next.Motors.TryGetValue(name, out var kfB))
            {
                result[name] = (
                    Vector3.Lerp(kfA.Position, kfB.Position, t),
                    Quaternion.Slerp(kfA.Rotation, kfB.Rotation, t)
                );
            }
            else result[name] = (kfA.Position, kfA.Rotation);
        }

        return result;
    }

    private static float MoveToward(float current, float target, float step)
    {
        float diff = target - current;
        if (MathF.Abs(diff) <= step) return target;
        return current + MathF.Sign(diff) * step;
    }
}
