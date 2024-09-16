using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFruitPlantActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public bool CheckWeeds;

    [DataField, AutoNetworkedField]
    public TimeSpan PlantCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier HealthCost = default!;
}

