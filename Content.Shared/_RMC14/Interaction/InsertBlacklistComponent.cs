using Content.Shared._RMC14.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.Components;

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
