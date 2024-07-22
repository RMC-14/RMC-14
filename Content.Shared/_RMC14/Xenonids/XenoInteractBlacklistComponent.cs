using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent]
[Access(typeof(XenoSystem))]
public sealed partial class XenoInteractBlacklistComponent : Component
{
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist = null;
}
