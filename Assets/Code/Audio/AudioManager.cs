using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AudioConstants
{
    public const float DEFAULT_FADE_TIME = 1f;
    public const float MIN_VOLUME = 0f;
    public const float MAX_VOLUME = 1f;
    public const float MIN_PITCH = -3f;
    public const float MAX_PITCH = 3f;
    public const int MAX_AUDIO_SOURCES = 20;
}

public enum AudioType
{
    Music,
    SFX,
    UI,
    Ambient,
    Voice
}

public enum AudioPriority
{
    Low = 0,
    Medium = 128,
    High = 256
}

[System.Serializable]
public class Sound
{
    [SerializeField] private string _name;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AudioType _type;
    [SerializeField][Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] private bool _loop = false;
    [SerializeField][Range(-3f, 3f)] private float _pitch = 1f;
    [SerializeField][Range(0f, 1f)] private float _spatialBlend = 0f;
    [SerializeField] private AudioPriority _priority = AudioPriority.Medium;

    private AudioSource _source;
    private float _originalVolume;
    private bool _isFading = false;

    // Properties
    public string Name { get => _name; set => _name = value; }
    public AudioClip Clip { get => _clip; set => _clip = value; }
    public AudioType Type { get => _type; set => _type = value; }
    public float Volume
    {
        get => _volume;
        set => _volume = Mathf.Clamp(value, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
    }
    public bool Loop { get => _loop; set => _loop = value; }
    public float Pitch
    {
        get => _pitch;
        set => _pitch = Mathf.Clamp(value, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
    public float SpatialBlend
    {
        get => _spatialBlend;
        set => _spatialBlend = Mathf.Clamp01(value);
    }
    public AudioPriority Priority { get => _priority; set => _priority = value; }
    public AudioSource Source { get => _source; set => _source = value; }
    public bool IsFading { get => _isFading; set => _isFading = value; }
    public float OriginalVolume { get => _originalVolume; set => _originalVolume = value; }
}

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private Sound[] _sounds;
    [SerializeField][Range(0f, 1f)] private float _masterVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float _musicVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float _sfxVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float _uiVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float _ambientVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float _voiceVolume = 1f;

    private Dictionary<string, Sound> _soundDictionary;
    private List<AudioSource> _audioSourcePool;
    private Sound _currentMusic;
    private const string VOLUME_SAVE_KEY = "AudioVolumes";

    #region Unity Methods

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
            LoadVolumes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveVolumes();
    }

    #endregion

    #region Initialization

    private void InitializeAudio()
    {
        _soundDictionary = new Dictionary<string, Sound>();
        _audioSourcePool = new List<AudioSource>();

        // Initialize audio source pool
        for (int i = 0; i < AudioConstants.MAX_AUDIO_SOURCES; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _audioSourcePool.Add(source);
        }

        // Initialize sounds
        foreach (Sound s in _sounds)
        {
            if (!_soundDictionary.ContainsKey(s.Name))
            {
                s.OriginalVolume = s.Volume;
                _soundDictionary.Add(s.Name, s);
            }
            else
            {
                Debug.LogWarning($"Duplicate sound name found: {s.Name}");
            }
        }
    }

    #endregion

    #region Public Methods

    public void Play(string name, bool fadeIn = false, float fadeTime = AudioConstants.DEFAULT_FADE_TIME)
    {
        if (!_soundDictionary.TryGetValue(name, out Sound sound))
        {
            Debug.LogWarning($"Sound {name} not found!");
            return;
        }

        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource == null)
        {
            Debug.LogWarning("No available audio sources!");
            return;
        }

        ConfigureAudioSource(sound, availableSource);

        if (sound.Type == AudioType.Music)
        {
            HandleMusicTransition(sound, fadeIn, fadeTime);
        }
        else
        {
            if (fadeIn)
                StartCoroutine(FadeIn(availableSource, fadeTime));
            else
                availableSource.Play();
        }
    }

    public void Stop(string name, bool fadeOut = false, float fadeTime = AudioConstants.DEFAULT_FADE_TIME)
    {
        if (!_soundDictionary.TryGetValue(name, out Sound sound))
            return;

        if (fadeOut && sound.Source != null)
            StartCoroutine(FadeOut(sound.Source, fadeTime));
        else if (sound.Source != null)
            sound.Source.Stop();
    }

    public void SetVolume(AudioType type, float volume)
    {
        volume = Mathf.Clamp01(volume);
        switch (type)
        {
            case AudioType.Music:
                _musicVolume = volume;
                break;
            case AudioType.SFX:
                _sfxVolume = volume;
                break;
            case AudioType.UI:
                _uiVolume = volume;
                break;
            case AudioType.Ambient:
                _ambientVolume = volume;
                break;
            case AudioType.Voice:
                _voiceVolume = volume;
                break;
        }
        UpdateAllVolumes();
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void PauseAll()
    {
        foreach (var source in _audioSourcePool)
        {
            if (source.isPlaying)
                source.Pause();
        }
    }

    public void ResumeAll()
    {
        foreach (var source in _audioSourcePool)
        {
            if (!source.isPlaying)
                source.UnPause();
        }
    }

    public void StopAll(bool fadeOut = false)
    {
        foreach (var source in _audioSourcePool)
        {
            if (fadeOut)
                StartCoroutine(FadeOut(source));
            else
                source.Stop();
        }
    }

    public void PlayWithParameters(string name, bool fadeIn, float fadeTime, float volumeMultiplier, float pitchMultiplier)
    {
        if (!_soundDictionary.TryGetValue(name, out Sound sound))
        {
            Debug.LogWarning($"Sound {name} not found!");
            return;
        }

        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource == null)
        {
            Debug.LogWarning("No available audio sources!");
            return;
        }

        // Lưu lại pitch và volume gốc
        float originalPitch = sound.Pitch;
        float originalVolume = sound.Volume;

        // Áp dụng multipliers
        sound.Pitch *= pitchMultiplier;
        sound.Volume *= volumeMultiplier;

        ConfigureAudioSource(sound, availableSource);

        if (sound.Type == AudioType.Music)
        {
            HandleMusicTransition(sound, fadeIn, fadeTime);
        }
        else
        {
            if (fadeIn)
                StartCoroutine(FadeIn(availableSource, fadeTime));
            else
                availableSource.Play();
        }

        // Khôi phục pitch và volume gốc
        sound.Pitch = originalPitch;
        sound.Volume = originalVolume;
    }

    #endregion

    #region Private Methods

    private AudioSource GetAvailableAudioSource()
    {
        return _audioSourcePool.FirstOrDefault(source => !source.isPlaying);
    }

    private void ConfigureAudioSource(Sound sound, AudioSource source)
    {
        sound.Source = source;
        source.clip = sound.Clip;
        source.volume = CalculateVolume(sound);
        source.loop = sound.Loop;
        source.pitch = sound.Pitch;
        source.spatialBlend = sound.SpatialBlend;
        source.priority = (int)sound.Priority;
    }

    private float CalculateVolume(Sound sound)
    {
        float typeVolume = GetVolumeForType(sound.Type);
        return sound.Volume * typeVolume * _masterVolume;
    }

    private float GetVolumeForType(AudioType type)
    {
        switch (type)
        {
            case AudioType.Music: return _musicVolume;
            case AudioType.SFX: return _sfxVolume;
            case AudioType.UI: return _uiVolume;
            case AudioType.Ambient: return _ambientVolume;
            case AudioType.Voice: return _voiceVolume;
            default: return 1f;
        }
    }

    private void HandleMusicTransition(Sound newMusic, bool fadeIn, float fadeTime)
    {
        if (_currentMusic != null && _currentMusic != newMusic)
        {
            StartCoroutine(FadeOut(_currentMusic.Source, fadeTime));
        }
        _currentMusic = newMusic;

        if (fadeIn)
            StartCoroutine(FadeIn(newMusic.Source, fadeTime));
        else
            newMusic.Source.Play();
    }

    private void UpdateAllVolumes()
    {
        foreach (var sound in _soundDictionary.Values)
        {
            if (sound.Source != null)
            {
                sound.Source.volume = CalculateVolume(sound);
            }
        }
    }

    #endregion

    #region Coroutines

    private IEnumerator FadeIn(AudioSource audioSource, float duration = AudioConstants.DEFAULT_FADE_TIME)
    {
        float startVolume = 0f;
        float targetVolume = audioSource.volume;

        audioSource.volume = startVolume;
        audioSource.Play();

        while (audioSource.volume < targetVolume)
        {
            audioSource.volume += targetVolume * Time.deltaTime / duration;
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOut(AudioSource audioSource, float duration = AudioConstants.DEFAULT_FADE_TIME)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    #endregion

    #region Save/Load System

    private void SaveVolumes()
    {
        var volumeData = new Dictionary<string, float>
        {
            {"master", _masterVolume},
            {"music", _musicVolume},
            {"sfx", _sfxVolume},
            {"ui", _uiVolume},
            {"ambient", _ambientVolume},
            {"voice", _voiceVolume}
        };

        string json = JsonUtility.ToJson(volumeData);
        PlayerPrefs.SetString(VOLUME_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadVolumes()
    {
        if (PlayerPrefs.HasKey(VOLUME_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(VOLUME_SAVE_KEY);
            var volumeData = JsonUtility.FromJson<Dictionary<string, float>>(json);

            _masterVolume = volumeData["master"];
            _musicVolume = volumeData["music"];
            _sfxVolume = volumeData["sfx"];
            _uiVolume = volumeData["ui"];
            _ambientVolume = volumeData["ambient"];
            _voiceVolume = volumeData["voice"];

            UpdateAllVolumes();
        }
    }

    #endregion
}