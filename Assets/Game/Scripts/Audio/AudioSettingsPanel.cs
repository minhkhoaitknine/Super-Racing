using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider, musicSlider, sfxSlider, ambienceSlider, uiSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Text masterValue, musicValue, sfxValue, ambienceValue, uiValue, muteLabel;
        private bool syncing;
        private Coroutine introRoutine;
        public event Action CloseRequested;

        private void OnEnable()
        {
            Bind(masterSlider, AudioBus.Master, masterValue); Bind(musicSlider, AudioBus.Music, musicValue);
            Bind(sfxSlider, AudioBus.Sfx, sfxValue); Bind(ambienceSlider, AudioBus.Ambience, ambienceValue); Bind(uiSlider, AudioBus.UI, uiValue);
            if (muteToggle != null) { muteToggle.onValueChanged.RemoveListener(OnMuteChanged); muteToggle.onValueChanged.AddListener(OnMuteChanged); }
            Refresh();
            if (introRoutine != null) StopCoroutine(introRoutine);
            introRoutine = StartCoroutine(PlayIntro());
        }
        private void OnDisable()
        {
            if (introRoutine != null) StopCoroutine(introRoutine);
            introRoutine = null;
            GameAudioManager.Instance?.FlushSettings();
        }
        public void Refresh()
        {
            GameAudioManager manager = GameAudioManager.Instance; if (manager == null) return; syncing = true;
            Set(masterSlider, manager.GetBusVolume(AudioBus.Master), masterValue); Set(musicSlider, manager.GetBusVolume(AudioBus.Music), musicValue);
            Set(sfxSlider, manager.GetBusVolume(AudioBus.Sfx), sfxValue); Set(ambienceSlider, manager.GetBusVolume(AudioBus.Ambience), ambienceValue); Set(uiSlider, manager.GetBusVolume(AudioBus.UI), uiValue);
            if (muteToggle != null) muteToggle.isOn = manager.IsMuted; UpdateMuteLabel(manager.IsMuted); syncing = false;
        }
        public void ResetDefaults() { GameAudioManager.Instance?.ResetAudioSettings(); Refresh(); }
        public void RequestClose() => CloseRequested?.Invoke();
        private void Bind(Slider slider, AudioBus bus, Text label)
        {
            if (slider == null) return; slider.minValue = 0f; slider.maxValue = bus == AudioBus.Master ? GameAudioManager.MaxMasterVolume : 1f; slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners(); slider.onValueChanged.AddListener(value => { if (syncing) return; GameAudioManager.Instance?.SetBusVolume(bus, value); SetLabel(label, value); });
            AudioSliderPreview preview = slider.GetComponent<AudioSliderPreview>();
            if (preview == null) preview = slider.gameObject.AddComponent<AudioSliderPreview>();
            preview.Bus = bus;
        }
        private void OnMuteChanged(bool value) { if (syncing) return; GameAudioManager.Instance?.SetMuted(value); UpdateMuteLabel(value); }
        private void UpdateMuteLabel(bool muted) { if (muteLabel != null) muteLabel.text = muted ? "MUTED" : "MUTE ALL"; }
        private static void Set(Slider slider, float value, Text label) { if (slider != null) slider.SetValueWithoutNotify(value); SetLabel(label, value); }
        private static void SetLabel(Text label, float value) { if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%"; }

        private IEnumerator PlayIntro()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            RectTransform card = transform.Find("Settings Card") as RectTransform;
            if (group == null || card == null) yield break;
            group.alpha = 0f;
            card.localScale = Vector3.one * .96f;
            const float duration = .18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float linear = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - linear, 3f);
                group.alpha = eased;
                card.localScale = Vector3.one * Mathf.Lerp(.96f, 1f, eased);
                yield return null;
            }
            group.alpha = 1f;
            card.localScale = Vector3.one;
            introRoutine = null;
        }
    }

    public sealed class AudioSliderPreview : MonoBehaviour, IPointerUpHandler
    {
        public AudioBus Bus { get; set; }
        public void OnPointerUp(PointerEventData _) => GameAudioManager.Instance?.PlayBusPreview(Bus);
    }
}
