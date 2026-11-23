using System.Linq;
using Content.Shared._RMC14.Preferences;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby
{
    /// <summary>
    ///     Receives <see cref="PlayerPreferences" /> and <see cref="GameSettings" /> from the server during the initial
    ///     connection.
    ///     Stores preferences on the server through <see cref="SelectCharacter" /> and <see cref="UpdateCharacter" />.
    /// </summary>
    public sealed class ClientPreferencesManager : IClientPreferencesManager
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action? OnServerDataLoaded;

        // RMC14
        public event Action? OnPreferencesUpdated;

        public GameSettings Settings { get; private set; } = default!;
        public PlayerPreferences Preferences { get; private set; } = default!;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgPreferencesAndSettings>(HandlePreferencesAndSettings);
            _netManager.RegisterNetMessage<MsgUpdateCharacter>();
            _netManager.RegisterNetMessage<MsgSelectCharacter>();
            _netManager.RegisterNetMessage<MsgDeleteCharacter>();

            // RMC14
            _netManager.RegisterNetMessage<MsgSetCharacterEnable>();
            _netManager.RegisterNetMessage<MsgUpdateJobPriorities>();
            _netManager.RegisterNetMessage<MsgUpdatePlayerSettings>();
            _netManager.RegisterNetMessage<MsgPreferencesSync>(HandlePreferencesSync);

            _baseClient.RunLevelChanged += BaseClientOnRunLevelChanged;
        }

        private void BaseClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
        {
            if (e.NewLevel == ClientRunLevel.Initialize)
            {
                Settings = default!;
                Preferences = default!;
            }
        }

        public void SelectCharacter(ICharacterProfile profile)
        {
            SelectCharacter(Preferences.IndexOfCharacter(profile));
        }

        public void SelectCharacter(int slot)
        {
            Preferences = Preferences with { SelectedCharacterIndex = slot }; // RMC14
            var msg = new MsgSelectCharacter
            {
                SelectedCharacterIndex = slot
            };
            _netManager.ClientSendMessage(msg);
        }

        public void UpdateCharacter(ICharacterProfile profile, int slot)
        {
            var collection = IoCManager.Instance!;
            profile.EnsureValid(_playerManager.LocalSession!, collection);
            var characters = new Dictionary<int, ICharacterProfile>(Preferences.Characters) {[slot] = profile};
            Preferences = Preferences with { Characters = characters }; // RMC14
            var msg = new MsgUpdateCharacter
            {
                Profile = profile,
                Slot = slot
            };
            _netManager.ClientSendMessage(msg);
        }

        public void CreateCharacter(ICharacterProfile profile)
        {
            var characters = new Dictionary<int, ICharacterProfile>(Preferences.Characters);
            var lowest = Enumerable.Range(0, Settings.MaxCharacterSlots)
                .Except(characters.Keys)
                .FirstOrNull();

            if (lowest == null)
            {
                throw new InvalidOperationException("Out of character slots!");
            }

            var l = lowest.Value;
            characters.Add(l, profile);
            Preferences = Preferences with { Characters = characters }; // RMC14

            UpdateCharacter(profile, l);
        }

        public void DeleteCharacter(ICharacterProfile profile)
        {
            DeleteCharacter(Preferences.IndexOfCharacter(profile));
        }

        public void DeleteCharacter(int slot)
        {
            var characters = Preferences.Characters.Where(p => p.Key != slot);
            Preferences = Preferences with { Characters = characters.ToDictionary() }; // RMC14
            var msg = new MsgDeleteCharacter
            {
                Slot = slot
            };
            _netManager.ClientSendMessage(msg);
        }

        // RMC14 begin
        public void SetCharacterEnable(int slot, bool enable = true)
        {
            if (!Preferences.TryGetHumanoidInSlot(slot, out var humanoid))
                return;

            var characters = new Dictionary<int, ICharacterProfile>(Preferences.Characters)
            {
                [slot] = humanoid.WithEnabled(enable),
            };

            Preferences = Preferences with { Characters = characters };
            _netManager.ClientSendMessage(new MsgSetCharacterEnable
            {
                CharacterIndex = slot,
                EnabledValue = enable,
            });
        }

        public void UpdateJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            Preferences = Preferences with { JobPriorities = jobPriorities };
            _netManager.ClientSendMessage(new MsgUpdateJobPriorities
            {
                JobPriorities = jobPriorities,
            });
        }

        public void UpdatePlayerSettings(RMCPlayerSettings settings)
        {
            settings = settings.Validated(_playerManager.LocalSession!, IoCManager.Instance!);
            Preferences = Preferences with { PlayerSettings = settings };
            _netManager.ClientSendMessage(new MsgUpdatePlayerSettings
            {
                Settings = settings,
            });
        }
        // RMC14 end

        public void UpdateConstructionFavorites(List<ProtoId<ConstructionPrototype>> favorites)
        {
            Preferences = Preferences with { ConstructionFavorites = favorites }; // RMC14
            var msg = new MsgUpdateConstructionFavorites
            {
                Favorites = favorites
            };
            _netManager.ClientSendMessage(msg);
        }

        private void HandlePreferencesAndSettings(MsgPreferencesAndSettings message)
        {
            Preferences = message.Preferences;
            Settings = message.Settings;

            OnServerDataLoaded?.Invoke();
        }

        // RMC14
        private void HandlePreferencesSync(MsgPreferencesSync message)
        {
            if (Settings == null)
                return;

            Preferences = message.Preferences;
            OnPreferencesUpdated?.Invoke();
        }
    }
}
