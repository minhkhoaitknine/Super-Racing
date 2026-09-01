using UnityEngine;

namespace SuperRacing.Audio
{
    public sealed class VehicleAudioMonitorOverlay : MonoBehaviour
    {
        private GUIStyle boxStyle;
        private GUIStyle warningStyle;

        private void OnGUI()
        {
            VehicleAudioEmitter vehicle = FindFirstObjectByType<VehicleAudioEmitter>();
            if (vehicle == null) return;

            boxStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            warningStyle ??= new GUIStyle(boxStyle)
            {
                normal = { textColor = new Color(1f, .8f, .2f) }
            };

            VehicleAudioTelemetry telemetry = vehicle.CurrentTelemetry;
            string text =
                "VEHICLE AUDIO MONITOR\n" +
                $"Last cue: {vehicle.LastOneShotClipName}\n" +
                $"One-shot count: {vehicle.OneShotPlayCount}\n" +
                $"Loudest loop: {vehicle.LoudestContinuousLayer}\n" +
                $"Gear: {telemetry.CurrentGear}   RPM: {telemetry.NormalizedRpm:0.00}\n" +
                $"Speed: {telemetry.SpeedKmh:0} km/h   Slip: {Mathf.Max(telemetry.ForwardSlip, telemetry.SidewaysSlip):0.00}";

            Rect rect = new Rect(Screen.width - 505f, 15f, 490f, 155f);
            GUI.Box(rect, text, vehicle.OneShotPlayCount > 8 ? warningStyle : boxStyle);
        }
    }
}
