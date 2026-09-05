using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour
    {
        private const string Prefix = "audio.";
        private const int SettingsVersion = 9;
        public const float MaxMasterVolume = .7f;
        public const float DefaultMusicVolume = 1f;
        public const float DefaultAmbienceVolume = .7f;
        public const float AmbienceHeadroomDb = 0f;
        public const float MenuMusicGain = 1f;
        public const float RaceMusicGain = .35f;
        public const float ResultMusicGain = .65f;
        public const float RaceAmbienceGain = .45f;
        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup, sfxGroup, ambienceGroup, uiGroup, raceGroup, engineGroup, tiresGroup, collisionGroup;
        [SerializeField] private AudioMixerSnapshot defaultSnapshot, countdownSnapshot, pausedSnapshot, finishSnapshot;
        [SerializeField] private AudioSource sfxSource, raceSource, uiSource, musicSource, ambiencePrimary, ambienceSecondary;
        private Coroutine ambienceTransition;
        private AudioLowPassFilter lowPassFilter;
        private readonly Stack<AudioSnapshotId> snapshotStack = new();
        private readonly Dictionary<AudioCueId, int> cuePlayCounts = new();
        private float ambiencePrimaryBaseVolume = .35f;
        private float ambienceSecondaryBaseVolume = .15f;
        private float currentMusicGain = MenuMusicGain;
        public static GameAudioManager Instance { get; private set; }
        public AudioCatalog Catalog => catalog;
        public string LastPlayedClipName { get; private set; } = "Nothing played yet";
        public bool IsMuted { get; private set; }
        public AudioSnapshotId CurrentSnapshot { get; private set; } = AudioSnapshotId.Default;
        public bool HasMixer => mixer != null;
        public string CurrentMusicClipName => musicSource != null && musicSource.clip != null ? musicSource.clip.name : "None";
        public string CurrentAmbienceClipName => ambiencePrimary != null && ambiencePrimary.clip != null ? ambiencePrimary.clip.name : "None";
        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
        public bool IsAmbiencePlaying => ambiencePrimary != null && ambiencePrimary.isPlaying;
        public AudioMixerGroup GetMixerGroup(string groupName) => mixer != null ? FindGroup(groupName) : null;
        public int GetCuePlayCount(AudioCueId cue) => cuePlayCounts.TryGetValue(cue, out int count) ? count : 0;
        public void ResetCueHistory() { cuePlayCounts.Clear(); LastPlayedClipName = "Nothing played yet"; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); EnsureSources();
            if (PlayerPrefs.GetInt(Prefix + "settingsVersion", 0) < SettingsVersion)
            {
                PlayerPrefs.SetFloat(Prefix + "master", MaxMasterVolume);
                PlayerPrefs.SetFloat(Prefix + "music", DefaultMusicVolume);
                PlayerPrefs.SetFloat(Prefix + "sfx", 1f);
                PlayerPrefs.SetFloat(Prefix + "ambience", DefaultAmbienceVolume);
                PlayerPrefs.SetFloat(Prefix + "ui", 1f);
                PlayerPrefs.SetInt(Prefix + "muted", 0);
                PlayerPrefs.SetInt(Prefix + "settingsVersion", SettingsVersion);
                PlayerPrefs.Save();
            }
            IsMuted = PlayerPrefs.GetInt(Prefix + "muted", 0) != 0;
            SetBusVolume(AudioBus.Master, PlayerPrefs.GetFloat(Prefix + "master", MaxMasterVolume));
            SetBusVolume(AudioBus.Music, PlayerPrefs.GetFloat(Prefix + "music", DefaultMusicVolume));
            SetBusVolume(AudioBus.Sfx, PlayerPrefs.GetFloat(Prefix + "sfx", 1f));
            SetBusVolume(AudioBus.Ambience, PlayerPrefs.GetFloat(Prefix + "ambience", DefaultAmbienceVolume));
            SetBusVolume(AudioBus.UI, PlayerPrefs.GetFloat(Prefix + "ui", 1f)); ApplyMuteState();
        }

        public void Configure(AudioCatalog value, AudioMixer audioMixer = null)
        {
            catalog = value;
            mixer = audioMixer != null ? audioMixer : value != null ? value.mixer : null;
            ResolveMixerAssets(); EnsureSources(); RouteSources();
            if (mixer != null)
            {
                if (sfxSource != null) sfxSource.volume = 1f;
                if (raceSource != null) raceSource.volume = 1f;
                if (uiSource != null) uiSource.volume = 1f;
                if (musicSource != null) musicSource.volume = 1f;
                if (ambiencePrimary != null) ambiencePrimary.volume = ambiencePrimaryBaseVolume;
                if (ambienceSecondary != null) ambienceSecondary.volume = ambienceSecondaryBaseVolume;
                AudioListener.volume = IsMuted ? 0f : 1f;
                SetBusVolume(AudioBus.Master, GetBusVolume(AudioBus.Master));
                SetBusVolume(AudioBus.Music, GetBusVolume(AudioBus.Music));
                SetBusVolume(AudioBus.Sfx, GetBusVolume(AudioBus.Sfx));
                SetBusVolume(AudioBus.Ambience, GetBusVolume(AudioBus.Ambience));
                SetBusVolume(AudioBus.UI, GetBusVolume(AudioBus.UI));
            }
        }
        public static float NormalizedToDb(float value) => value <= .0001f ? -80f : Mathf.Log10(Mathf.Clamp01(value)) * 20f;
        public float GetBusVolume(AudioBus bus) => PlayerPrefs.GetFloat(Prefix + BusKey(bus), DefaultVolume(bus));
        public void SetBusVolume(AudioBus bus, float value)
        {
            value = bus == AudioBus.Master ? Mathf.Clamp(value, 0f, MaxMasterVolume) : Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Prefix + BusKey(bus), value);
            string parameter = bus switch { AudioBus.Master => "MasterVolume", AudioBus.Music => "MusicVolume", AudioBus.Sfx => "SfxVolume", AudioBus.Ambience => "AmbienceVolume", _ => "UiVolume" };
            float db = NormalizedToDb(value);
            if (bus == AudioBus.Ambience && value > .0001f) db += AmbienceHeadroomDb;
            if (mixer == null || !mixer.SetFloat(parameter, db)) ApplyFallbackVolumes();
        }
        public void SetMuted(bool muted) { IsMuted = muted; PlayerPrefs.SetInt(Prefix + "muted", muted ? 1 : 0); ApplyMuteState(); }
        public void ResetAudioSettings()
        {
            SetMuted(false); SetBusVolume(AudioBus.Master, MaxMasterVolume); SetBusVolume(AudioBus.Music, DefaultMusicVolume);
            SetBusVolume(AudioBus.Sfx, 1f); SetBusVolume(AudioBus.Ambience, DefaultAmbienceVolume); SetBusVolume(AudioBus.UI, 1f); PlayerPrefs.Save();
        }
        public void ApplySnapshot(AudioSnapshotId id, float transitionSeconds = .25f)
        {
            CurrentSnapshot = id;
            ApplyLowPass(id == AudioSnapshotId.Paused ? 5500f : 22000f);
            AudioMixerSnapshot snapshot = id switch { AudioSnapshotId.Countdown => countdownSnapshot, AudioSnapshotId.Paused => pausedSnapshot, AudioSnapshotId.Finish => finishSnapshot, _ => defaultSnapshot };
            if (snapshot != null) snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
            else StartCoroutine(ApplyRuntimeSnapshot(id, transitionSeconds));
        }
        public void ResetSnapshotState(AudioSnapshotId id = AudioSnapshotId.Default, float transitionSeconds = 0f)
        {
            snapshotStack.Clear();
            ApplySnapshot(id, transitionSeconds);
        }
        public void PushSnapshot(AudioSnapshotId id, float transitionSeconds = .25f) { snapshotStack.Push(CurrentSnapshot); ApplySnapshot(id, transitionSeconds); }
        public void PopSnapshot(float transitionSeconds = .25f) => ApplySnapshot(snapshotStack.Count > 0 ? snapshotStack.Pop() : AudioSnapshotId.Default, transitionSeconds);
        public void FlushSettings() { PlayerPrefs.Save(); }
        public void PlayCue(AudioCueId cue, Vector3? position = null, float volume = 1f)
        {
            AudioClip clip = catalog != null ? catalog.GetCue(cue) : null;
            if (clip == null) { Debug.LogWarning($"[Audio] Cue {cue} has no clip.", this); return; }
            cuePlayCounts[cue] = GetCuePlayCount(cue) + 1;
            if (position.HasValue) PlayAtPoint(clip, position.Value, volume);
            else if (cue >= AudioCueId.UIHover) PlayOneShot(clip, volume, true);
            else { raceSource.PlayOneShot(clip, volume); LastPlayedClipName = clip.name; }
            Debug.Log($"[Audio] Cue {cue} #{cuePlayCounts[cue]}: {clip.name}", this);
        }
        public void PlayMusic(MusicId id, float fadeSeconds = .35f)
        {
            if (catalog == null) return;
            AudioClip clip = id switch { MusicId.Menu => catalog.menuMusic, MusicId.Result => catalog.resultMusic, _ => catalog.raceMusic };
            float gain = id switch { MusicId.Menu => MenuMusicGain, MusicId.Result => ResultMusicGain, _ => RaceMusicGain };
            StartCoroutine(CrossfadeMusic(clip, gain, fadeSeconds));
        }
        public void PlayMenuMusic() => PlayMusic(MusicId.Menu);
        public void PlayRaceMusic() => PlayMusic(MusicId.Race);
        public void PlayResultMusic() => PlayMusic(MusicId.Result);
        public void StopMusic() { if (musicSource != null) musicSource.Stop(); }
        public void StopAmbience()
        {
            if (ambienceTransition != null)
            {
                StopCoroutine(ambienceTransition);
                ambienceTransition = null;
            }
            if (ambiencePrimary != null) ambiencePrimary.Stop();
            if (ambienceSecondary != null) ambienceSecondary.Stop();
        }
        public void PlayOneShot(AudioClip clip, float volume = 1f, bool ui = false)
        { if (clip == null) return; EnsureSources(); (ui ? uiSource : sfxSource).PlayOneShot(clip, volume); LastPlayedClipName = clip.name; }
        public AudioClip PlayRandomCue(AudioClip[] variants, AudioClip fallback, AudioSource source, float volume = 1f, float pitchRange = .05f)
        {
            AudioClip clip = variants != null && variants.Length > 0 ? variants[Random.Range(0, variants.Length)] : fallback;
            if (clip == null || source == null) return null;
            source.pitch = Random.Range(1f - Mathf.Abs(pitchRange), 1f + Mathf.Abs(pitchRange));
            source.PlayOneShot(clip, Mathf.Clamp01(volume)); LastPlayedClipName = clip.name; return clip;
        }
        public void PlayBusPreview(AudioBus bus)
        {
            if (catalog == null) return;
            if (bus == AudioBus.Music || bus == AudioBus.Ambience) return;
            PlayCue(AudioCueId.UIClick);
        }
        public void PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return; GameObject emitter = new("One shot audio"); emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>(); source.clip = clip; source.volume = volume * (mixer == null ? GetBusVolume(AudioBus.Sfx) : 1f);
            source.spatialBlend = 1f; source.minDistance = 2f; source.maxDistance = 45f; source.outputAudioMixerGroup = sfxGroup;
            source.Play(); Destroy(emitter, clip.length + .1f); LastPlayedClipName = clip.name;
        }
        public void ApplyMapProfile(MapAudioProfile profile, float transitionSeconds = .35f, float outputGain = 1f)
        {
            if (profile == null) return;
            EnsureSources();
            if (ambienceTransition != null) StopCoroutine(ambienceTransition);
            ambienceTransition = StartCoroutine(CrossfadeAmbience(profile, transitionSeconds, Mathf.Clamp01(outputGain)));
        }
        public void SetMasterVolume(float value) => SetBusVolume(AudioBus.Master, value);
        public void SetMusicVolume(float value) => SetBusVolume(AudioBus.Music, value);
        public void SetSfxVolume(float value) => SetBusVolume(AudioBus.Sfx, value);
        public void SetAmbienceVolume(float value) => SetBusVolume(AudioBus.Ambience, value);
        public void SetUiVolume(float value) => SetBusVolume(AudioBus.UI, value);
        public void FadeOut(AudioSource source, float seconds) { if (source != null) StartCoroutine(FadeOutRoutine(source, seconds)); }

        private void ResolveMixerAssets()
        {
            if (mixer == null) return; musicGroup = FindGroup("Music"); sfxGroup = FindGroup("SFX"); ambienceGroup = FindGroup("Ambience"); uiGroup = FindGroup("UI"); raceGroup = FindGroup("Race"); engineGroup = FindGroup("Engine"); tiresGroup = FindGroup("Tires"); collisionGroup = FindGroup("Collision");
            defaultSnapshot = mixer.FindSnapshot("Default"); countdownSnapshot = mixer.FindSnapshot("Countdown"); pausedSnapshot = mixer.FindSnapshot("Paused"); finishSnapshot = mixer.FindSnapshot("Finish");
        }
        private AudioMixerGroup FindGroup(string name) { AudioMixerGroup[] found = mixer.FindMatchingGroups(name); return found.Length > 0 ? found[0] : null; }
        private void EnsureSources()
        {
            ResolveMixerAssets(); if (sfxSource == null) sfxSource = CreateSource(false, 1f, sfxGroup); if (raceSource == null) raceSource = CreateSource(false, 1f, raceGroup); if (uiSource == null) uiSource = CreateSource(false, 1f, uiGroup);
            if (musicSource == null) musicSource = CreateSource(true, mixer == null ? DefaultMusicVolume : 1f, musicGroup); if (ambiencePrimary == null) ambiencePrimary = CreateSource(true, .35f, ambienceGroup); if (ambienceSecondary == null) ambienceSecondary = CreateSource(true, .15f, ambienceGroup);
            RouteSources();
        }
        private void RouteSources()
        {
            if (sfxSource != null) sfxSource.outputAudioMixerGroup = sfxGroup;
            if (raceSource != null) raceSource.outputAudioMixerGroup = raceGroup;
            if (uiSource != null) uiSource.outputAudioMixerGroup = uiGroup;
            if (musicSource != null) musicSource.outputAudioMixerGroup = musicGroup;
            if (ambiencePrimary != null) ambiencePrimary.outputAudioMixerGroup = ambienceGroup;
            if (ambienceSecondary != null) ambienceSecondary.outputAudioMixerGroup = ambienceGroup;
        }
        private AudioSource CreateSource(bool loop, float volume, AudioMixerGroup group) { AudioSource s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = loop; s.volume = volume; s.outputAudioMixerGroup = group; return s; }
        private void ApplyMuteState() { AudioListener.volume = IsMuted ? 0f : mixer == null ? GetBusVolume(AudioBus.Master) : 1f; ApplyFallbackVolumes(); }
        private void ApplyFallbackVolumes()
        {
            if (mixer != null) return; AudioListener.volume = IsMuted ? 0f : GetBusVolume(AudioBus.Master);
            if (musicSource != null) musicSource.volume = GetBusVolume(AudioBus.Music) * currentMusicGain; if (sfxSource != null) sfxSource.volume = GetBusVolume(AudioBus.Sfx); if (uiSource != null) uiSource.volume = GetBusVolume(AudioBus.UI);
            float a = GetBusVolume(AudioBus.Ambience); if (ambiencePrimary != null) ambiencePrimary.volume = ambiencePrimaryBaseVolume * a; if (ambienceSecondary != null) ambienceSecondary.volume = ambienceSecondaryBaseVolume * a;
        }
        private static string BusKey(AudioBus bus) => bus.ToString().ToLowerInvariant();
        private static float DefaultVolume(AudioBus bus) => bus == AudioBus.Master ? MaxMasterVolume : bus == AudioBus.Music ? DefaultMusicVolume : bus == AudioBus.Ambience ? DefaultAmbienceVolume : 1f;
        private static void PlayLoop(AudioSource source, AudioClip clip, float volume) { if (source == null || clip == null || source.clip == clip && source.isPlaying) return; source.Stop(); source.clip = clip; source.volume = volume; source.loop = true; source.Play(); }
        private IEnumerator CrossfadeMusic(AudioClip clip, float gain, float seconds)
        {
            if (clip == null || musicSource == null) yield break;
            gain = Mathf.Clamp01(gain);
            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                currentMusicGain = gain;
                musicSource.volume = (mixer == null ? GetBusVolume(AudioBus.Music) : 1f) * currentMusicGain;
                yield break;
            }
            float start = musicSource.volume;
            for (float t = 0; musicSource.isPlaying && t < seconds; t += Time.unscaledDeltaTime) { musicSource.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(.01f, seconds)); yield return null; }
            musicSource.Stop(); musicSource.clip = clip; musicSource.loop = true; currentMusicGain = gain; musicSource.Play();
            float target = (mixer == null ? GetBusVolume(AudioBus.Music) : 1f) * currentMusicGain;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { musicSource.volume = Mathf.Lerp(0f, target, t / Mathf.Max(.01f, seconds)); yield return null; } musicSource.volume = target;
        }

        private void ApplyLowPass(float cutoff)
        {
            if (lowPassFilter == null) lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null) lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = cutoff;
        }
        private IEnumerator CrossfadeAmbience(MapAudioProfile profile, float seconds, float outputGain)
        {
            float half = Mathf.Max(.01f, seconds * .5f);
            bool sameClips = ambiencePrimary.clip == profile.primaryAmbience && ambienceSecondary.clip == profile.secondaryAmbience;
            float primaryStart = ambiencePrimary.volume;
            float secondaryStart = ambienceSecondary.volume;
            float busScale = mixer == null ? GetBusVolume(AudioBus.Ambience) : 1f;
            float primaryTarget = profile.primaryVolume * busScale * outputGain;
            float secondaryTarget = profile.secondaryVolume * busScale * outputGain;

            if (!sameClips)
            {
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    float a = t / half;
                    ambiencePrimary.volume = Mathf.Lerp(primaryStart, 0f, a);
                    ambienceSecondary.volume = Mathf.Lerp(secondaryStart, 0f, a);
                    yield return null;
                }

                ambiencePrimary.Stop();
                ambienceSecondary.Stop();
                ambiencePrimary.clip = profile.primaryAmbience;
                ambienceSecondary.clip = profile.secondaryAmbience;
                ambiencePrimary.loop = true;
                ambienceSecondary.loop = true;
                ambiencePrimary.volume = 0f;
                ambienceSecondary.volume = 0f;
                if (ambiencePrimary.clip != null) ambiencePrimary.Play();
                if (ambienceSecondary.clip != null) ambienceSecondary.Play();
                primaryStart = 0f;
                secondaryStart = 0f;
            }

            ambiencePrimaryBaseVolume = profile.primaryVolume * outputGain;
            ambienceSecondaryBaseVolume = profile.secondaryVolume * outputGain;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                float a = t / half;
                ambiencePrimary.volume = Mathf.Lerp(primaryStart, primaryTarget, a);
                ambienceSecondary.volume = Mathf.Lerp(secondaryStart, secondaryTarget, a);
                yield return null;
            }

            ambiencePrimary.volume = primaryTarget;
            ambienceSecondary.volume = secondaryTarget;
            ambienceTransition = null;
        }
        private IEnumerator ApplyRuntimeSnapshot(AudioSnapshotId id, float seconds)
        {
            float musicTarget = (mixer == null ? GetBusVolume(AudioBus.Music) : 1f) * currentMusicGain, ambienceTarget = GetBusVolume(AudioBus.Ambience), sfxTarget = GetBusVolume(AudioBus.Sfx);
            if (id == AudioSnapshotId.Countdown) musicTarget *= .45f;
            else if (id == AudioSnapshotId.Paused) { musicTarget *= .25f; ambienceTarget *= .25f; sfxTarget *= .5f; }
            else if (id == AudioSnapshotId.Finish) ambienceTarget *= .2f;
            float musicStart = musicSource != null ? musicSource.volume : 0f, primaryStart = ambiencePrimary != null ? ambiencePrimary.volume : 0f, sfxStart = sfxSource != null ? sfxSource.volume : 0f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float a = t / Mathf.Max(.01f, seconds); if (musicSource != null) musicSource.volume = Mathf.Lerp(musicStart, musicTarget, a);
                if (ambiencePrimary != null) ambiencePrimary.volume = Mathf.Lerp(primaryStart, ambienceTarget * .35f, a); if (sfxSource != null) sfxSource.volume = Mathf.Lerp(sfxStart, sfxTarget, a); yield return null;
            }
        }
        private void OnApplicationQuit() => FlushSettings();
        private static IEnumerator FadeOutRoutine(AudioSource source, float seconds) { float start = source.volume; for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { source.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(.01f, seconds)); yield return null; } source.Stop(); source.volume = start; }
    }
}
