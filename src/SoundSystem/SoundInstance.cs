using System.Numerics;
using ReconEngine.System3D;

namespace ReconEngine.SoundSystem;

public class ReconSound3D : ReconEntity3D
{
    private string _soundPath = "";
    private uint _soundId;

    private readonly ISoundInstance _soundData = ReconCore.SoundProvider.Instantiate();

    public string Sound
    {
        get => _soundPath;
        set
        {
            _soundPath = value;
            _soundId = DynamicResouceLoader.LoadAsset(value, ResourceAssetType.Sound);
            if (_soundId != 0) _soundData.SoundId = _soundId;
        }
    }

    public new Vector3 Position
    {
        get => _soundData.Position;
        set => _soundData.Position = value;
    }

    public bool IsPlaying => _soundData.IsPlaying;
    public bool IsPaused => _soundData.IsPaused;
    public bool IsLoaded => _soundData.IsLoaded;
    public float Duration => _soundData.Duration;
    public float TimePosition => _soundData.TimePosition;

    public bool IsLooped
    {
        get => _soundData.IsLooped;
        set { _soundData.IsLooped = value; }
    }
    public float Volume
    {
        get => _soundData.Volume;
        set { _soundData.Volume = value; }
    }
    public float FallOffMinDistance
    {
        get => _soundData.FallOffMinDistance;
        set { _soundData.FallOffMinDistance = value; }
    }
    public float FallOffMaxDistance
    {
        get => _soundData.FallOffMaxDistance;
        set { _soundData.FallOffMaxDistance = value; }
    }
    public float FallOffValue
    {
        get => _soundData.FallOffValue;
        set { _soundData.FallOffValue = value; }
    }
    public SoundFallOffMode FallOffMode
    {
        get => _soundData.FallOffMode;
        set { _soundData.FallOffMode = value; }
    }

    public void Play() => _soundData?.Play();
    public void Stop() => _soundData?.Stop();
    public void Pause() => _soundData?.Pause();
    public void Resume() => _soundData?.Resume();
    public void PlayOneShot() => _soundData?.OneShot();
}
