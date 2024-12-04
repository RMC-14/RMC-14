using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ForTheHive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForTheHiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? BaseSprite;

    [DataField, AutoNetworkedField]
    public string? ActiveSprite;

    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTimeBase = TimeSpan.FromSeconds(1.6);

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public int Minimum = 200;

    [DataField, AutoNetworkedField]
    public DamageSpecifier BaseDamage = new();

    [DataField]
    public ComponentRegistry? Acid;
}
