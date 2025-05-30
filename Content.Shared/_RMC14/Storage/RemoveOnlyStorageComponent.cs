﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RemoveOnlyStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new();

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
