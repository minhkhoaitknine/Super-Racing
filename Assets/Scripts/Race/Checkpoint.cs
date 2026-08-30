using UnityEngine;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Min(0)] [SerializeField] private int checkpointIndex;

        public int CheckpointIndex => checkpointIndex;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnValidate()
        {
            checkpointIndex = Mathf.Max(0, checkpointIndex);

            Collider trigger = GetComponent<Collider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            LapTracker tracker = other.GetComponentInParent<LapTracker>();
            if (tracker != null)
            {
                tracker.TryPassCheckpoint(this);
            }
        }
    }
}
