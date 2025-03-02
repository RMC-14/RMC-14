using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Dislocate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDislocateComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Punch");

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    public float FlingRange = 1; // 1 tile from start

    [DataField, AutoNetworkedField]
    public TimeSpan RootTime = TimeSpan.FromSeconds(1.2);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownReductionTime = TimeSpan.FromSeconds(5);

    [DataField]
    public DamageSpecifier Damage = new();
}
