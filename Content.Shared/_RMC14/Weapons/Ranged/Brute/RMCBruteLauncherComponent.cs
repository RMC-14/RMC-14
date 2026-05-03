using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Targeting;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged.Brute;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBruteLauncherComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AimDelay = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId<SkillDefinitionComponent> RequiredSkill = "RMCSkillEngineer";

    [DataField]
    public int RequiredSkillLevel = 3;

    [DataField]
    public TargetedEffects TargetedEffect = TargetedEffects.Targeted;

    [DataField]
    public bool ShowDirection = true;

    [DataField]
    public string LockOnState = "sniper_lockon_guided";

    [DataField]
    public string LockOnStateDirection = "sniper_lockon_guided_direction";

    [DataField]
    public string LaserState = "laser_beam_guided";

    [DataField, AutoNetworkedField]
    public bool LockComplete;

    // Server-authoritative lock state used to clean up targeting visuals consistently.
    [DataField, AutoNetworkedField]
    public EntityUid? LockTarget;

    // Prevents stale do-afters from firing after a newer lock attempt replaces them.
    [DataField]
    public uint LockId;
}

[Serializable, NetSerializable]
public sealed partial class RMCBruteLockOnDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public uint LockId;

    [DataField(required: true)]
    public NetEntity Target;

    [DataField(required: true)]
    public NetCoordinates Coordinates;

    public RMCBruteLockOnDoAfterEvent(uint lockId, NetEntity target, NetCoordinates coordinates)
    {
        LockId = lockId;
        Target = target;
        Coordinates = coordinates;
    }

    private RMCBruteLockOnDoAfterEvent()
    {
    }

    public override DoAfterEvent Clone()
    {
        return new RMCBruteLockOnDoAfterEvent(LockId, Target, Coordinates);
    }
}
