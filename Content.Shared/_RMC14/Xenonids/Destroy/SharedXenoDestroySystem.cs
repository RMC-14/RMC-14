using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Stun;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Destroy;
public abstract class SharedXenoDestroySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCGibSystem _rmcGib = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPull = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDestroyComponent, XenoDestroyActionEvent>(OnXenoDestroyAction);
        SubscribeLocalEvent<XenoDestroyComponent, XenoDestroyLeapDoafter>(OnXenoDestroyDoafter);

        SubscribeLocalEvent<XenoDestroyLeapingComponent, AttemptMobCollideEvent>(OnLeapCollide);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, AttemptMobTargetCollideEvent>(OnLeapTargetCollide);

        SubscribeLocalEvent<XenoDestroyLeapingComponent, ComponentInit>(OnLeapingInit);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, ComponentRemove>(OnLeapingRemove);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, DropAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, UseAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, PickupAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, AttackAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, ThrowAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, ChangeDirectionAttemptEvent>(OnLeapingCancel);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, InteractionAttemptEvent>(OnLeapingCancelInteract);
        SubscribeLocalEvent<XenoDestroyLeapingComponent, PullAttemptEvent>(OnLeapingCancelPull);
    }

    private void OnXenoDestroyAction(Entity<XenoDestroyComponent> xeno, ref XenoDestroyActionEvent args)
    {
        if (args.Handled || !_turf.TryGetTileRef(args.Target, out var tile))
            return;

        var target = _turf.GetTileCenter(tile.Value);

        if (!_interaction.InRangeUnobstructed(xeno, target, xeno.Comp.Range) || _rmcMap.IsTileBlocked(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-destroy-cant-reach"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        if (!_area.TryGetArea(target, out var area, out var _) || area.Value.Comp.NoTunnel)
        {
            _popup.PopupClient(Loc.GetString("rmc-destroy-cant-area"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        _jitter.DoJitter(xeno, xeno.Comp.JumpTime, true, 80, 8, true);

        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.JumpTime, new XenoDestroyLeapDoafter(EntityManager.GetNetCoordinates(target)), xeno)
        {
            BreakOnMove = true,
            BreakOnRest = true
        };

        _doafter.TryStartDoAfter(doAfter);
        Dirty(xeno);
    }

    private void OnXenoDestroyDoafter(Entity<XenoDestroyComponent> xeno, ref XenoDestroyLeapDoafter args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (_net.IsClient)
            return;

        args.Handled = true;

        var coords = EntityManager.GetCoordinates(args.TargetCoords);

        if (!_interaction.InRangeUnobstructed(xeno, coords, xeno.Comp.Range) || _rmcMap.IsTileBlocked(coords))
        {
            _popup.PopupClient(Loc.GetString("rmc-destroy-cant-reach"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        _rotateToFace.TryFaceCoordinates(xeno, _transform.ToMapCoordinates(args.TargetCoords).Position);
        _rmcPull.TryStopAllPullsFromAndOn(xeno);

        //Root
        _stun.TrySlowdown(xeno, xeno.Comp.CrashTime, true, 0f, 0f);
        if (_net.IsServer)
        {
            var leaping = EnsureComp<XenoDestroyLeapingComponent>(xeno);
            leaping.Target = coords;
            leaping.LeapMoveAt = _timing.CurTime + xeno.Comp.CrashTime / 2;
            leaping.LeapEndAt = _timing.CurTime + xeno.Comp.CrashTime;
            Dirty(xeno.Owner, leaping);

            var filter = Filter.Pvs(xeno);
            Vector2 offset = _transform.ToMapCoordinates(coords).Position - _transform.GetMapCoordinates(xeno).Position;

            var ev = new XenoDestroyLeapStartEvent(GetNetEntity(xeno), offset);
            RaiseNetworkEvent(ev, filter);
        }

        PredictedSpawnAtPosition(xeno.Comp.Telegraph, coords);

        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote);
    }

    private void OnLeapCollide(Entity<XenoDestroyLeapingComponent> xeno, ref AttemptMobCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLeapTargetCollide(Entity<XenoDestroyLeapingComponent> xeno, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLeapingCancel<T>(Entity<XenoDestroyLeapingComponent> ent, ref T args) where T : CancellableEntityEventArgs
    {
        args.Cancel();
    }

    private void OnLeapingCancelInteract(Entity<XenoDestroyLeapingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLeapingCancelPull(Entity<XenoDestroyLeapingComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }


    private void CrashDown(Entity<XenoDestroyComponent> xeno)
    {
        RemCompDeferred<XenoDestroyLeapingComponent>(xeno);

        if (_transform.GetGrid(xeno.Owner) is not { } gridId || !TryComp<MapGridComponent>(gridId, out var grid))
            return;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        foreach (var tile in _map.GetTilesIntersecting(gridId, grid, Box2.CenteredAround(_transform.GetMoverCoordinates(xeno).Position, new Vector2(2, 2))))
        {
            //Gib mobs, knockback items, also kill structures
            foreach (var ent in _entityLookup.GetEntitiesInTile(tile, LookupFlags.All))
            {
                if (HasComp<MobStateComponent>(ent) && _xeno.CanAbilityAttackTarget(xeno, ent))
                {
                    if (!xeno.Comp.Gibs || !TryComp<BodyComponent>(ent, out var body))
                    {
                        //just do a ton of damage instead
                        _damage.TryChangeDamage(ent, xeno.Comp.MobDamage, true, origin: xeno, tool: xeno);
                        continue;
                    }

                    if (_net.IsServer)
                    {
                        _rmcGib.ScatterInventoryItems(ent);
                        _body.GibBody(ent, true, body);
                    }
                    continue;
                }

                if (HasComp<ItemComponent>(ent) && !Transform(ent).Anchored)
                {
                    _size.KnockBack(ent, _transform.GetMapCoordinates(xeno), xeno.Comp.Knockback, xeno.Comp.Knockback, 15, true);
                    continue;
                }

                if (_whitelist.IsWhitelistPass(xeno.Comp.Structures, ent))
                {
                    var ev = new GetExplosionResistanceEvent(xeno.Comp.ExplosionType.Id);
                    RaiseLocalEvent(ent, ref ev);

                    _damage.TryChangeDamage(ent, xeno.Comp.StructureDamage * ev.DamageCoefficient, true, origin: xeno, tool: xeno);
                    continue;
                }
            }

            PredictedSpawnAtPosition(xeno.Comp.SmokeEffect, _turf.GetTileCenter(tile));
        }

        //Shake - effects everyone
        _mobs.Clear();
        _entityLookup.GetEntitiesInRange(Transform(xeno).Coordinates, xeno.Comp.ShakeCameraRange, _mobs);

        foreach (var mob in _mobs)
        {
            if (mob.Owner == xeno.Owner)
            {
                //Smaller
                _cameraShake.ShakeCamera(mob, 5, 1);
                continue;
            }

            _cameraShake.ShakeCamera(mob, 15, 1);
        }

        SetCooldown(xeno);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDestroyLeapingComponent, XenoDestroyComponent>();

        while (query.MoveNext(out var uid, out var leaping, out var destroy))
        {
            if (_mob.IsDead(uid))
            {
                RemCompDeferred<XenoDestroyLeapingComponent>(uid);
                continue;
            }

            if (leaping.LeapMoveAt != null && time > leaping.LeapMoveAt)
            {
                if (leaping.Target != null)
                    _transform.SetCoordinates(uid, leaping.Target.Value);

                leaping.LeapMoveAt = null;
                Dirty(uid, leaping);
            }

            if (leaping.LeapEndAt == null || time < leaping.LeapEndAt)
                continue;

            CrashDown((uid, destroy));
        }
    }

    private void SetCooldown(Entity<XenoDestroyComponent> xeno)
    {
        foreach (var (actionId, action) in _rmcActions.GetActionsWithEvent<XenoDestroyActionEvent>(xeno))
        {
            _actions.SetCooldown(actionId, xeno.Comp.Cooldown);
            break;
        }
    }

    private void OnLeapingInit(Entity<XenoDestroyLeapingComponent> xeno, ref ComponentInit args)
    {
        var actions = _actions.GetActions(xeno);
        foreach (var action in actions)
        {
            _actions.SetEnabled(action.AsNullable(), false);
        }

        if (xeno.Comp.Target == null || !TryComp<XenoDestroyComponent>(xeno, out var destroy))
            return;
    }

    protected virtual void OnLeapingRemove(Entity<XenoDestroyLeapingComponent> xeno, ref ComponentRemove args)
    {
        var actions = _actions.GetActions(xeno);
        foreach (var action in actions)
        {
            _actions.SetEnabled(action.AsNullable(), true);
        }
    }
}
