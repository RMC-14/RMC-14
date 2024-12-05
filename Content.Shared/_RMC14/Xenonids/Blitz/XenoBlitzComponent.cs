using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Blitz;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoBlitzComponent : Component
{
    [DataField, AutoNetworkedField]
    public int PlasmaCost = 50;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseUseDelay = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan FinishedUseDelay = TimeSpan.FromSeconds(11);

    [DataField, AutoNetworkedField]
    public bool Dashed = false;

    [DataField, AutoNetworkedField]
    public bool SlashReady = false;

    [DataField, AutoNetworkedField]
    public TimeSpan SlashAroundAt;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage;

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectExtraSlash";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("AlienClaw");

    [DataField, AutoNetworkedField]
    public TimeSpan SlashDashTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public float Range = 1.5f;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionToReset = "ActionXenoBlitz";

    [DataField, AutoNetworkedField]
    public int HitsToRecharge = 1;
}
