using System.Numerics;

namespace ReconEngine.SoundSystem.SoundProviders;

public class NullSoundInstance(uint id) : ISoundInstance
{
    public uint Id { get; private set; } = id;
    public uint SoundId { get; set; } = 0;

    public Vector3 Position { get; set; } = Vector3.Zero;

    public bool IsPlaying => false;
    public bool IsPaused => false;
    public bool IsLoaded => true;

    public float Duration => 0;
    public float TimePosition => 0;

    public bool IsLooped { get; set; } = false;
    public float Volume { get; set; } = 0;

    public float FallOffMinDistance { get; set; } = 0;
    public float FallOffMaxDistance { get; set; } = 0;
    public float FallOffValue { get; set; } = 0;
    public SoundFallOffMode FallOffMode { get; set; }

    public void Play() { }
    public void Stop() { }

    public void Pause() { }
    public void Resume() { }
    public void OneShot() { }
}

public class NullSoundProvider : ISoundProvider
{
    public void Initialize() { }
    public void Deinitialize() { }

    public uint LoadSound(string filePath) => 0;
    public object? GetSoundData(uint id) => null;

    public ISoundInstance Instantiate() => new NullSoundInstance(0);
    public void RemoveInstance(uint id) { }

    public void Update(float deltaTime, Vector3 cameraPos, Vector3 cameraDir) { }
}
