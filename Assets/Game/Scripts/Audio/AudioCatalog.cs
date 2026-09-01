using UnityEngine;

namespace SuperRacing.Audio
{
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Super Racing/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Header("Vehicle")]
        public AudioClip engineStart;
        public AudioClip engineIdle;
        public AudioClip engineDrive;
        public AudioClip accelerationLoad;
        public AudioClip tireRoll;
        public AudioClip tireSkid;
        public AudioClip collisionLight;
        public AudioClip collisionMedium;
        public AudioClip collisionHeavy;
        public AudioClip gearShift;
        public AudioClip engineOffLoad;
        public AudioClip respawn;
        public AudioClip landing;

        [Header("Race")]
        public AudioClip countdownTick;
        public AudioClip startedGo;
        public AudioClip checkpointPassed;
        public AudioClip lapChanged;
        public AudioClip finished;
        public AudioClip newRecord;
        public AudioClip invalidCheckpoint;
        public AudioClip restart;

        [Header("UI")]
        public AudioClip uiHover;
        public AudioClip uiClick;
        public AudioClip uiConfirm;
        public AudioClip uiBack;
        public AudioClip uiSelectionChanged;
        public AudioClip uiError;
        public AudioClip uiStartRace;
        public AudioClip uiResultsOpen;

        [Header("Ambience and Music")]
        public AudioClip beachWaves;
        public AudioClip beachWind;
        public AudioClip desertWind;
        public AudioClip desertSandGust;
        public AudioClip raceMusic;
        public AudioClip menuMusic;
        public AudioClip resultMusic;

        [Header("Profiles")]
        public VehicleAudioProfile speedsterProfile;
        public VehicleAudioProfile balancedProfile;
        public VehicleAudioProfile controlProfile;
        public SurfaceAudioProfile asphaltSurface;
        public SurfaceAudioProfile sandSurface;
        public SurfaceAudioProfile grassSurface;

        public AudioClip GetCue(AudioCueId cue)
        {
            return cue switch
            {
                AudioCueId.CountdownTick => countdownTick, AudioCueId.StartedGo => startedGo,
                AudioCueId.CheckpointPassed => checkpointPassed, AudioCueId.LapChanged => lapChanged,
                AudioCueId.Finished => finished, AudioCueId.NewRecord => newRecord,
                AudioCueId.InvalidCheckpoint => invalidCheckpoint, AudioCueId.Restart => restart,
                AudioCueId.Respawn => respawn, AudioCueId.Landing => landing,
                AudioCueId.CollisionLight => collisionLight, AudioCueId.CollisionHeavy => collisionHeavy,
                AudioCueId.UIHover => uiHover, AudioCueId.UIClick => uiClick,
                AudioCueId.UIConfirm => uiConfirm, AudioCueId.UIBack => uiBack,
                AudioCueId.UISelectionChanged => uiSelectionChanged, AudioCueId.UIError => uiError,
                AudioCueId.UIStartRace => uiStartRace, AudioCueId.UIResultsOpen => uiResultsOpen,
                _ => null
            };
        }
    }
}
