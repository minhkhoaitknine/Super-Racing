using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
                bool accepted = tracker.TryPassCheckpoint(this);
                if (!accepted && checkpointIndex == 0)
                {
                    RaceManager manager = FindFirstObjectByType<RaceManager>();
                    manager?.TryCompleteFromFinishLine(tracker);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!IsFinishLine())
            {
                return;
            }

            Collider trigger = GetComponent<Collider>();
            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = new Color(0f, 1f, 1f, 0.08f);
            if (trigger is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(box.center, box.size);
                DrawLocalCross(box.center, box.size);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 2f);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 8f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 8f, 0.75f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 8f);
            Gizmos.DrawSphere(transform.position + transform.forward * 8f, 0.55f);
            Gizmos.color = previousColor;

#if UNITY_EDITOR
            Handles.color = Color.cyan;
            Handles.Label(transform.position + Vector3.up * 5f, "FINISH LINE");
#endif
        }

        private bool IsFinishLine()
        {
            return checkpointIndex == 0 || name.Equals("FinishLine", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void DrawLocalCross(Vector3 center, Vector3 size)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float y = center.y - size.y * 0.5f + 0.08f;

            Vector3 frontLeft = new(center.x - halfX, y, center.z + halfZ);
            Vector3 frontRight = new(center.x + halfX, y, center.z + halfZ);
            Vector3 backLeft = new(center.x - halfX, y, center.z - halfZ);
            Vector3 backRight = new(center.x + halfX, y, center.z - halfZ);

            Gizmos.DrawLine(frontLeft, backRight);
            Gizmos.DrawLine(frontRight, backLeft);
        }
    }
}
