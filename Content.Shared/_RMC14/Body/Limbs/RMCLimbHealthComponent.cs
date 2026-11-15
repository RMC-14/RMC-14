using Content.Shared._RMC14.Body.Zones;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     splits the mob's brute and burn across its limbs
///     the totals stay on the damageable component and the limbs always add up to them
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCLimbHealthSystem))]
public sealed partial class RMCLimbHealthComponent : Component
{
    // a limb only gets an entry once it takes damage, eyes and mouth count as the head so they never get one
    [DataField, AutoNetworkedField]
    public Dictionary<RMCTargetZone, RMCLimb> Limbs = new();

    // defaults are cmss13's limb max damage, parent limb and organ_rel_size
    [DataField]
    public Dictionary<RMCTargetZone, RMCLimbDefinition> Definitions = new()
    {
        [RMCTargetZone.Chest] = new(200, null, 70),
        [RMCTargetZone.Abdomen] = new(200, RMCTargetZone.Chest, 30),
        [RMCTargetZone.Head] = new(60, RMCTargetZone.Chest, 15),
        [RMCTargetZone.LeftArm] = new(35, RMCTargetZone.Chest, 25),
        [RMCTargetZone.RightArm] = new(35, RMCTargetZone.Chest, 25),
        [RMCTargetZone.LeftHand] = new(30, RMCTargetZone.LeftArm, 7),
        [RMCTargetZone.RightHand] = new(30, RMCTargetZone.RightArm, 7),
        [RMCTargetZone.LeftLeg] = new(35, RMCTargetZone.Abdomen, 25),
        [RMCTargetZone.RightLeg] = new(35, RMCTargetZone.Abdomen, 25),
        [RMCTargetZone.LeftFoot] = new(30, RMCTargetZone.LeftLeg, 10),
        [RMCTargetZone.RightFoot] = new(30, RMCTargetZone.RightLeg, 10),
    };

    // set by SetNextHit right before a damage call and used up by it
    [ViewVariables]
    public RMCLimbHit? NextHit;

    [ViewVariables]
    public GameTick NextHitTick;

    // where the damage being applied right now lands, decided before armour runs
    [ViewVariables]
    public RMCLimbHit? CurrentHit;

    [ViewVariables]
    public GameTick CurrentHitTick;
}
