using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoRecoveryPheromonesComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_pheromones_hud.rsi"), "recovery");

    [DataField]
    public FixedPoint2 Multiplier;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextRegenTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public FixedPoint2 HealthRegen = 0.5;

    [DataField]
    public FixedPoint2 PlasmaRegen = 1.5;

    public override bool SessionSpecific => true;
}
