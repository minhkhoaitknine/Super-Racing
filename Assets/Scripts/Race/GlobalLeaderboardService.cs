using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace SuperRacing.Race
{
    [Serializable]
    public sealed class LeaderboardMap
    {
        public string trackId;
        public string leaderboardId;
    }

    [Serializable]
    public sealed class GlobalLeaderboardSettings
    {
        public string environment = "production";
        public List<LeaderboardMap> maps = new();

        public string BoardFor(string trackId) => maps.Find(m => m.trackId == trackId)?.leaderboardId;
    }

    [Serializable]
    public sealed class PendingRaceScore
    {
        public string board;
        public long milliseconds;
    }

    [Serializable]
    public sealed class PendingRaceScores
    {
        public List<PendingRaceScore> scores = new();

        public void KeepBest(string board, long milliseconds)
        {
            if (string.IsNullOrWhiteSpace(board) || milliseconds <= 0) return;
            PendingRaceScore existing = scores.Find(s => s.board == board);
            if (existing == null) scores.Add(new PendingRaceScore { board = board, milliseconds = milliseconds });
            else existing.milliseconds = Math.Min(existing.milliseconds, milliseconds);
        }
    }

    /// <summary>Anonymous identity and durable, best-only race uploads. Never blocks gameplay.</summary>
    public sealed class GlobalLeaderboardService : MonoBehaviour
    {
        public static GlobalLeaderboardService Instance { get; private set; }
        public GlobalLeaderboardSettings Settings { get; private set; }
        public string Status { get; private set; } = "Connecting...";
        public string PlayerId => UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
        public bool HasPendingScores => pending.scores.Count > 0;
        private PendingRaceScores pending = new();
        private Task signIn;
        private bool uploading;
        private float nextRetry;
        private string queueKey;
        private readonly SemaphoreSlim nameLock = new(1, 1);
        public string Username { get; private set; } = "PLAYER";
        public event Action UsernameChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Instance == null) new GameObject("Global Leaderboards").AddComponent<GlobalLeaderboardService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TextAsset config = Resources.Load<TextAsset>("GlobalLeaderboards");
            Settings = config != null ? JsonUtility.FromJson<GlobalLeaderboardSettings>(config.text) : new GlobalLeaderboardSettings();
            queueKey = $"SuperRacing.GlobalScores.v1.{Application.cloudProjectId}.{Settings.environment}";
            Username = PlayerPrefs.GetString(queueKey + ".Name", "PLAYER");
            try { pending = JsonUtility.FromJson<PendingRaceScores>(PlayerPrefs.GetString(queueKey, "")) ?? new PendingRaceScores(); }
            catch (ArgumentException) { pending = new PendingRaceScores(); }
            pending.scores ??= new List<PendingRaceScore>();
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextRetry || uploading) return;
            nextRetry = Time.realtimeSinceStartup + 30f;
            if (HasPendingScores || string.IsNullOrEmpty(PlayerId)) _ = FlushAsync();
        }

        public static long ToMilliseconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f || seconds > 86400f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            return Math.Max(1L, (long)Math.Round((double)seconds * 1000d, MidpointRounding.AwayFromZero));
        }

        public void SubmitRace(string trackId, float seconds)
        {
            string board = Settings.BoardFor(trackId);
            if (string.IsNullOrEmpty(board)) return; // Prototype/test maps are deliberately excluded.
            long milliseconds;
            try { milliseconds = ToMilliseconds(seconds); }
            catch (ArgumentOutOfRangeException) { Status = "Invalid race time; score was not uploaded."; return; }
            pending.KeepBest(board, milliseconds);
            SaveQueue();
            Status = "Saving global record...";
            _ = FlushAsync();
        }

        public Task EnsureSignedInAsync()
        {
            if (!string.IsNullOrEmpty(PlayerId)) return Task.CompletedTask;
            if (signIn == null || signIn.IsCompleted) signIn = SignInAsync();
            return signIn;
        }

        public async Task LoadUsernameAsync()
        {
            await EnsureSignedInAsync();
            await nameLock.WaitAsync();
            try { CacheUsername(await AuthenticationService.Instance.GetPlayerNameAsync()); }
            finally { nameLock.Release(); }
        }

        public async Task ChangeUsernameAsync(string name)
        {
            string error = ValidateUsername(name);
            if (error != null) throw new ArgumentException(error);
            await EnsureSignedInAsync();
            await nameLock.WaitAsync();
            try { CacheUsername(await AuthenticationService.Instance.UpdatePlayerNameAsync(name.Trim())); }
            finally { nameLock.Release(); }
        }

        public static string ValidateUsername(string name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name)) return "Enter a username.";
            if (name.Length > 24) return "Use 24 characters or fewer.";
            foreach (char c in name)
                if (char.IsWhiteSpace(c) || char.IsControl(c) || c == '#' || c == '<' || c == '>')
                    return "No spaces or # < > characters.";
            return null;
        }

        public static string WithoutNameTag(string name)
        {
            if (string.IsNullOrEmpty(name)) return "PLAYER";
            int hash = name.LastIndexOf('#');
            if (hash > 0 && hash < name.Length - 1 && int.TryParse(name.Substring(hash + 1), out _))
                return name.Substring(0, hash);
            return name;
        }

        private void CacheUsername(string name)
        {
            Username = WithoutNameTag(name);
            PlayerPrefs.SetString(queueKey + ".Name", Username);
            PlayerPrefs.Save();
            UsernameChanged?.Invoke();
        }

        private async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(Settings.environment));
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Status = HasPendingScores ? "Global record waiting to sync." : "Connected";
        }

        public async Task FlushAsync()
        {
            if (uploading) return;
            uploading = true;
            try
            {
                await EnsureSignedInAsync();
                foreach (PendingRaceScore item in new List<PendingRaceScore>(pending.scores))
                {
                    long submitted = item.milliseconds;
                    try
                    {
                        await LeaderboardsService.Instance.AddPlayerScoreAsync(item.board, submitted);
                        // A faster race may have been queued while the request was in flight.
                        if (item.milliseconds == submitted) pending.scores.Remove(item);
                        SaveQueue();
                    }
                    catch (Exception ex) { Status = FriendlyError(ex, true); }
                }
                if (!HasPendingScores) Status = "Global records synced";
            }
            catch (Exception ex) { Status = FriendlyError(ex, HasPendingScores); }
            finally { uploading = false; nextRetry = Time.realtimeSinceStartup + 30f; }
        }

        public async Task<LeaderboardScoresPage> GetTopAsync(string trackId)
        {
            await EnsureSignedInAsync();
            string board = Settings.BoardFor(trackId);
            if (string.IsNullOrEmpty(board)) throw new InvalidOperationException("No leaderboard configured for this track.");
            return await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Limit = 100 });
        }

        public async Task<LeaderboardEntry> GetOwnAsync(string trackId)
        {
            await EnsureSignedInAsync();
            try { return await LeaderboardsService.Instance.GetPlayerScoreAsync(Settings.BoardFor(trackId)); }
            catch (LeaderboardsException ex) when (ex.Reason == LeaderboardsExceptionReason.EntryNotFound) { return null; }
        }

        public static string DisplayName(LeaderboardEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.PlayerName)) return WithoutNameTag(entry.PlayerName);
            string id = entry.PlayerId ?? "unknown";
            return "Racer " + id.Substring(0, Math.Min(8, id.Length));
        }

        public static string FriendlyError(Exception ex, bool queued = false)
        {
            string suffix = queued ? " Record saved on this device; will retry." : " Please retry.";
            if (ex is LeaderboardsException lb && lb.Reason == LeaderboardsExceptionReason.LeaderboardNotFound)
                return "This leaderboard is not available yet." + suffix;
            return "Could not connect to global leaderboards." + suffix;
        }

        private void SaveQueue()
        {
            PlayerPrefs.SetString(queueKey, JsonUtility.ToJson(pending));
            PlayerPrefs.Save();
        }
    }
}
