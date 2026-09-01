using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperRacing.Audio
{
    public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public void PlayClick() => Play(GameAudioManager.Instance?.Catalog?.uiClick);
        public void PlayConfirm() => Play(GameAudioManager.Instance?.Catalog?.uiConfirm);
        public void PlayBack() => Play(GameAudioManager.Instance?.Catalog?.uiBack);
        public void PlaySelectionChanged() => Play(GameAudioManager.Instance?.Catalog?.uiSelectionChanged);
        public void PlayError() => Play(GameAudioManager.Instance?.Catalog?.uiError);
        public void OnPointerEnter(PointerEventData eventData) => Play(GameAudioManager.Instance?.Catalog?.uiHover, 0.45f);
        public void OnSelect(BaseEventData eventData) => Play(GameAudioManager.Instance?.Catalog?.uiHover, 0.45f);

        private static void Play(AudioClip clip, float volume = 1f) => GameAudioManager.Instance?.PlayOneShot(clip, volume, true);
    }
}
