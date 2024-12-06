using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Movement.Events;
using Content.Shared.StatusEffect;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Burrow;

/// <summary>
/// Deals with Burrowing, where a xeno goes into the ground and shortly comes back up
/// </summary>
public abstract partial class SharedXenoBurrowSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBurrowComponent, ExamineAttemptEvent>(PreventExamine);
        SubscribeLocalEvent<XenoBurrowComponent, UpdateCanMoveEvent>(PreventMovement);

        SubscribeLocalEvent<XenoBurrowComponent, BeforeStatusEffectAddedEvent>(PreventEffects);
        SubscribeLocalEvent<XenoBurrowComponent, BeforeDamageChangedEvent>(PreventDamage);
        SubscribeLocalEvent<XenoBurrowComponent, PreventCollideEvent>(PreventCollision);
    }

    private void PreventExamine(EntityUid ent, XenoBurrowComponent comp, ref ExamineAttemptEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        if (HasComp<XenoComponent>(args.Examiner))
        {
            return;
        }

        args.Cancel();
    }

    private void PreventMovement(EntityUid ent, XenoBurrowComponent comp, ref UpdateCanMoveEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        args.Cancel();
    }

    private void PreventEffects(EntityUid ent, XenoBurrowComponent comp, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        // Note: If any beneficial effects is added that makes sense underground, this may have to be more precise
        args.Cancelled = true;
    }

    private void PreventDamage(EntityUid ent, XenoBurrowComponent comp, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        args.Cancelled = true;
    }

    private void PreventCollision(EntityUid ent, XenoBurrowComponent comp, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        args.Cancelled = true;
    }
}

public sealed partial class XenoBurrowActionEvent : WorldTargetActionEvent;

/// <summary>
/// Called when a Xeno starts to burrow towards a specific tile
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoBurrowMoveDoAfter : SimpleDoAfterEvent
{
    public NetCoordinates TargetCoords;
    public XenoBurrowMoveDoAfter(NetCoordinates targetCoords)
    {
        TargetCoords = targetCoords;
    }
}
/// <summary>
/// Called when a xeno starts to burrow down into the current tile
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoBurrowDownDoAfter : SimpleDoAfterEvent;
