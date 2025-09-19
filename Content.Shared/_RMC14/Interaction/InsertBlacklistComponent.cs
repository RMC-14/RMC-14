using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Storage.Components;

[RegisterComponent]
public sealed partial class InsertBlacklistComponent : Component
{
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;
    
    [DataField("blacklistedMobStates")]
    public HashSet<MobState>? BlacklistedMobStates = null;

    [DataField("whitelistedMobStates")]
    public HashSet<MobState>? WhitelistedMobStates = null;
}
