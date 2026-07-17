using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._RMC14.Xenonids.Stab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoTailStabSystem))]
public sealed partial class XenoAltTailStabComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId HitAnimationId = "RMCEffectSlam";

    [DataField]
    public DamageSpecifier TailDamage = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier SoundHit = new SoundCollectionSpecifier("Punch")
    {
        Params = AudioParams.Default.WithVariation(0.15f),
    };

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan BigDazeTime = TimeSpan.FromSeconds(0);

    [DataField]
    public int ArmorPiercing = 200; // By default pierces through all armor

    [DataField, AutoNetworkedField]
    public bool HitMobsOnly = true;

    //TODO RMC14 an option to only hit the chest (dancer tail lance parity)
}
