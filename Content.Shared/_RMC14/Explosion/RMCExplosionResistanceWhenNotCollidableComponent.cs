using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
/// Modifies explosion resistance while the entity's physics body is not collidable.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class RMCExplosionResistanceWhenNotCollidableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<ExplosionPrototype>, float> Modifiers = new();
}
