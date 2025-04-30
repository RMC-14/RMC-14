﻿using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunIFFSystem))]
public sealed partial class ItemIFFComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<NpcFactionPrototype>? Faction;
}
