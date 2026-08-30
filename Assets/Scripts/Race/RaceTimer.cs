using System;
using UnityEngine;
using UnityEngine.Events;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    public sealed class RaceTimer : MonoBehaviour
    {
        [Serializable]
        public sealed class TimeChangedEvent : UnityEvent<float> { }

        [SerializeField] private TimeChangedEvent onTimeChanged = new();

        public float ElapsedSeconds { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action<float> TimeChanged;

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            ElapsedSeconds += Time.deltaTime;
            TimeChanged?.Invoke(ElapsedSeconds);
            onTimeChanged.Invoke(ElapsedSeconds);
        }

        public void StartTimer()
        {
            IsRunning = true;
        }

        public void StopTimer()
        {
            IsRunning = false;
        }

        public void ResetTimer()
        {
            IsRunning = false;
            ElapsedSeconds = 0f;
            TimeChanged?.Invoke(ElapsedSeconds);
            onTimeChanged.Invoke(ElapsedSeconds);
        }
    }
}
