﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class RMCExplosiveDeleteComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 1;

    [DataField, AutoNetworkedField]
    public float Delay = 3;

    [DataField, AutoNetworkedField]
    public float BeepInterval = 10;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public bool DeleteWalls = true;
}
