using System.Collections.Generic;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleArmorHardpointComponent : Component
{
    [DataField("modifierSets")]
    public List<ProtoId<DamageModifierSetPrototype>> ModifierSets = new();

    [DataField("explosionCoefficient")]
    public float? ExplosionCoefficient;
}
