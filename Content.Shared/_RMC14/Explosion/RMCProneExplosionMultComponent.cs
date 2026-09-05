using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCExplosionSystem))]
public sealed partial class RMCProneExplosionMultComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ProneMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float NoMultCenterRadius = 0.5f;
}

/// <summary>
/// Raise right before ExplosionRecievedEvent, containing resistance modified damage, meant to modify damage right before it happens.
/// </summary>
/// <param name="Explosion"></param>
/// <param name="Epicenter"></param>
/// <param name="Damage"></param>
/// <param name="HasDirectionOverride">Overrides epicenter checks. True = not at epicenter, False = at epicenter.</param>
[ByRefEvent]
public record struct BeforeExplosionRecievedEvent(ProtoId<ExplosionPrototype> Explosion, MapCoordinates Epicenter, DamageSpecifier Damage, bool? HasDirectionOverride = null);
