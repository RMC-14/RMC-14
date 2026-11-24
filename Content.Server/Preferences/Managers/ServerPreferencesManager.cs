using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.Preferences;
using Content.Shared.CCVar;
using Content.Shared.Construction.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Preferences.Managers
{
    /// <summary>
    /// Sends <see cref="MsgPreferencesAndSettings"/> before the client joins the lobby.
    /// Receives <see cref="MsgSelectCharacter"/> and <see cref="MsgUpdateCharacter"/> at any time.
    /// </summary>
    public sealed class ServerPreferencesManager : IServerPreferencesManager, IPostInjectInit
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDependencyCollection _dependencies = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly UserDbDataManager _userDb = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        // Cache player prefs on the server so we don't need as much async hell related to them.
        private readonly Dictionary<NetUserId, PlayerPrefData> _cachedPlayerPrefs =
            new();

        // RMC14
        private readonly ConcurrentDictionary<NetUserId, SemaphoreSlim> _prefsMutationLocks = new();
        // RMC14

        private ISawmill _sawmill = default!;

        private int MaxCharacterSlots => _cfg.GetCVar(CCVars.GameMaxCharacterSlots);

        public void Init()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>();
            _netManager.RegisterNetMessage<MsgSelectCharacter>(HandleSelectCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>(HandleUpdateCharacterMessage);
            _netManager.RegisterNetMessage<MsgDeleteCharacter>(HandleDeleteCharacterMessage);
            _netManager.RegisterNetMessage<MsgUpdateConstructionFavorites>(HandleUpdateConstructionFavoritesMessage);
            // RMC14
            _netManager.RegisterNetMessage<MsgSetCharacterEnable>(HandleSetCharacterEnableMessage);
            _netManager.RegisterNetMessage<MsgUpdateJobPriorities>(HandleUpdateJobPrioritiesMessage);
            _netManager.RegisterNetMessage<MsgUpdatePlayerSettings>(HandleUpdatePlayerSettingsMessage);
            _netManager.RegisterNetMessage<MsgPreferencesSync>();

            _sawmill = _log.GetSawmill("prefs");
        }

        // RMC14 begin
        private async Task WithPrefsMutationLock(NetUserId userId, Func<Task> action)
        {
            var semaphore = _prefsMutationLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool TryGetLoadedPrefs(NetUserId userId, [NotNullWhen(true)] out PlayerPrefData? prefsData)
        {
            if (_cachedPlayerPrefs.TryGetValue(userId, out prefsData) && prefsData.PrefsLoaded)
                return true;

            _sawmill.Warning($"User {userId} tried to modify preferences before they loaded.");
            prefsData = null;
            return false;
        }

        private void SetAndSync(NetUserId userId, PlayerPrefData prefsData, PlayerPreferences prefs)
        {
            prefsData.Prefs = prefs;
            if (!_playerManager.TryGetSessionById(userId, out var session))
                return;

            _netManager.ServerSendMessage(new MsgPreferencesSync { Preferences = prefs }, session.Channel);
        }

        private bool ShouldStorePrefs(NetUserId userId)
        {
            return _playerManager.TryGetSessionById(userId, out var session) &&
                   ShouldStorePrefs(session.Channel.AuthType);
        }
        // RMC14 end

        private async void HandleSelectCharacterMessage(MsgSelectCharacter message)
        {
            var userId = message.MsgChannel.UserId;
            // RMC14 begin
            await WithPrefsMutationLock(userId, async () =>
            {
                var index = message.SelectedCharacterIndex;
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                if (index < 0 || index >= MaxCharacterSlots)
                    return;

                var curPrefs = prefsData.Prefs!;
                if (!curPrefs.Characters.ContainsKey(index))
                    return;

                SetAndSync(userId, prefsData, curPrefs with { SelectedCharacterIndex = index });

                if (ShouldStorePrefs(message.MsgChannel.AuthType))
                    await _db.SaveSelectedCharacterIndexAsync(userId, index);
            });
            // RMC14 end
        }

        private async void HandleUpdateCharacterMessage(MsgUpdateCharacter message)
        {
            var userId = message.MsgChannel.UserId;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (message.Profile == null)
                _sawmill.Error($"User {userId} sent a {nameof(MsgUpdateCharacter)} with a null profile in slot {message.Slot}.");
            else
                await SetProfile(userId, message.Slot, message.Profile);
        }

        public Task SetProfile(NetUserId userId, int slot, ICharacterProfile profile)
        {
            // RMC14 begin
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                if (slot < 0 || slot >= MaxCharacterSlots)
                    return;

                var curPrefs = prefsData.Prefs!;
                var session = _playerManager.GetSessionById(userId);

                profile.EnsureValid(session, _dependencies);

                if (profile is HumanoidCharacterProfile humanoid &&
                    curPrefs.TryGetHumanoidInSlot(slot, out var existing))
                {
                    profile = humanoid.WithEnabled(existing.Enabled);
                }

                var profiles = new Dictionary<int, ICharacterProfile>(curPrefs.Characters)
                {
                    [slot] = profile
                };

                SetAndSync(userId, prefsData, curPrefs with { Characters = profiles });

                if (ShouldStorePrefs(session.Channel.AuthType))
                    await _db.SaveCharacterSlotAsync(userId, profile, slot);
            });
            // RMC14 ens
        }

        public Task SetConstructionFavorites(NetUserId userId, List<ProtoId<ConstructionPrototype>> favorites)
        {
            // RMC14
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                SetAndSync(userId, prefsData, prefsData.Prefs! with { ConstructionFavorites = favorites });

                if (ShouldStorePrefs(userId))
                    await _db.SaveConstructionFavoritesAsync(userId, favorites);
            });
        }

        // RMC14 begin
        public Task SetJobPriorities(NetUserId userId, IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                var newPrefs = prefsData.Prefs! with { JobPriorities = SanitizeJobPriorities(jobPriorities) };
                SetAndSync(userId, prefsData, newPrefs);

                if (ShouldStorePrefs(userId))
                    await _db.SaveJobPrioritiesAsync(userId, newPrefs.JobPriorities);
            });
        }

        public Task SetPlayerSettings(NetUserId userId, RMCPlayerSettings settings)
        {
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                var session = _playerManager.GetSessionById(userId);
                var validated = settings.Validated(session, _dependencies);
                SetAndSync(userId, prefsData, prefsData.Prefs! with { PlayerSettings = validated });

                if (ShouldStorePrefs(session.Channel.AuthType))
                    await _db.SavePlayerSettingsAsync(userId, validated);
            });
        }

        public Task SetCharacterEnabled(NetUserId userId, int slot, bool enabled)
        {
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                var curPrefs = prefsData.Prefs!;
                if (!curPrefs.TryGetHumanoidInSlot(slot, out var humanoid) || humanoid.Enabled == enabled)
                    return;

                if (!enabled && !curPrefs.Characters.Any(c => c.Key != slot && c.Value is HumanoidCharacterProfile { Enabled: true }))
                {
                    SetAndSync(userId, prefsData, curPrefs);
                    return;
                }

                var updated = humanoid.WithEnabled(enabled);
                var profiles = new Dictionary<int, ICharacterProfile>(curPrefs.Characters)
                {
                    [slot] = updated,
                };

                SetAndSync(userId, prefsData, curPrefs with { Characters = profiles });

                if (ShouldStorePrefs(userId))
                    await _db.SaveCharacterSlotAsync(userId, updated, slot);
            });
        }

        private Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(
            IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> priorities)
        {
            return PlayerPreferences.SanitizeJobPriorities(priorities.Where(p =>
                _prototypeManager.TryIndex(p.Key, out var job) && job.SetPreference));
        }

        private async void HandleSetCharacterEnableMessage(MsgSetCharacterEnable message)
        {
            await SetCharacterEnabled(message.MsgChannel.UserId, message.CharacterIndex, message.EnabledValue);
        }

        private async void HandleUpdateJobPrioritiesMessage(MsgUpdateJobPriorities message)
        {
            await SetJobPriorities(message.MsgChannel.UserId, message.JobPriorities);
        }

        private async void HandleUpdatePlayerSettingsMessage(MsgUpdatePlayerSettings message)
        {
            await SetPlayerSettings(message.MsgChannel.UserId, message.Settings);
        }
        // RMC14 end

        private async void HandleDeleteCharacterMessage(MsgDeleteCharacter message)
        {
            // RMC14
            await DeleteProfile(message.MsgChannel.UserId, message.Slot);
        }

        public Task DeleteProfile(NetUserId userId, int slot)
        {
            // RMC14
            return WithPrefsMutationLock(userId, async () =>
            {
                if (!TryGetLoadedPrefs(userId, out var prefsData))
                    return;

                if (slot < 0 || slot >= MaxCharacterSlots)
                    return;

                var curPrefs = prefsData.Prefs!;
                if (!curPrefs.Characters.ContainsKey(slot))
                    return;

                // If they try to delete the slot they have selected then we switch to another one.
                // Of course, that's only if they HAVE another slot.
                int? nextSlot = null;
                if (curPrefs.SelectedCharacterIndex == slot)
                {
                    if (curPrefs.Characters.Count <= 1)
                    {
                        // Only slot left, can't delete.
                        return;
                    }

                    nextSlot = curPrefs.Characters.First(p => p.Key != slot).Key;
                }

                var profiles = new Dictionary<int, ICharacterProfile>(curPrefs.Characters);
                profiles.Remove(slot);

                var selectedSlot = nextSlot ?? curPrefs.SelectedCharacterIndex;
                HumanoidCharacterProfile? reEnabled = null;
                if (!profiles.Values.Any(p => p is HumanoidCharacterProfile { Enabled: true }) &&
                    profiles.TryGetValue(selectedSlot, out var fallback) &&
                    fallback is HumanoidCharacterProfile fallbackHumanoid)
                {
                    reEnabled = fallbackHumanoid.WithEnabled(true);
                    profiles[selectedSlot] = reEnabled;
                }

                SetAndSync(userId, prefsData, curPrefs with
                {
                    Characters = profiles,
                    SelectedCharacterIndex = selectedSlot,
                });

                if (!ShouldStorePrefs(userId))
                    return;

                if (nextSlot != null)
                    await _db.DeleteSlotAndSetSelectedIndex(userId, slot, nextSlot.Value);
                else
                    await _db.SaveCharacterSlotAsync(userId, null, slot);

                if (reEnabled != null)
                    await _db.SaveCharacterSlotAsync(userId, reEnabled, selectedSlot);
            });
            // RMC14 end
        }

        private async void HandleUpdateConstructionFavoritesMessage(MsgUpdateConstructionFavorites message)
        {
            var userId = message.MsgChannel.UserId;

            // Validate items in the message so that a modified client cannot freely store a gigabyte of arbitrary data.
            var validatedSet = new HashSet<ProtoId<ConstructionPrototype>>();
            foreach (var favorite in message.Favorites)
            {
                if (_prototypeManager.HasIndex(favorite))
                    validatedSet.Add(favorite);
            }

            var validatedList = message.Favorites;
            if (validatedSet.Count != message.Favorites.Count)
            {
                // A difference in counts indicates that unrecognized or duplicate IDs are present.
                _sawmill.Warning($"User {userId} sent invalid construction favorites.");
                validatedList = validatedSet.ToList();
            }

            await SetConstructionFavorites(userId, validatedList); // RMC14
        }

        // Should only be called via UserDbDataManager.
        public async Task LoadData(ICommonSession session, CancellationToken cancel)
        {
            if (!ShouldStorePrefs(session.Channel.AuthType))
            {
                // Don't store data for guests.
                var prefsData = new PlayerPrefData
                {
                    PrefsLoaded = true,
                    Prefs = new PlayerPreferences(
                        new[] { new KeyValuePair<int, ICharacterProfile>(0, HumanoidCharacterProfile.Random()) },
                        0, Color.Transparent, [])
                    // RMC14
                    {
                        JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
                        {
                            { SharedGameTicker.FallbackOverflowJob, JobPriority.High },
                        },
                    },
                };

                _cachedPlayerPrefs[session.UserId] = prefsData;
            }
            else
            {
                var prefsData = new PlayerPrefData();
                var loadTask = LoadPrefs();
                _cachedPlayerPrefs[session.UserId] = prefsData;

                await loadTask;

                async Task LoadPrefs()
                {
                    var prefs = await GetOrCreatePreferencesAsync(session.UserId, cancel);
                    prefsData.Prefs = prefs;
                }
            }
        }

        public void FinishLoad(ICommonSession session)
        {
            // This is a separate step from the actual database load.
            // Sanitizing preferences requires play time info due to loadouts.
            // And play time info is loaded concurrently from the DB with preferences.
            var prefsData = _cachedPlayerPrefs[session.UserId];
            DebugTools.Assert(prefsData.Prefs != null);
            prefsData.Prefs = SanitizePreferences(session, prefsData.Prefs, _dependencies);

            prefsData.PrefsLoaded = true;

            var msg = new MsgPreferencesAndSettings();
            msg.Preferences = prefsData.Prefs;
            msg.Settings = new GameSettings
            {
                MaxCharacterSlots = MaxCharacterSlots
            };
            _netManager.ServerSendMessage(msg, session.Channel);
        }

        public void OnClientDisconnected(ICommonSession session)
        {
            _cachedPlayerPrefs.Remove(session.UserId);
            // RMC14
            if (_prefsMutationLocks.TryRemove(session.UserId, out var semaphore))
                semaphore.Dispose();
        }

        public bool HavePreferencesLoaded(ICommonSession session)
        {
            return _cachedPlayerPrefs.ContainsKey(session.UserId);
        }


        /// <summary>
        /// Tries to get the preferences from the cache
        /// </summary>
        /// <param name="userId">User Id to get preferences for</param>
        /// <param name="playerPreferences">The user preferences if true, otherwise null</param>
        /// <returns>If preferences are not null</returns>
        public bool TryGetCachedPreferences(NetUserId userId,
            [NotNullWhen(true)] out PlayerPreferences? playerPreferences)
        {
            if (_cachedPlayerPrefs.TryGetValue(userId, out var prefs))
            {
                playerPreferences = prefs.Prefs;
                return prefs.Prefs != null;
            }

            playerPreferences = null;
            return false;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage.
        /// </summary>
        public PlayerPreferences GetPreferences(NetUserId userId)
        {
            var prefs = _cachedPlayerPrefs[userId].Prefs;
            if (prefs == null)
            {
                throw new InvalidOperationException("Preferences for this player have not loaded yet.");
            }

            return prefs;
        }

        /// <summary>
        /// Retrieves preferences for the given username from storage or returns null.
        /// </summary>
        public PlayerPreferences? GetPreferencesOrNull(NetUserId? userId)
        {
            if (userId == null)
                return null;

            if (_cachedPlayerPrefs.TryGetValue(userId.Value, out var pref))
                return pref.Prefs;
            return null;
        }

        private async Task<PlayerPreferences> GetOrCreatePreferencesAsync(NetUserId userId, CancellationToken cancel)
        {
            var prefs = await _db.GetPlayerPreferencesAsync(userId, cancel);
            if (prefs is null)
            {
                return await _db.InitPrefsAsync(userId, HumanoidCharacterProfile.Random(), cancel);
            }

            return prefs;
        }

        private PlayerPreferences SanitizePreferences(ICommonSession session, PlayerPreferences prefs, IDependencyCollection collection)
        {
            // Clean up preferences in case of changes to the game,
            // such as removed jobs still being selected.

            return new PlayerPreferences(prefs.Characters.Select(p =>
            {
                return new KeyValuePair<int, ICharacterProfile>(p.Key, p.Value.Validated(session, collection));
            }), prefs.SelectedCharacterIndex, prefs.AdminOOCColor, prefs.ConstructionFavorites)
            // RMC14
            {
                JobPriorities = SanitizeJobPriorities(prefs.JobPriorities),
                PlayerSettings = prefs.PlayerSettings.Validated(session, collection),
            };
        }

        public IEnumerable<KeyValuePair<NetUserId, ICharacterProfile>> GetSelectedProfilesForPlayers(
            List<NetUserId> usernames)
        {
            return usernames
                .Select(p => (_cachedPlayerPrefs[p].Prefs, p))
                .Where(p => p.Prefs != null)
                .Select(p => new KeyValuePair<NetUserId, ICharacterProfile>(p.p, p.Prefs!.SelectedCharacter));
        }

        internal static bool ShouldStorePrefs(LoginType loginType)
        {
            return loginType.HasStaticUserId();
        }

        private sealed class PlayerPrefData
        {
            public bool PrefsLoaded;
            public PlayerPreferences? Prefs;
        }

        void IPostInjectInit.PostInject()
        {
            _userDb.AddOnLoadPlayer(LoadData);
            _userDb.AddOnFinishLoad(FinishLoad);
            _userDb.AddOnPlayerDisconnect(OnClientDisconnected);
        }
    }
}
