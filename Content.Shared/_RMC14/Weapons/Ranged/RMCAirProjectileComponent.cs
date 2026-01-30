using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCAirProjectileComponent : Component
{
    /// <summary>
    ///     The prototype to spawn when the entity is shot into the air.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;
}
