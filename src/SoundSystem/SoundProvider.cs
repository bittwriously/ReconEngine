using System.Numerics;

namespace ReconEngine.SoundSystem;

public enum SoundFallOffMode
{
    Inverse = 0,
    Linear = 1,
    Exponential = 2,
    None = 3,
}

public interface ISoundInstance
{
    public uint Id { get; }
    public uint SoundId { get; set; }

    public Vector3 Position { get; set; }

    public bool IsPlaying { get; }
    public bool IsPaused { get; }
    public bool IsLoaded { get; }

    public float Duration { get; }
    public float TimePosition { get; }

    public bool IsLooped { get; set; }
    public float Volume { get; set; }

    public float FallOffMaxDistance { get; set; }
    public float FallOffValue { get; set; }
    public float FallOffMinDistance { get; set; }
    public SoundFallOffMode FallOffMode { get; set; }

    public void Play();
    public void Stop();
    public void Pause();
    public void Resume();
    public void OneShot();
}

public interface ISoundProvider
{
    public void Initialize();
    public void Deinitialize();

    public uint LoadSound(string filePath);
    public object? GetSoundData(uint id);

    public ISoundInstance Instantiate();
    public void RemoveInstance(uint instanceId);

    public void Update(float deltaTime, Vector3 cameraPos, Vector3 cameraDir);
}
