using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Teleporter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTeleporterSystem))]
public sealed partial class RMCTeleporterComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Adjust;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? TeleportDamage = default!;
}
