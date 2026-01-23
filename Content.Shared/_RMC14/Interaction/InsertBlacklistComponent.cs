using Content.Shared.Whitelist;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCInteractionSystem))]
public sealed partial class InsertBlacklistComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public HashSet<MobState>? BlacklistedMobStates = null;

    [DataField]
    public HashSet<MobState>? WhitelistedMobStates = null;
}
