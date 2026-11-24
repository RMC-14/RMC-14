using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RMC14.Preferences;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Preferences.Managers
{
    public interface IServerPreferencesManager
    {
        void Init();

        Task LoadData(ICommonSession session, CancellationToken cancel);
        void FinishLoad(ICommonSession session);
        void OnClientDisconnected(ICommonSession session);

        bool TryGetCachedPreferences(NetUserId userId, [NotNullWhen(true)] out PlayerPreferences? playerPreferences);
        PlayerPreferences GetPreferences(NetUserId userId);
        PlayerPreferences? GetPreferencesOrNull(NetUserId? userId);
        IEnumerable<KeyValuePair<NetUserId, ICharacterProfile>> GetSelectedProfilesForPlayers(List<NetUserId> userIds);
        bool HavePreferencesLoaded(ICommonSession session);

        Task SetProfile(NetUserId userId, int slot, ICharacterProfile profile);
        Task SetConstructionFavorites(NetUserId userId, List<ProtoId<ConstructionPrototype>> favorites);
        // RMC14
        Task SetJobPriorities(NetUserId userId, IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities);
        Task SetPlayerSettings(NetUserId userId, RMCPlayerSettings settings);
        Task SetCharacterEnabled(NetUserId userId, int slot, bool enabled);

        Task DeleteProfile(NetUserId userId, int slot);
    }
}
