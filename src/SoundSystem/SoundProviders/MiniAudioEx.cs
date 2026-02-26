using System.Numerics;
using MiniAudioEx.Core.StandardAPI;

namespace ReconEngine.SoundSystem.SoundProviders;

internal static class MiniAudioConverter
{
    public static Vector3 MiniAudioToNumeric(Vector3f v3f) => new(v3f.x, v3f.y, v3f.z);
    public static Vector3f NumericToMiniAudio(Vector3 v3) => new(v3.X, v3.Y, v3.Z);
    public static AttenuationModel FallOffModeToAttenuationModel (SoundFallOffMode fallOffMode)
    {
        return fallOffMode switch
        {
            SoundFallOffMode.Inverse => AttenuationModel.Inverse,
            SoundFallOffMode.Linear => AttenuationModel.Linear,
            SoundFallOffMode.Exponential => AttenuationModel.Exponential,
            _ => AttenuationModel.None,
        };
    }
}

public class MiniAudioSoundInstance(uint id) : ISoundInstance
{
    internal AudioSource _source = new();
    internal AudioClip? _clip;
    private uint _soundId = 0;
    private bool _paused = false;
    private float _durationCache = 0f;
    private ulong _pauseCursor = 0;

    public uint Id { get; private set; } = id;
    public uint SoundId
    {
        get => _soundId;
        set {
            if (_source.IsPlaying) Stop();
            if (ReconCore.SoundProvider.GetSoundData(value) is AudioClip clip) _clip = clip;
            else _clip = null;
            _soundId = value;
        }
    }

    public Vector3 Position
    {
        get => MiniAudioConverter.MiniAudioToNumeric(_source.Position);
        set => _source.Position = MiniAudioConverter.NumericToMiniAudio(value);
    }

    public bool IsPlaying => _source.IsPlaying;
    public bool IsPaused => _paused;
    public bool IsLoaded => _clip != null;

    public float Duration => _durationCache;
    public float TimePosition => _source.Cursor;

    public bool IsLooped { get => _source.Loop; set => _source.Loop = value; }

    public float Volume { get => _source.Volume; set => _source.Volume = value; }

    public float FallOffMinDistance { get => _source.MinDistance; set => _source.MinDistance = value; }
    public float FallOffMaxDistance { get => _source.MaxDistance; set => _source.MaxDistance = value; }
    public float FallOffValue { get => _source.RollOff; set => _source.RollOff = value; }
    private SoundFallOffMode _fallOffMode;
    public SoundFallOffMode FallOffMode
    {
        get => _fallOffMode;
        set {
            _fallOffMode = value;
            _source.AttenuationModel = MiniAudioConverter.FallOffModeToAttenuationModel(value);
        }
    }

    public void Play()
    {
        if (_clip == null) return;
        _paused = false;
        _source.Stop();
        _source.Play(_clip);
        _durationCache = _source.Length;
    }
    public void Stop()
    {
        if (_clip == null) return;
        _paused = false;
        _source.Stop();
    }

    public void Pause()
    {
        if (_clip == null) return;
        if (_paused) return;
        _paused = true;
        _pauseCursor = _source.Cursor;
        _source.Stop();
    }
    public void Resume()
    {
        if (_clip == null) return;
        if (!_paused) return;
        _paused = false;
        _source.Play(_clip);
        _source.Cursor = _pauseCursor;
    }
    public void OneShot() // ONESHOT REFERENCE!?!?
    {
        if (_clip == null) return;
        _source.PlayOneShot(_clip);
    }
}

public class MiniAudioSoundProvider : ISoundProvider
{
    private AudioListener _listener = null!;
    public void Initialize()
    {
        AudioContext.Initialize(44100, 4);
        _listener = new();
        _listener.Enabled = true;
    }
    public void Deinitialize() => AudioContext.Deinitialize();

    private readonly Dictionary<uint, AudioClip> _loadedSounds = [];
    private uint _soundCount;
    private readonly Dictionary<uint, MiniAudioSoundInstance> _loadedInstances = [];
    private uint _instanceCount;

    public uint LoadSound(string filePath)
    {
        AudioClip clip;
        try { clip = new(filePath); }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED TO LOAD SOUND: {ex.Message}");
            return 0;
        }
        _soundCount++;
        _loadedSounds[_soundCount] = clip;
        return _soundCount;
    }
    public object? GetSoundData(uint id) => _loadedSounds.GetValueOrDefault(id);

    public ISoundInstance Instantiate()
    {
        _instanceCount++;
        MiniAudioSoundInstance inst = new(_instanceCount);
        _loadedInstances[_instanceCount] = inst;
        return inst;
    }
    public void RemoveInstance(uint id) => _loadedInstances.Remove(id);

    public void Update(float deltaTime, Vector3 cameraPos, Vector3 cameraDir)
    {
        _listener.Position = MiniAudioConverter.NumericToMiniAudio(cameraPos);
        _listener.Direction = MiniAudioConverter.NumericToMiniAudio(cameraDir);
        AudioContext.Update();
    }

}
