using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.Audio
{
    public sealed class UIButtonAudio : MonoBehaviour
    {
        private Button automaticButton;
        private AudioCueId automaticCue = AudioCueId.UIClick;

        public void EnableAutomaticClick(AudioCueId cue = AudioCueId.UIClick)
        {
            automaticCue = cue;
            if (automaticButton != null) return;
            automaticButton = GetComponent<Button>();
            if (automaticButton != null) automaticButton.onClick.AddListener(PlayAutomaticCue);
        }

        private void OnDestroy()
        {
            if (automaticButton != null) automaticButton.onClick.RemoveListener(PlayAutomaticCue);
        }

        private void PlayAutomaticCue() => GameAudioManager.Instance?.PlayCue(automaticCue);
        public void PlayClick() => Play(GameAudioManager.Instance?.Catalog?.uiClick);
        public void PlayConfirm() => Play(GameAudioManager.Instance?.Catalog?.uiConfirm);
        public void PlayBack() => Play(GameAudioManager.Instance?.Catalog?.uiBack);
        public void PlaySelectionChanged() => Play(GameAudioManager.Instance?.Catalog?.uiSelectionChanged);
        public void PlayError() => Play(GameAudioManager.Instance?.Catalog?.uiError);
        private static void Play(AudioClip clip, float volume = 1f) => GameAudioManager.Instance?.PlayOneShot(clip, volume, true);
    }
}
