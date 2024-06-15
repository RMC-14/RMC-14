using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoFrenzyPheromonesComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_pheromones_hud.rsi"), "frenzy");

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier;

    [DataField, AutoNetworkedField]
    public float AttackDamageModifier = 1.1f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MovementSpeedModifier = 0.1;

    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageTypePrototype>> DamageTypes = new() { "Blunt", "Slash", "Piercing" };
}
