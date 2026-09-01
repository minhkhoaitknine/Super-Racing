using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour
    {
        private const string Prefix = "audio.";
        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup, sfxGroup, ambienceGroup, uiGroup, raceGroup, engineGroup, tiresGroup, collisionGroup;
        [SerializeField] private AudioMixerSnapshot defaultSnapshot, countdownSnapshot, pausedSnapshot, finishSnapshot;
        [SerializeField] private AudioSource sfxSource, raceSource, uiSource, musicSource, ambiencePrimary, ambienceSecondary;
        public static GameAudioManager Instance { get; private set; }
        public AudioCatalog Catalog => catalog;
        public string LastPlayedClipName { get; private set; } = "Nothing played yet";
        public bool IsMuted { get; private set; }
        public AudioMixerGroup GetMixerGroup(string groupName) => mixer != null ? FindGroup(groupName) : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); EnsureSources();
            IsMuted = PlayerPrefs.GetInt(Prefix + "muted", 0) != 0;
            SetBusVolume(AudioBus.Master, PlayerPrefs.GetFloat(Prefix + "master", 1f));
            SetBusVolume(AudioBus.Music, PlayerPrefs.GetFloat(Prefix + "music", .7f));
            SetBusVolume(AudioBus.Sfx, PlayerPrefs.GetFloat(Prefix + "sfx", 1f));
            SetBusVolume(AudioBus.Ambience, PlayerPrefs.GetFloat(Prefix + "ambience", .7f));
            SetBusVolume(AudioBus.UI, PlayerPrefs.GetFloat(Prefix + "ui", 1f)); ApplyMuteState();
        }

        public void Configure(AudioCatalog value, AudioMixer audioMixer = null) { catalog = value; mixer = audioMixer; ResolveMixerAssets(); EnsureSources(); }
        public static float NormalizedToDb(float value) => value <= .0001f ? -80f : Mathf.Log10(Mathf.Clamp01(value)) * 20f;
        public float GetBusVolume(AudioBus bus) => PlayerPrefs.GetFloat(Prefix + BusKey(bus), DefaultVolume(bus));
        public void SetBusVolume(AudioBus bus, float value)
        {
            value = Mathf.Clamp01(value); PlayerPrefs.SetFloat(Prefix + BusKey(bus), value);
            string parameter = bus switch { AudioBus.Master => "MasterVolume", AudioBus.Music => "MusicVolume", AudioBus.Sfx => "SfxVolume", AudioBus.Ambience => "AmbienceVolume", _ => "UiVolume" };
            if (mixer == null || !mixer.SetFloat(parameter, NormalizedToDb(value))) ApplyFallbackVolumes();
        }
        public void SetMuted(bool muted) { IsMuted = muted; PlayerPrefs.SetInt(Prefix + "muted", muted ? 1 : 0); ApplyMuteState(); }
        public void ResetAudioSettings()
        {
            SetMuted(false); SetBusVolume(AudioBus.Master, 1f); SetBusVolume(AudioBus.Music, .7f);
            SetBusVolume(AudioBus.Sfx, 1f); SetBusVolume(AudioBus.Ambience, .7f); SetBusVolume(AudioBus.UI, 1f); PlayerPrefs.Save();
        }
        public void ApplySnapshot(AudioSnapshotId id, float transitionSeconds = .25f)
        {
            AudioMixerSnapshot snapshot = id switch { AudioSnapshotId.Countdown => countdownSnapshot, AudioSnapshotId.Paused => pausedSnapshot, AudioSnapshotId.Finish => finishSnapshot, _ => defaultSnapshot };
            if (snapshot != null) snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
            else StartCoroutine(ApplyRuntimeSnapshot(id, transitionSeconds));
        }
        public void PlayCue(AudioCueId cue, Vector3? position = null, float volume = 1f)
        { AudioClip clip = catalog != null ? catalog.GetCue(cue) : null; if (position.HasValue) PlayAtPoint(clip, position.Value, volume); else if (cue >= AudioCueId.UIHover) PlayOneShot(clip, volume, true); else { if (clip != null) raceSource.PlayOneShot(clip, volume); LastPlayedClipName = clip != null ? clip.name : LastPlayedClipName; } }
        public void PlayMusic(MusicId id, float fadeSeconds = .35f)
        {
            if (catalog == null) return;
            AudioClip clip = id switch { MusicId.Menu => catalog.menuMusic, MusicId.Result => catalog.resultMusic, _ => catalog.raceMusic };
            StartCoroutine(CrossfadeMusic(clip, fadeSeconds));
        }
        public void PlayMenuMusic() => PlayMusic(MusicId.Menu);
        public void PlayRaceMusic() => PlayMusic(MusicId.Race);
        public void PlayResultMusic() => PlayMusic(MusicId.Result);
        public void StopMusic() { if (musicSource != null) musicSource.Stop(); }
        public void StopAmbience()
        {
            if (ambiencePrimary != null) ambiencePrimary.Stop();
            if (ambienceSecondary != null) ambienceSecondary.Stop();
        }
        public void PlayOneShot(AudioClip clip, float volume = 1f, bool ui = false)
        { if (clip == null) return; EnsureSources(); (ui ? uiSource : sfxSource).PlayOneShot(clip, volume); LastPlayedClipName = clip.name; }
        public void PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return; GameObject emitter = new("One shot audio"); emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>(); source.clip = clip; source.volume = volume * GetBusVolume(AudioBus.Sfx);
            source.spatialBlend = 1f; source.minDistance = 2f; source.maxDistance = 45f; source.outputAudioMixerGroup = sfxGroup;
            source.Play(); Destroy(emitter, clip.length + .1f); LastPlayedClipName = clip.name;
        }
        public void ApplyMapProfile(MapAudioProfile profile)
        { if (profile == null) return; PlayLoop(ambiencePrimary, profile.primaryAmbience, profile.primaryVolume); PlayLoop(ambienceSecondary, profile.secondaryAmbience, profile.secondaryVolume); }
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
            if (musicSource == null) musicSource = CreateSource(true, .7f, musicGroup); if (ambiencePrimary == null) ambiencePrimary = CreateSource(true, .35f, ambienceGroup); if (ambienceSecondary == null) ambienceSecondary = CreateSource(true, .15f, ambienceGroup);
        }
        private AudioSource CreateSource(bool loop, float volume, AudioMixerGroup group) { AudioSource s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = loop; s.volume = volume; s.outputAudioMixerGroup = group; return s; }
        private void ApplyMuteState() { AudioListener.volume = IsMuted ? 0f : GetBusVolume(AudioBus.Master); ApplyFallbackVolumes(); }
        private void ApplyFallbackVolumes()
        {
            if (mixer != null) return; AudioListener.volume = IsMuted ? 0f : GetBusVolume(AudioBus.Master);
            if (musicSource != null) musicSource.volume = GetBusVolume(AudioBus.Music); if (sfxSource != null) sfxSource.volume = GetBusVolume(AudioBus.Sfx); if (uiSource != null) uiSource.volume = GetBusVolume(AudioBus.UI);
            float a = GetBusVolume(AudioBus.Ambience); if (ambiencePrimary != null) ambiencePrimary.volume = .35f * a; if (ambienceSecondary != null) ambienceSecondary.volume = .15f * a;
        }
        private static string BusKey(AudioBus bus) => bus.ToString().ToLowerInvariant();
        private static float DefaultVolume(AudioBus bus) => bus == AudioBus.Music || bus == AudioBus.Ambience ? .7f : 1f;
        private static void PlayLoop(AudioSource source, AudioClip clip, float volume) { if (source == null || clip == null || source.clip == clip && source.isPlaying) return; source.Stop(); source.clip = clip; source.volume = volume; source.loop = true; source.Play(); }
        private IEnumerator CrossfadeMusic(AudioClip clip, float seconds)
        {
            if (clip == null || musicSource == null || musicSource.clip == clip && musicSource.isPlaying) yield break; float start = musicSource.volume;
            for (float t = 0; musicSource.isPlaying && t < seconds; t += Time.unscaledDeltaTime) { musicSource.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(.01f, seconds)); yield return null; }
            musicSource.Stop(); musicSource.clip = clip; musicSource.loop = true; musicSource.Play();
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { musicSource.volume = Mathf.Lerp(0f, GetBusVolume(AudioBus.Music), t / Mathf.Max(.01f, seconds)); yield return null; } musicSource.volume = GetBusVolume(AudioBus.Music);
        }
        private IEnumerator ApplyRuntimeSnapshot(AudioSnapshotId id, float seconds)
        {
            float musicTarget = GetBusVolume(AudioBus.Music), ambienceTarget = GetBusVolume(AudioBus.Ambience), sfxTarget = GetBusVolume(AudioBus.Sfx);
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
        private static IEnumerator FadeOutRoutine(AudioSource source, float seconds) { float start = source.volume; for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { source.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(.01f, seconds)); yield return null; } source.Stop(); source.volume = start; }
    }
}
