using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoFrenzyPheromonesComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_CM14/Interface/xeno_pheromones_hud.rsi"), "frenzy");

    [DataField]
    public FixedPoint2 Multiplier;

    [DataField]
    public float AttackDamageModifier = 1.1f;

    [DataField]
    public FixedPoint2 MovementSpeedModifier = 0.1;

    [DataField, ValidatePrototypeId<DamageGroupPrototype>]
    public string[] DamageTypes = { "Blunt", "Slash", "Piercing" };


    public override bool SessionSpecific => true;
}
