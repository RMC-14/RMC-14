using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerLingeringAcidComponent : Component
{
    [DataField]
    public TimeSpan MinLifetime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan MaxLifetime = TimeSpan.FromSeconds(20);

    [DataField]
    public DamageSpecifier CrossBurnDamage = new() { DamageDict = { { "Heat", 20 } } };

    [DataField, AutoNetworkedField]
    public EntityUid? Caster;
}
