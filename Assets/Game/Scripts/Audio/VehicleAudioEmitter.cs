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
        [SerializeField] private float collisionCooldown = .55f;
        [SerializeField] private float minimumCollisionSpeed = 3f;
        [SerializeField] private float minimumShiftInterval = .65f;
        [SerializeField] private float gearConfirmationTime = .18f;
        [SerializeField] private float mediumCollisionImpulse = 5f;
        [SerializeField] private float heavyCollisionImpulse = 11f;
        [SerializeField] private float landingVelocity = 3f;
        [SerializeField] private bool enableCollisionOneShots = true;
        [SerializeField] private bool enableLandingOneShots = true;
        [SerializeField] private bool enableEngineStartOneShot;
        [SerializeField] private bool enableTireRoll = true;
        [SerializeField] private bool enableTireSkid = true;
        private readonly List<AudioSource> rpmSources = new();
        private AudioSource loadSource, offLoadSource, rollSource, skidSource, brakeSkidSource, oneShotSource;
        private AudioLowPassFilter lowPassFilter;
        private Rigidbody body;
        private IVehicleAudioTelemetrySource telemetrySource;
        private VehicleAudioTelemetry telemetry;
        private int previousGear = 1, pendingGear = 1;
        private float shiftTimer, lastCollisionTime, lastShiftTime = -10f, pendingGearSince, airborneSince = -1f;
        private float lastRespawnTime = -10f, lastBackfireTime = -10f, previousThrottle;
        private Vector3 previousPosition;
        private float previousUpDot = 1f;
        private bool positionInitialized;
        private bool wasGrounded = true;
        private bool landingBaselineEstablished;
        private float initialGroundedSince = -1f;
        private bool engineMuted, tiresMuted, oneShotsMuted;
        private float airborneDownVelocity;
        private float externalThrottle = -1f;
        private bool externalBrake;

        public VehicleAudioTelemetry CurrentTelemetry => telemetry;
        public VehicleAudioProfile Profile => profile;
        public string LastOneShotClipName { get; private set; } = "None";
        public int OneShotPlayCount { get; private set; }
        public int LandingPlayCount { get; private set; }
        public float CurrentBrakeSkid { get; private set; }
        public int EngineLoopCount => rpmSources.Count + (loadSource != null ? 1 : 0) + (offLoadSource != null ? 1 : 0);
        public int TireLoopCount => (rollSource != null ? 1 : 0) + (skidSource != null ? 1 : 0) + (brakeSkidSource != null ? 1 : 0);
        public float LoudestEngineVolume
        {
            get
            {
                float value = Mathf.Max(loadSource != null ? loadSource.volume : 0f, offLoadSource != null ? offLoadSource.volume : 0f);
                foreach (AudioSource source in rpmSources) if (source != null) value = Mathf.Max(value, source.volume);
                return value;
            }
        }
        public string LoudestContinuousLayer
        {
            get
            {
                string label = "None"; float loudest = .005f;
                CheckLayer("Engine load", loadSource, ref label, ref loudest);
                CheckLayer("Engine off-load", offLoadSource, ref label, ref loudest);
                CheckLayer("Tire roll", rollSource, ref label, ref loudest);
                CheckLayer("Tire skid", skidSource, ref label, ref loudest);
                CheckLayer("Brake skid", brakeSkidSource, ref label, ref loudest);
                for (int i = 0; i < rpmSources.Count; i++) CheckLayer("Engine RPM " + i, rpmSources[i], ref label, ref loudest);
                return label + " (" + loudest.ToString("0.00") + ")";
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); telemetrySource = FindTelemetrySource();
            if (catalog == null) catalog = GameAudioManager.Instance != null ? GameAudioManager.Instance.Catalog : Resources.Load<AudioCatalog>("AudioCatalog");
            if (profile == null && catalog != null) profile = ProfileForVehicleName(catalog, gameObject.name);
            if ((surfaces == null || surfaces.Length == 0) && catalog != null) surfaces = new[] { catalog.asphaltSurface, catalog.sandSurface, catalog.grassSurface };
            BuildSources();
            lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (lowPassFilter == null) lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        }
        private void Start()
        {
            if (IsMenuScene(gameObject.scene.name))
            {
                enabled = false;
                return;
            }

            AudioClip start = profile != null ? profile.engineStart : catalog?.engineStart;
            if (enableEngineStartOneShot && start != null) PlayVehicleOneShot(start, .72f);
            foreach (AudioSource source in rpmSources) source.Play();
            loadSource?.Play(); offLoadSource?.Play(); rollSource?.Play(); skidSource?.Play(); brakeSkidSource?.Play();
        }

        private static bool IsMenuScene(string sceneName)
        {
            string value = sceneName == null ? "" : sceneName.ToLowerInvariant();
            return value.Contains("menu") || value.Contains("garage") || value.Contains("selection") || value.Contains("lobby");
        }
        private void Update()
        {
            HandleDiagnosticKeys();
            telemetry = telemetrySource != null ? telemetrySource.AudioTelemetry : BuildFallbackTelemetry();
            if (externalThrottle >= 0f) telemetry.Throttle = externalThrottle; if (externalBrake) telemetry.Brake = 1f;
            RefineSurfaceTelemetry();
            UpdateGearAndRpm(); UpdateEngine(); UpdateTires(); UpdateLanding(); UpdateRespawn(); UpdateBackfire();
            if (lowPassFilter != null) lowPassFilter.cutoffFrequency = GameAudioManager.Instance?.CurrentSnapshot == AudioSnapshotId.Paused ? 5500f : 22000f;
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
            if (brakeSkidSource != null) brakeSkidSource.mute = tiresMuted;
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
                AudioClip shift = Choose(profile.gearShiftVariants, profile.gearShift);
                if (shift != null && (catalog == null || shift != catalog.restart)) PlayVehicleOneShot(shift, .38f);
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
            for (int i = 0; i < 4; i++) { rpmSources[i].pitch = pitch; rpmSources[i].volume = Mathf.MoveTowards(rpmSources[i].volume, weights[i] * normalization * profile.engineVolume * .82f, Time.deltaTime * 3.5f); }
            float load = Mathf.Clamp01(telemetry.Throttle) * profile.loadVolume * .68f;
            SetLayer(loadSource, pitch, load); SetLayer(offLoadSource, pitch * .92f, (1f - telemetry.Throttle) * Mathf.Clamp01(telemetry.SpeedKmh / 25f) * profile.loadVolume * .48f);
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
            SwapLoop(rollSource, surface.tireRoll); SwapLoop(skidSource, surface.tireSkid); SwapLoop(brakeSkidSource, surface.tireSkid);
            float speed = Mathf.Clamp01(telemetry.SpeedKmh / 100f); float slip = Mathf.Max(Mathf.Abs(telemetry.ForwardSlip), Mathf.Abs(telemetry.SidewaysSlip));
            float slipSkid = Mathf.InverseLerp(surface.skidThreshold, surface.skidThreshold + .65f, slip);
            CurrentBrakeSkid = BrakeSkidAmount(telemetry.Brake, telemetry.SpeedKmh);
            float skid = Mathf.Max(slipSkid, CurrentBrakeSkid);
            float rollTarget = enableTireRoll && telemetry.IsGrounded ? speed * (1f - skid) * surface.rollVolume : 0f;
            rollSource.pitch = Mathf.Lerp(.75f, 1.25f, speed) * surface.pitchMultiplier; rollSource.volume = Mathf.MoveTowards(rollSource.volume, rollTarget, Time.deltaTime * 2f);
            // Natural corner/wheel slip and deliberate braking use separate sources. This
            // keeps Space braking recognizable without raising normal tire roll/skid.
            float skidTarget = enableTireSkid && telemetry.IsGrounded ? SkidTargetVolume(slipSkid, 0f, speed, surface.skidVolume) * (1f - CurrentBrakeSkid * .85f) : 0f;
            float brakeTarget = enableTireSkid && telemetry.IsGrounded ? BrakeSkidTargetVolume(CurrentBrakeSkid, surface.skidVolume) : 0f;
            skidSource.pitch = Mathf.Lerp(.85f, 1.08f, speed) * surface.pitchMultiplier; skidSource.volume = Mathf.MoveTowards(skidSource.volume, skidTarget, Time.deltaTime * 4f);
            brakeSkidSource.pitch = Mathf.Lerp(.9f, 1.02f, speed) * surface.pitchMultiplier;
            brakeSkidSource.volume = Mathf.MoveTowards(brakeSkidSource.volume, brakeTarget, Time.deltaTime * (brakeTarget > brakeSkidSource.volume ? 7.5f : 4f));
        }
        public static float BrakeSkidAmount(float brake, float speedKmh)
            => Mathf.Clamp01(brake) * Mathf.InverseLerp(8f, 45f, Mathf.Max(0f, speedKmh)) * .95f;
        public static float SkidTargetVolume(float slipSkid, float brakeSkid, float normalizedSpeed, float surfaceVolume)
            => Mathf.Max(Mathf.Clamp01(slipSkid) * Mathf.Clamp01(normalizedSpeed), Mathf.Clamp01(brakeSkid)) * Mathf.Clamp01(surfaceVolume) * .8f;
        public static float BrakeSkidTargetVolume(float brakeSkid, float surfaceVolume)
            => Mathf.Clamp01(Mathf.Clamp01(brakeSkid) * Mathf.Clamp01(surfaceVolume) * 1.15f);
        private void UpdateLanding()
        {
            // A spawned vehicle can begin slightly above the road while physics settles.
            // Establish a stable grounded baseline first so that initial placement is not
            // presented as a real jump landing.
            if (!landingBaselineEstablished)
            {
                if (telemetry.IsGrounded)
                {
                    if (initialGroundedSince < 0f) initialGroundedSince = Time.unscaledTime;
                    if (Time.unscaledTime - initialGroundedSince >= .25f) landingBaselineEstablished = true;
                }
                else initialGroundedSince = -1f;
                airborneSince = -1f;
                airborneDownVelocity = 0f;
                wasGrounded = telemetry.IsGrounded;
                return;
            }
            if (!telemetry.IsGrounded)
            {
                if (wasGrounded) airborneSince = Time.unscaledTime;
                airborneDownVelocity = Mathf.Max(airborneDownVelocity, -body.linearVelocity.y);
            }
            float airborneDuration = airborneSince >= 0f ? Time.unscaledTime - airborneSince : 0f;
            if (enableLandingOneShots && !wasGrounded && telemetry.IsGrounded && airborneDuration >= .22f && airborneDownVelocity >= landingVelocity && catalog?.landing != null)
            {
                LandingPlayCount++;
                PlayVehicleOneShot(catalog.landing, .12f + Mathf.InverseLerp(landingVelocity, 12f, airborneDownVelocity) * .62f);
            }
            if (telemetry.IsGrounded) { airborneDownVelocity = 0f; airborneSince = -1f; }
            wasGrounded = telemetry.IsGrounded;
        }
        private void UpdateRespawn()
        {
            Vector3 position = transform.position;
            float upDot = Vector3.Dot(transform.up, Vector3.up);
            if (positionInitialized && Time.unscaledTime - lastRespawnTime > 1.25f)
            {
                float jump = Vector3.Distance(position, previousPosition);
                bool teleported = jump > 6f && body.linearVelocity.magnitude < 5f;
                bool recoveredFlip = previousUpDot < .25f && upDot > .72f && jump > 1f;
                if ((teleported || recoveredFlip) && catalog?.respawn != null)
                {
                    lastRespawnTime = Time.unscaledTime;
                    PlayVehicleOneShot(catalog.respawn, .68f);
                }
            }
            previousPosition = position; previousUpDot = upDot; positionInitialized = true;
        }
        private void UpdateBackfire()
        {
            if (profile == null) return;
            float drop = previousThrottle - telemetry.Throttle;
            if (drop >= profile.backfireThrottleDrop && telemetry.NormalizedRpm >= profile.backfireMinimumRpm && Time.unscaledTime - lastBackfireTime > .8f)
            {
                AudioClip clip = Choose(profile.backfireVariants, profile.backfire);
                if (clip != null) { lastBackfireTime = Time.unscaledTime; PlayVehicleOneShot(clip, .4f); }
            }
            previousThrottle = telemetry.Throttle;
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
            bool isMedium = !isHeavy && impulse >= mediumCollisionImpulse;
            AudioClip clip = isHeavy ? Choose(catalog.collisionHeavyVariants, catalog.collisionHeavy) : isMedium ? Choose(catalog.collisionMediumVariants, catalog.collisionMedium) : Choose(catalog.collisionLightVariants, catalog.collisionLight);
            if (clip != null)
            {
                oneShotSource.pitch = Random.Range(.94f, 1.06f);
                // Collision recordings have much lower average loudness than the engine loops.
                // Keep impact dynamics, but never let a valid hit disappear under the engine/music bed.
                PlayVehicleOneShot(clip, Mathf.Clamp(.72f + impulse / heavyCollisionImpulse * .2f, .72f, .92f));
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
        private void RefineSurfaceTelemetry()
        {
            if (!telemetry.IsGrounded) return;
            if (Physics.Raycast(transform.position + Vector3.up * .35f, Vector3.down, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore))
                telemetry.CurrentSurface = SurfaceAudioResolver.Resolve(hit.collider, telemetry.CurrentSurface);
        }
        private VehicleAudioTelemetry BuildFallbackTelemetry()
        {
            float speed = body.linearVelocity.magnitude * 3.6f; float lateral = Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.right));
            bool grounded = Physics.Raycast(transform.position + Vector3.up * .2f, Vector3.down, out RaycastHit hit, 1.3f, ~0, QueryTriggerInteraction.Ignore);
            float throttle = externalThrottle >= 0f ? externalThrottle : ReadFallbackThrottle();
            float brake = externalBrake || ReadFallbackBrake() ? 1f : 0f;
            return new VehicleAudioTelemetry { SpeedKmh = speed, Throttle = throttle, Brake = brake, IsGrounded = grounded, SidewaysSlip = lateral / 8f, CurrentSurface = grounded ? DetectSurface(hit.collider) : SurfaceType.Asphalt };
        }
        public static float ReadFallbackThrottle()
        {
            float value = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)) value = 1f;
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null) value = Mathf.Max(value, gamepad.rightTrigger.ReadValue());
            return value;
        }
        public static bool ReadFallbackBrake()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed)) return true;
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftTrigger.ReadValue() > .1f;
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
        private static AudioClip Choose(AudioClip[] variants, AudioClip fallback)
        {
            if (variants == null || variants.Length == 0) return fallback;
            for (int tries = 0; tries < variants.Length; tries++)
            {
                AudioClip clip = variants[Random.Range(0, variants.Length)];
                if (clip != null) return clip;
            }
            return fallback;
        }
        private SurfaceAudioProfile GetSurface(SurfaceType type) { if (surfaces != null) foreach (SurfaceAudioProfile s in surfaces) if (s != null && s.surface == type) return s; return surfaces != null && surfaces.Length > 0 ? surfaces[0] : null; }
        private void BuildSources()
        {
            AudioClip fallback = catalog?.engineDrive; AudioClip idle = profile?.idle != null ? profile.idle : catalog?.engineIdle;
            rpmSources.Add(CreateLoop("Engine Idle", idle)); rpmSources.Add(CreateLoop("Engine Low", profile?.lowRpm != null ? profile.lowRpm : fallback));
            rpmSources.Add(CreateLoop("Engine Mid", profile?.midRpm != null ? profile.midRpm : fallback)); rpmSources.Add(CreateLoop("Engine High", profile?.highRpm != null ? profile.highRpm : fallback));
            loadSource = CreateLoop("Engine Load", profile?.onLoad != null ? profile.onLoad : catalog?.accelerationLoad); offLoadSource = CreateLoop("Engine Off Load", profile?.offLoad);
            SurfaceAudioProfile surface = GetSurface(SurfaceType.Asphalt); rollSource = CreateLoop("Tire Roll", surface?.tireRoll != null ? surface.tireRoll : catalog?.tireRoll); skidSource = CreateLoop("Tire Skid", surface?.tireSkid != null ? surface.tireSkid : catalog?.tireSkid); brakeSkidSource = CreateLoop("Tire Brake Skid", surface?.tireSkid != null ? surface.tireSkid : catalog?.tireSkid); oneShotSource = CreateSource("Vehicle One Shots", false);
            rollSource.spatialBlend = .15f;
            skidSource.spatialBlend = .05f;
            brakeSkidSource.spatialBlend = .02f;
            // Impact/shift transients must stay present with a third-person camera.
            // A small spatial blend preserves direction without losing most of the transient to distance rolloff.
            oneShotSource.spatialBlend = .05f;
        }
        private void RebuildSources()
        {
            foreach (AudioSource source in rpmSources) DestroySource(source);
            rpmSources.Clear();
            DestroySource(loadSource); DestroySource(offLoadSource); DestroySource(rollSource); DestroySource(skidSource); DestroySource(brakeSkidSource); DestroySource(oneShotSource);
            loadSource = offLoadSource = rollSource = skidSource = brakeSkidSource = oneShotSource = null;
            BuildSources();
            foreach (AudioSource source in rpmSources) source.Play();
            loadSource?.Play(); offLoadSource?.Play(); rollSource?.Play(); skidSource?.Play(); brakeSkidSource?.Play();
        }
        private static void DestroySource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            Destroy(source);
        }
        private AudioSource CreateLoop(string name, AudioClip clip) { AudioSource s = CreateSource(name, true); s.clip = clip; s.volume = 0f; return s; }
        private AudioSource CreateSource(string name, bool loop)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>(); s.loop = loop; s.playOnAwake = false; s.spatialBlend = .3f; s.minDistance = 4f; s.maxDistance = 70f;
            string group = name.Contains("Tire") ? "Tires" : name.Contains("One Shot") ? "Collision" : "Engine"; s.outputAudioMixerGroup = GameAudioManager.Instance?.GetMixerGroup(group); return s;
        }
        private static void SwapLoop(AudioSource source, AudioClip clip) { if (source == null || clip == null || source.clip == clip) return; source.clip = clip; if (!source.isPlaying) source.Play(); }
    }
}
