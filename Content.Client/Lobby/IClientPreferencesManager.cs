using Content.Shared._RMC14.Preferences;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby
{
    public interface IClientPreferencesManager
    {
        event Action OnServerDataLoaded;

        // RMC14
        event Action OnPreferencesUpdated;

        bool ServerDataLoaded => Settings != null;

        GameSettings? Settings { get; }
        PlayerPreferences? Preferences { get; }
        void Initialize();
        void SelectCharacter(ICharacterProfile profile);
        void SelectCharacter(int slot);
        void UpdateCharacter(ICharacterProfile profile, int slot);
        void CreateCharacter(ICharacterProfile profile);
        void DeleteCharacter(ICharacterProfile profile);
        void DeleteCharacter(int slot);

        // RMC14
        void SetCharacterEnable(int slot, bool enable = true);
        void UpdateJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities);
        void UpdatePlayerSettings(RMCPlayerSettings settings);

        void UpdateConstructionFavorites(List<ProtoId<ConstructionPrototype>> favorites);
    }
}
