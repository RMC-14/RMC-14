using Content.Shared._RMC14.Intel.Tech;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Intel.Tech;

[RegisterComponent]
[Access(typeof(TechSystem))]
public sealed partial class RespawnOnCryoComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Spawner;
}
