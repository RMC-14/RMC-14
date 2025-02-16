﻿using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Spawners;

[RegisterComponent]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class CorpseSpawnerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Spawn;
}
