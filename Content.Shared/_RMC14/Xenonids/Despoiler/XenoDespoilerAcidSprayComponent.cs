using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerAcidSprayComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public bool StunsOnEmpowered;

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan GrantImmunityDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public EntityUid? Caster;
}
