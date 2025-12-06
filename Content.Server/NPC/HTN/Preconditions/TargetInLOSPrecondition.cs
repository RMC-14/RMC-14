using System.Collections.Generic; //RMC
using Content.Server.Interaction;
using Content.Shared._RMC14.Weapons.Ranged.IFF; //RMC
using Content.Shared.Mobs.Systems; //RMC
using Content.Shared.Inventory; //RMC
using Content.Shared.Physics;
using Robust.Shared.Prototypes; //RMC

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class TargetInLOSPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private MobStateSystem _mobState = default!; //RMC
    private GunIFFSystem _gunIFF = default!; //RMC
    private InteractionSystem _interaction = default!;

    [DataField("targetKey")]
    public string TargetKey = "Target";

    [DataField("rangeKey")]
    public string RangeKey = "RangeKey";

    [DataField("opaqueKey")]
    public bool UseOpaqueForLOSChecksKey = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _interaction = sysManager.GetEntitySystem<InteractionSystem>();
        _mobState = sysManager.GetEntitySystem<MobStateSystem>(); //RMC
        _gunIFF = sysManager.GetEntitySystem<GunIFFSystem>(); //RMC
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
            return false;

        var range = blackboard.GetValueOrDefault<float>(RangeKey, _entManager);
        var collisionGroup = UseOpaqueForLOSChecksKey ? CollisionGroup.Opaque : (CollisionGroup.Impassable | CollisionGroup.InteractImpassable);

        // RMC begin
        return _interaction.InRangeUnobstructed(
            owner,
            target,
            range,
            collisionGroup,
            predicate: blocker => ShouldIgnoreLosBlocker(owner, target, blocker));
    }

    private bool ShouldIgnoreLosBlocker(EntityUid owner, EntityUid target, EntityUid blocker)
    {
        if (blocker == target || blocker == owner)
            return false;

        if (_mobState.IsDead(blocker))
            return true;

        if (SharesIff(owner, blocker))
            return true;

        return false;
    }

    private bool SharesIff(EntityUid owner, EntityUid other)
    {
        var factions = new HashSet<EntProtoId<IFFFactionComponent>>();

        if (_entManager.TryGetComponent(owner, out UserIFFComponent? userIff))
            factions.UnionWith(userIff.Factions);

        var ev = new GetIFFFactionEvent(SlotFlags.IDCARD, new HashSet<EntProtoId<IFFFactionComponent>>());
        _entManager.EventBus.RaiseLocalEvent(owner, ref ev);

        factions.UnionWith(ev.Factions);

        foreach (var faction in factions)
        {
            if (_gunIFF.IsInFaction(other, faction))
                return true;
        }

        return false;
    }
    //  RMC end
}
