using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared._RMC14.Xenonids.TailLash;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Xenonids.Tail_Lash;

public sealed class XenoTailLashSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private XenoPlasmaSystem _plasma = default!;
    [Dependency] private XenoSystem _xeno = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private RMCSizeStunSystem _size = default!;
    [Dependency] private RMCSlowSystem _slow = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private RMCPullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTailLashComponent, XenoTailLashActionEvent>(OnTailLashAction);
        SubscribeLocalEvent<XenoTailLashComponent, XenoTailLashDoAfterEvent>(OnTailLashDoAfter);
    }

    private void OnTailLashAction(Entity<XenoTailLashComponent> xeno, ref XenoTailLashActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_plasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.Cost))
            return;

        if (_transform.GetGrid(args.Target) is not { } gridId ||
    !TryComp(gridId, out MapGridComponent? grid))
            return;

        var direction = (args.Target.Position - _transform.GetMoverCoordinates(xeno).Position).Normalized().ToAngle() - Angle.FromDegrees(90);

        var xenoCoord = _transform.GetMoverCoordinates(xeno);
        var area = Box2.CenteredAround(xenoCoord.Position, new(xeno.Comp.Width, xeno.Comp.Height)).Translated(new(0, (xeno.Comp.Height / 2) + 0.5f));
        var rot = new Box2Rotated(area, direction, xenoCoord.Position); // Correct the angle

        bool valid = false;

        var bounds = rot.CalcBoundingBox();

        foreach (var tile in _map.GetTilesIntersecting(gridId, grid, rot))
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, _turf.GetTileCenter(tile), xeno.Comp.Width * xeno.Comp.Height, collisionMask:CollisionGroup.MobMask)) //Range arbitiary, just needs to reach
                continue;

            valid = true;

            if (_net.IsClient)
                continue;

            var spawn = xeno.Comp.Effect;
            if (!bounds.Encloses(Box2.CenteredAround(_turf.GetTileCenter(tile).Position, Vector2.One)))
                spawn = xeno.Comp.EffectEdge;
            SpawnAtPosition(spawn, _turf.GetTileCenter(tile));
        }

        if (!valid)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-tail-lash-no-room"), xeno, xeno, PopupType.MediumCaution);
            return;
        }

        xeno.Comp.Area = rot;

        var ar = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Windup, new XenoTailLashDoAfterEvent(), xeno)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(ar);
    }

    private void OnTailLashDoAfter(Entity<XenoTailLashComponent> xeno, ref XenoTailLashDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || xeno.Comp.Area == null || !_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.Cost))
        {
            xeno.Comp.Area = null;
            return;
        }

        EnsureComp<XenoSweepingComponent>(xeno);

        DoCooldown(xeno);

        if (_net.IsClient)
            return;

        args.Handled = true;

        foreach (var ent in _physics.GetCollidingEntities(Transform(xeno).MapID, xeno.Comp.Area.Value))
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            if (!_interaction.InRangeUnobstructed(xeno.Owner, ent.Owner, xeno.Comp.Width * xeno.Comp.Height, collisionMask: CollisionGroup.MobMask)) //Ditto
                continue;

            if (_size.TryGetSize(ent, out var size) && size >= RMCSizes.Big)
                continue;

            _stun.TryParalyze(ent, _xeno.TryApplyXenoDebuffMultiplier(ent, xeno.Comp.StunTime), true);
            _slow.TrySlowdown(ent, _xeno.TryApplyXenoDebuffMultiplier(ent, xeno.Comp.SlowTime));

            _pulling.TryStopAllPullsFromAndOn(ent);

            var origin = _transform.GetMapCoordinates(xeno);
            _size.KnockBack(ent, origin, xeno.Comp.FlingDistance, xeno.Comp.FlingDistance, 10);

            _rmcDamageable.DoLethalDamage(ent, origin: xeno);
        }

        xeno.Comp.Area = null;
        Dirty(xeno);
    }

    private void DoCooldown(Entity<XenoTailLashComponent> xeno)
    {
        foreach (var (actionId, action) in _rmcActions.GetActionsWithEvent<XenoTailLashActionEvent>(xeno))
        {
            _actions.SetCooldown(actionId, xeno.Comp.Cooldown);
        }
    }
}
