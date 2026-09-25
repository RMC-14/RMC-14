using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class WoundableComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageGroupPrototype>, WoundType> WoundGroups = new()
    {
        { "Brute", WoundType.Brute },
        { "Burn", WoundType.Burn },
    };

    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedMinDamage = 10;

    [DataField, AutoNetworkedField]
    public float BloodLossMultiplier = 0.0375f;

    [DataField, AutoNetworkedField]
    public TimeSpan DurationMultiplier = TimeSpan.FromSeconds(2.5f);
}
