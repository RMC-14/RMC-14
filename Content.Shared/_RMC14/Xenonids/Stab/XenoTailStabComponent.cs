using System.Diagnostics.Metrics;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Stab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoTailStabSystem))]
public sealed partial class XenoTailStabComponent : Component, IComponentDebug
{
    [DataField, AutoNetworkedField]
    public EntProtoId TailAnimationId = "WeaponArcThrust";

    [DataField, AutoNetworkedField]
    public EntProtoId HitAnimationId = "RMCEffectTailHit";

    [DataField, AutoNetworkedField]
    public FixedPoint2 TailRange = 2;

    [DataField]
    public DamageSpecifier TailDamage = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier SoundHit = new SoundCollectionSpecifier("XenoBite")
    {
        Params = AudioParams.Default.WithVariation(0.15f).WithVolume(-3),
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier SoundMiss = new SoundCollectionSpecifier("XenoTailSwipe")
    {
        Params = AudioParams.Default.WithVariation(0.15f),
    };

    [DataField, AutoNetworkedField]
    public float ChargeTime = 1; // TODO RMC14 implement this

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan BigDazeTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>? Inject;

    [DataField, AutoNetworkedField]
    public bool Toggle = false;

    [DataField, AutoNetworkedField]
    public bool InjectNeuro = false;

    public string GetDebugString()
    {
        var injectStrings = Inject?.Select(item => $"{item.Key}: {item.Value}");
        return $"""
            TailAnimationId: {TailAnimationId}
            HitAnimationId: {HitAnimationId}
            TailRange: {TailRange}
            TailDamage: {TailDamage}
            DazeTime: {DazeTime.TotalSeconds}
            BigDazeTime: {BigDazeTime.TotalSeconds}
            Toggle: {Toggle}
            InjectNeuro: {InjectNeuro}
            Inject:
              {string.Join("\r\n  ", injectStrings ?? [])}
            """;
    }
}
