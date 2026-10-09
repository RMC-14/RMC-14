using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Mutiny;

/// <summary>
///     Snapshots a mutiny participant's side when an IFF projectile is fired.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MutinyProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Rule;

    [DataField, AutoNetworkedField]
    public MutinySide ShooterSide;

    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> IffFaction = "FactionMarine";
}
