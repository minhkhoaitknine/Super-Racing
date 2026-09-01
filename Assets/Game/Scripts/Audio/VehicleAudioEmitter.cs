using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleAudioEmitter : MonoBehaviour
    {
        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private VehicleAudioProfile profile;
        [SerializeField] private SurfaceAudioProfile[] surfaces;
        [SerializeField] private float collisionCooldown = 2f;
        [SerializeField] private float minimumCollisionSpeed = 3f;
        [SerializeField] private float minimumShiftInterval = .65f;
        [SerializeField] private float gearConfirmationTime = .18f;
        [SerializeField] private float mediumCollisionImpulse = 5f;
        [SerializeField] private float heavyCollisionImpulse = 11f;
        [SerializeField] private float landingVelocity = 3f;
        [SerializeField] private bool enableCollisionOneShots = true;
        [SerializeField] private bool enableLandingOneShots = true;
        [SerializeField] private bool enableTireRoll = true;
        [SerializeField] private bool enableTireSkid;
        private readonly List<AudioSource> rpmSources = new();
        private AudioSource loadSource, offLoadSource, rollSource, skidSource, oneShotSource;
        private Rigidbody body;
        private IVehicleAudioTelemetrySource telemetrySource;
        private VehicleAudioTelemetry telemetry;
        private int previousGear = 1, pendingGear = 1;
        private float shiftTimer, lastCollisionTime, lastShiftTime = -10f, pendingGearSince, airborneSince = -1f;
        private bool wasGrounded = true;
        private bool engineMuted, tiresMuted, oneShotsMuted;
        private float airborneDownVelocity;
        private float externalThrottle = -1f;
        private bool externalBrake;

        public VehicleAudioTelemetry CurrentTelemetry => telemetry;
        public VehicleAudioProfile Profile => profile;
        public string LastOneShotClipName { get; private set; } = "None";
        public int OneShotPlayCount { get; private set; }
        public string LoudestContinuousLayer
        {
            get
            {
                string label = "None"; float loudest = .005f;
                CheckLayer("Engine load", loadSource, ref label, ref loudest);
                CheckLayer("Engine off-load", offLoadSource, ref label, ref loudest);
                CheckLayer("Tire roll", rollSource, ref label, ref loudest);
                CheckLayer("Tire skid", skidSource, ref label, ref loudest);
                for (int i = 0; i < rpmSources.Count; i++) CheckLayer("Engine RPM " + i, rpmSources[i], ref label, ref loudest);
                return label + " (" + loudest.ToString("0.00") + ")";
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); telemetrySource = FindTelemetrySource();
            if (FindFirstObjectByType<VehicleAudioMonitorOverlay>() == null)
                new GameObject("Vehicle Audio Monitor").AddComponent<VehicleAudioMonitorOverlay>();
            if (catalog == null) catalog = GameAudioManager.Instance != null ? GameAudioManager.Instance.Catalog : Resources.Load<AudioCatalog>("AudioCatalog");
            if (profile == null && catalog != null) profile = ProfileForVehicleName(catalog, gameObject.name);
            if (surfaces == null || surfaces.Length == 0 && catalog != null) surfaces = new[] { catalog.asphaltSurface, catalog.sandSurface, catalog.grassSurface };
            BuildSources();
        }
        private void Start()
        {
            AudioClip start = profile != null ? profile.engineStart : catalog?.engineStart;
            if (start != null) PlayVehicleOneShot(start, .55f); foreach (AudioSource source in rpmSources) source.Play();
            loadSource?.Play(); offLoadSource?.Play(); rollSource?.Play(); skidSource?.Play();
        }
        private void Update()
        {
            HandleDiagnosticKeys();
            telemetry = telemetrySource != null ? telemetrySource.AudioTelemetry : BuildFallbackTelemetry();
            if (externalThrottle >= 0f) telemetry.Throttle = externalThrottle; if (externalBrake) telemetry.Brake = 1f;
            UpdateGearAndRpm(); UpdateEngine(); UpdateTires(); UpdateLanding();
            ApplyDiagnosticMutes();
        }
        private void HandleDiagnosticKeys()
        {
            Keyboard keyboard = Keyboard.current; if (keyboard == null) return;
            if (keyboard.f6Key.wasPressedThisFrame) { engineMuted = !engineMuted; Debug.Log($"[VehicleAudio] ENGINE {(engineMuted ? "MUTED" : "ON")}", this); }
            if (keyboard.f7Key.wasPressedThisFrame) { tiresMuted = !tiresMuted; Debug.Log($"[VehicleAudio] TIRES {(tiresMuted ? "MUTED" : "ON")}", this); }
            if (keyboard.f8Key.wasPressedThisFrame) { oneShotsMuted = !oneShotsMuted; Debug.Log($"[VehicleAudio] ONE-SHOTS {(oneShotsMuted ? "MUTED" : "ON")}", this); }
        }
        private void ApplyDiagnosticMutes()
        {
            foreach (AudioSource source in rpmSources) if (source != null) source.mute = engineMuted;
            if (loadSource != null) loadSource.mute = engineMuted;
            if (offLoadSource != null) offLoadSource.mute = engineMuted;
            if (rollSource != null) rollSource.mute = tiresMuted;
            if (skidSource != null) skidSource.mute = tiresMuted;
            if (oneShotSource != null) oneShotSource.mute = oneShotsMuted;
        }
        public void SetProfile(VehicleAudioProfile value) { if (value == null || value == profile) return; profile = value; RebuildSources(); }
        public void SetThrottle(float value) => externalThrottle = Mathf.Clamp01(Mathf.Abs(value));
        public void SetBrake(bool value) => externalBrake = value;

        private void UpdateGearAndRpm()
        {
            if (profile == null) return;
            int gear = telemetry.CurrentGear > 0 ? telemetry.CurrentGear : profile.GearForSpeed(telemetry.SpeedKmh);
            if (gear != pendingGear) { pendingGear = gear; pendingGearSince = Time.unscaledTime; }
            bool confirmed = gear != previousGear && Time.unscaledTime - pendingGearSince >= gearConfirmationTime;
            bool cooledDown = Time.unscaledTime - lastShiftTime >= minimumShiftInterval;
            if (confirmed && cooledDown && telemetry.SpeedKmh > 5f)
            {
                shiftTimer = profile.shiftDuration;
                // Older generated profiles used the race Restart cue as a placeholder gear shift.
                // Never play that incorrect mapping; wait for a dedicated vehicle shift sample.
                if (profile.gearShift != null && (catalog == null || profile.gearShift != catalog.restart))
                    PlayVehicleOneShot(profile.gearShift, .2f);
                previousGear = gear;
                lastShiftTime = Time.unscaledTime;
            }
            telemetry.CurrentGear = previousGear;
            if (telemetry.NormalizedRpm <= 0f) telemetry.NormalizedRpm = profile.RpmForSpeed(telemetry.SpeedKmh, gear, telemetry.Throttle);
            shiftTimer = Mathf.Max(0f, shiftTimer - Time.deltaTime); if (shiftTimer > 0f) telemetry.NormalizedRpm *= Mathf.Lerp(.55f, 1f, 1f - shiftTimer / Mathf.Max(.01f, profile.shiftDuration));
        }
        private void UpdateEngine()
        {
            if (profile == null || rpmSources.Count < 4) return; float rpm = Mathf.Clamp01(telemetry.NormalizedRpm);
            float[] weights = RpmWeights(rpm); float pitch = Mathf.Lerp(profile.minPitch, profile.maxPitch, rpm);
            float weightTotal = 0f; for (int i = 0; i < weights.Length; i++) weightTotal += weights[i];
            float normalization = weightTotal > 1f ? 1f / weightTotal : 1f;
            for (int i = 0; i < 4; i++) { rpmSources[i].pitch = pitch; rpmSources[i].volume = Mathf.MoveTowards(rpmSources[i].volume, weights[i] * normalization * profile.engineVolume * .62f, Time.deltaTime * 3.5f); }
            float load = Mathf.Clamp01(telemetry.Throttle) * profile.loadVolume * .45f;
            SetLayer(loadSource, pitch, load); SetLayer(offLoadSource, pitch * .92f, (1f - telemetry.Throttle) * Mathf.Clamp01(telemetry.SpeedKmh / 25f) * profile.loadVolume * .35f);
        }
        public static float[] RpmWeights(float rpm)
        {
            rpm = Mathf.Clamp01(rpm);
            return new[] { Mathf.Clamp01(1f - rpm / .28f), Tri(rpm, .05f, .38f, .66f), Tri(rpm, .3f, .64f, .92f), Mathf.Clamp01((rpm - .67f) / .25f) };
        }
        private static float Tri(float value, float start, float peak, float end) => value <= peak ? Mathf.InverseLerp(start, peak, value) : 1f - Mathf.InverseLerp(peak, end, value);
        private static void SetLayer(AudioSource source, float pitch, float volume) { if (source == null) return; source.pitch = pitch; source.volume = Mathf.MoveTowards(source.volume, volume, Time.deltaTime * 2.5f); }
        private static void CheckLayer(string name, AudioSource source, ref string label, ref float loudest)
        {
            if (source == null || !source.isPlaying || source.volume <= loudest) return;
            loudest = source.volume;
            label = name + ": " + (source.clip != null ? source.clip.name : "no clip");
        }

        private void UpdateTires()
        {
            SurfaceAudioProfile surface = GetSurface(telemetry.CurrentSurface); if (surface == null) return;
            SwapLoop(rollSource, surface.tireRoll); SwapLoop(skidSource, surface.tireSkid);
            float speed = Mathf.Clamp01(telemetry.SpeedKmh / 100f); float slip = Mathf.Max(Mathf.Abs(telemetry.ForwardSlip), Mathf.Abs(telemetry.SidewaysSlip));
            float skid = Mathf.InverseLerp(surface.skidThreshold, surface.skidThreshold + .65f, slip);
            float rollTarget = enableTireRoll && telemetry.IsGrounded ? speed * (1f - skid) * surface.rollVolume : 0f;
            rollSource.pitch = Mathf.Lerp(.75f, 1.25f, speed) * surface.pitchMultiplier; rollSource.volume = Mathf.MoveTowards(rollSource.volume, rollTarget, Time.deltaTime * 2f);
            float skidTarget = enableTireSkid && telemetry.IsGrounded ? skid * speed * surface.skidVolume * .32f : 0f;
            skidSource.pitch = Mathf.Lerp(.85f, 1.08f, speed) * surface.pitchMultiplier; skidSource.volume = Mathf.MoveTowards(skidSource.volume, skidTarget, Time.deltaTime * 4f);
        }
        private void UpdateLanding()
        {
            if (!telemetry.IsGrounded)
            {
                if (wasGrounded) airborneSince = Time.unscaledTime;
                airborneDownVelocity = Mathf.Max(airborneDownVelocity, -body.linearVelocity.y);
            }
            float airborneDuration = airborneSince >= 0f ? Time.unscaledTime - airborneSince : 0f;
            if (enableLandingOneShots && !wasGrounded && telemetry.IsGrounded && airborneDuration >= .22f && airborneDownVelocity >= landingVelocity && catalog?.landing != null)
                PlayVehicleOneShot(catalog.landing, Mathf.InverseLerp(landingVelocity, 12f, airborneDownVelocity) * .65f);
            if (telemetry.IsGrounded) { airborneDownVelocity = 0f; airborneSince = -1f; }
            wasGrounded = telemetry.IsGrounded;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!enableCollisionOneShots) return;
            if (catalog == null || Time.unscaledTime - lastCollisionTime < collisionCooldown) return;
            if (collision.relativeVelocity.magnitude < minimumCollisionSpeed) return;
            bool hasNonGroundImpact = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up)) < .65f) { hasNonGroundImpact = true; break; }
            }
            if (!hasNonGroundImpact) return;
            float impulse = collision.impulse.magnitude / Mathf.Max(.1f, body.mass); if (impulse < 1.2f) return; lastCollisionTime = Time.unscaledTime;
            bool isHeavy = impulse >= heavyCollisionImpulse;
            AudioClip clip = isHeavy ? catalog.collisionHeavy : impulse >= mediumCollisionImpulse && catalog.collisionMedium != null ? catalog.collisionMedium : catalog.collisionLight;
            if (clip != null)
            {
                oneShotSource.pitch = Random.Range(.94f, 1.06f);
                PlayVehicleOneShot(clip, Mathf.Clamp01(.18f + impulse / heavyCollisionImpulse * .24f));
                if (isHeavy) StartCoroutine(StopOneShotAfter(clip.length * .5f));
            }
        }
        private void PlayVehicleOneShot(AudioClip clip, float volume)
        {
            if (clip == null || oneShotSource == null) return;
            oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            LastOneShotClipName = clip.name;
            OneShotPlayCount++;
            Debug.Log($"[VehicleAudio] One-shot #{OneShotPlayCount}: {clip.name} at {Mathf.Clamp01(volume):0.00} volume", this);
        }
        private IEnumerator StopOneShotAfter(float seconds)
        {
            const float fadeSeconds = .08f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds - fadeSeconds));
            float startVolume = oneShotSource != null ? oneShotSource.volume : 1f;
            for (float elapsed = 0f; oneShotSource != null && elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                oneShotSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeSeconds);
                yield return null;
            }
            if (oneShotSource != null) { oneShotSource.Stop(); oneShotSource.volume = startVolume; }
        }
        private VehicleAudioTelemetry BuildFallbackTelemetry()
        {
            float speed = body.linearVelocity.magnitude * 3.6f; float lateral = Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.right));
            bool grounded = Physics.Raycast(transform.position + Vector3.up * .2f, Vector3.down, out RaycastHit hit, 1.3f, ~0, QueryTriggerInteraction.Ignore);
            return new VehicleAudioTelemetry { SpeedKmh = speed, Throttle = Mathf.Max(0f, externalThrottle), Brake = externalBrake ? 1f : 0f, IsGrounded = grounded, SidewaysSlip = lateral / 8f, CurrentSurface = grounded ? DetectSurface(hit.collider) : SurfaceType.Asphalt };
        }
        private IVehicleAudioTelemetrySource FindTelemetrySource() { foreach (MonoBehaviour item in GetComponents<MonoBehaviour>()) if (item is IVehicleAudioTelemetrySource found) return found; return null; }
        private static VehicleAudioProfile ProfileForVehicleName(AudioCatalog audioCatalog, string vehicleName)
        {
            string normalized = vehicleName == null ? "" : vehicleName.ToLowerInvariant();
            if (normalized.Contains("speedster")) return audioCatalog.speedsterProfile;
            if (normalized.Contains("control")) return audioCatalog.controlProfile;
            return audioCatalog.balancedProfile;
        }
        private static SurfaceType DetectSurface(Collider collider) { string value = collider == null ? "" : (collider.tag + collider.name).ToLowerInvariant(); return value.Contains("sand") ? SurfaceType.Sand : value.Contains("grass") ? SurfaceType.Grass : SurfaceType.Asphalt; }
        private SurfaceAudioProfile GetSurface(SurfaceType type) { if (surfaces != null) foreach (SurfaceAudioProfile s in surfaces) if (s != null && s.surface == type) return s; return surfaces != null && surfaces.Length > 0 ? surfaces[0] : null; }
        private void BuildSources()
        {
            AudioClip fallback = catalog?.engineDrive; AudioClip idle = profile?.idle != null ? profile.idle : catalog?.engineIdle;
            rpmSources.Add(CreateLoop("Engine Idle", idle)); rpmSources.Add(CreateLoop("Engine Low", profile?.lowRpm != null ? profile.lowRpm : fallback));
            rpmSources.Add(CreateLoop("Engine Mid", profile?.midRpm != null ? profile.midRpm : fallback)); rpmSources.Add(CreateLoop("Engine High", profile?.highRpm != null ? profile.highRpm : fallback));
            loadSource = CreateLoop("Engine Load", profile?.onLoad != null ? profile.onLoad : catalog?.accelerationLoad); offLoadSource = CreateLoop("Engine Off Load", profile?.offLoad);
            SurfaceAudioProfile surface = GetSurface(SurfaceType.Asphalt); rollSource = CreateLoop("Tire Roll", surface?.tireRoll != null ? surface.tireRoll : catalog?.tireRoll); skidSource = CreateLoop("Tire Skid", surface?.tireSkid != null ? surface.tireSkid : catalog?.tireSkid); oneShotSource = CreateSource("Vehicle One Shots", false);
        }
        private void RebuildSources() { foreach (AudioSource s in rpmSources) Destroy(s); rpmSources.Clear(); if (loadSource != null) Destroy(loadSource); if (offLoadSource != null) Destroy(offLoadSource); BuildSources(); foreach (AudioSource s in rpmSources) s.Play(); loadSource?.Play(); offLoadSource?.Play(); }
        private AudioSource CreateLoop(string name, AudioClip clip) { AudioSource s = CreateSource(name, true); s.clip = clip; s.volume = 0f; return s; }
        private AudioSource CreateSource(string name, bool loop)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>(); s.loop = loop; s.playOnAwake = false; s.spatialBlend = .65f; s.minDistance = 2f; s.maxDistance = 45f;
            string group = name.Contains("Tire") ? "Tires" : name.Contains("One Shot") ? "Collision" : "Engine"; s.outputAudioMixerGroup = GameAudioManager.Instance?.GetMixerGroup(group); return s;
        }
        private static void SwapLoop(AudioSource source, AudioClip clip) { if (source == null || clip == null || source.clip == clip) return; source.clip = clip; if (!source.isPlaying) source.Play(); }
    }
}
