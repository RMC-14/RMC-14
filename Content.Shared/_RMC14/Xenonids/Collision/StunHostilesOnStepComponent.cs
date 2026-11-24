using Content.Shared.Damage;
using Content.Shared.StatusEffect;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Collision;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCollisionSystem))]
public sealed partial class StunHostilesOnStepComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public float Ratio = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public ProtoId<StatusEffectPrototype> DisableStatus = "KnockedDown";

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
