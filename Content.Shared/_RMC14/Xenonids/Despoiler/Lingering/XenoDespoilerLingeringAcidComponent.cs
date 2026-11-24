using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Lingering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerLingeringAcidComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan MinLifetime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxLifetime = TimeSpan.FromSeconds(20);

    [DataField]
    public DamageSpecifier CrossBurnDamage = new() { DamageDict = { { "Heat", 20 } } };

    [DataField, AutoNetworkedField]
    public EntityUid? Caster;
}
