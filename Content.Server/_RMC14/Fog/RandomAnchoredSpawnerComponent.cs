using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Fog;

/// <summary>
///     Spawns the given entity on the same tile at round-start if a <see cref="RandomAnchoredRemoverComponent"/>
///     of the same group gets chosen.
/// </summary>
[RegisterComponent]
[Access(typeof(FogSystem))]
public sealed partial class RandomAnchoredSpawnerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Group;

    [DataField(required: true)]
    public EntProtoId? Spawn;
}
