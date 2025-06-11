using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoFlingSystem))]
public sealed partial class XenoFlingComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public float Range = 3.5f; // 4 tiles from start

    [DataField, AutoNetworkedField]
    public float EnragedRange = 0f;

    [DataField, AutoNetworkedField]
    public float ThrowSpeed = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    public int HealAmount = 0;

    [DataField, AutoNetworkedField]
    public int EnragedHealAmount = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan HealDelay = TimeSpan.FromSeconds(0.05);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg");
}
