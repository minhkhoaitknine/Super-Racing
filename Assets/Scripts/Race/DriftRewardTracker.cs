using SuperRacing.Contracts;
using UnityEngine;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    public sealed class DriftRewardTracker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minimumCleanDriftSeconds = 1f;
        [SerializeField, Min(0f)] private float collisionImpactThreshold = 2.5f;

        private IVehicleController controller;
        private float currentDriftSeconds;
        private float earnedCleanDriftSeconds;
        private bool currentDriftIsClean = true;

        public float CleanDriftSeconds => earnedCleanDriftSeconds;

        public void Configure(IVehicleController vehicleController) => controller = vehicleController;

        public void ResetProgress()
        {
            currentDriftSeconds = 0f;
            earnedCleanDriftSeconds = 0f;
            currentDriftIsClean = true;
        }

        private void Update()
        {
            if (controller != null && controller.CanDrive && controller.IsDrifting)
            {
                currentDriftSeconds += Time.deltaTime;
            }
            else
            {
                CompleteCurrentDrift();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (currentDriftSeconds > 0f && collision.relativeVelocity.magnitude >= collisionImpactThreshold)
            {
                currentDriftIsClean = false;
            }
        }

        public void CompleteCurrentDrift()
        {
            if (currentDriftIsClean && currentDriftSeconds >= minimumCleanDriftSeconds)
            {
                earnedCleanDriftSeconds += currentDriftSeconds;
            }

            currentDriftSeconds = 0f;
            currentDriftIsClean = true;
        }
    }
}
