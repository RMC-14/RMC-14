﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCInteractionSystem))]
public sealed partial class InteractedBlacklistComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
