using UnityEngine;
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

        private void OnEnable()
        {
            Bind(masterSlider, AudioBus.Master, masterValue); Bind(musicSlider, AudioBus.Music, musicValue);
            Bind(sfxSlider, AudioBus.Sfx, sfxValue); Bind(ambienceSlider, AudioBus.Ambience, ambienceValue); Bind(uiSlider, AudioBus.UI, uiValue);
            if (muteToggle != null) { muteToggle.onValueChanged.RemoveListener(OnMuteChanged); muteToggle.onValueChanged.AddListener(OnMuteChanged); }
            Refresh();
        }
        public void Refresh()
        {
            GameAudioManager manager = GameAudioManager.Instance; if (manager == null) return; syncing = true;
            Set(masterSlider, manager.GetBusVolume(AudioBus.Master), masterValue); Set(musicSlider, manager.GetBusVolume(AudioBus.Music), musicValue);
            Set(sfxSlider, manager.GetBusVolume(AudioBus.Sfx), sfxValue); Set(ambienceSlider, manager.GetBusVolume(AudioBus.Ambience), ambienceValue); Set(uiSlider, manager.GetBusVolume(AudioBus.UI), uiValue);
            if (muteToggle != null) muteToggle.isOn = manager.IsMuted; UpdateMuteLabel(manager.IsMuted); syncing = false;
        }
        public void ResetDefaults() { GameAudioManager.Instance?.ResetAudioSettings(); Refresh(); }
        private void Bind(Slider slider, AudioBus bus, Text label)
        {
            if (slider == null) return; slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners(); slider.onValueChanged.AddListener(value => { if (syncing) return; GameAudioManager.Instance?.SetBusVolume(bus, value); SetLabel(label, value); });
        }
        private void OnMuteChanged(bool value) { if (syncing) return; GameAudioManager.Instance?.SetMuted(value); UpdateMuteLabel(value); }
        private void UpdateMuteLabel(bool muted) { if (muteLabel != null) muteLabel.text = muted ? "Unmute" : "Mute"; }
        private static void Set(Slider slider, float value, Text label) { if (slider != null) slider.SetValueWithoutNotify(value); SetLabel(label, value); }
        private static void SetLabel(Text label, float value) { if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%"; }
    }
}
