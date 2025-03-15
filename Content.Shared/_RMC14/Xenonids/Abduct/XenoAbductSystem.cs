using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Hook;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Xenonids.Abduct;

public sealed partial class XenoAbductSystem : EntitySystem
{
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly XenoHookSystem _hook = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCPullingSystem _pulling = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly HashSet<EntityUid> abductEnts = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAbductComponent, XenoAbductActionEvent>(OnXenoAbduct);
        SubscribeLocalEvent<XenoAbductComponent, XenoAbductDoAfterEvent>(OnXenoAbductDoafter);
    }

    private void OnXenoAbduct(Entity<XenoAbductComponent> xeno, ref XenoAbductActionEvent args)
    {
        if (args.Handled || args.Coords == null)
            return;

        if (!_plasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.Cost))
            return;

        CleanUpTiles(xeno);

        var tiles = _line.DrawLine(xeno.Owner.ToCoordinates(), args.Coords.Value, TimeSpan.Zero, out _);

        if (tiles.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-abduct-no-room"), xeno, PopupType.SmallCaution);
            return;
        }

        var duct = new XenoAbductDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.DoafterTime, duct, xeno, null)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BlockDuplicate = true,
        };

        if (_doafter.TryStartDoAfter(doAfter))
        {
            _stun.TrySlowdown(xeno, xeno.Comp.DoafterTime, false, 0f, 0f);

            if (_net.IsClient)
                return;

            foreach (var tile in tiles)
            {
                xeno.Comp.Tiles.Add(Spawn(xeno.Comp.Telegraph, tile.Coordinates));
            }

            if (xeno.Comp.Emote is { } emote)
                _emote.TryEmoteWithChat(xeno, emote);
        }
    }

    private void OnXenoAbductDoafter(Entity<XenoAbductComponent> xeno, ref XenoAbductDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-abduct-cancel"), xeno, xeno, PopupType.Medium);
            CleanUpTiles(xeno);

            DoCooldown(xeno);
            _status.TryRemoveStatusEffect(xeno, "SlowedDown");
            return;
        }

        args.Handled = true;

        if (!_plasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.Cost))
            return;

        if (_net.IsClient)
            return;

        if (!TryComp<XenoHookComponent>(xeno, out var hook))
            return;

        var hookEnt = (xeno.Owner, hook);

        //The fun part
        List<EntityUid> targets = new();

        foreach (var tile in xeno.Comp.Tiles)
        {
            abductEnts.Clear();
            _lookup.GetEntitiesInRange(tile.ToCoordinates(), xeno.Comp.TileRadius, abductEnts);

            foreach (var ent in abductEnts)
            {
                //Can't grab if:
                //Not human, not harmable
                //Dead, Incapacitated, or big
                //Incapacitated includes dead, crit, or stunned looks like
                if (HasComp<StunnedComponent>(ent) || !_xeno.CanAbilityAttackTarget(xeno, ent) || _mob.IsCritical(ent))
                    continue;

                if (!targets.Contains(ent))
                    targets.Add(ent);
            }
        }

        CleanUpTiles(xeno);
        //Pull em in
        string popupMsg = Loc.GetString("rmc-xeno-abduct-none");
        _audio.PlayPvs(xeno.Comp.Sound, xeno);

        TimeSpan slowTime = TimeSpan.Zero;
        TimeSpan rootTime = TimeSpan.Zero;
        TimeSpan dazeTime = TimeSpan.Zero;
        TimeSpan stunTime = TimeSpan.FromSeconds(0.4);

        if (targets.Count > 2)
        {
            popupMsg = Loc.GetString("rmc-xeno-abduct-more", ("targets", targets.Count));
            stunTime = xeno.Comp.StunTime;
        }
        else if (targets.Count == 2)
        {
            popupMsg = Loc.GetString("rmc-xeno-abduct-two");
            rootTime = xeno.Comp.RootTime;
            dazeTime = xeno.Comp.DazeTime;
        }
        else if (targets.Count == 1)
        {
            popupMsg = Loc.GetString("rmc-xeno-abduct-one");
            slowTime = xeno.Comp.SlowTime;
        }

        DoCooldown(xeno);
        _popup.PopupEntity(popupMsg, xeno, xeno, PopupType.Medium);

        for (var i = 0; i < targets.Count; i++)
        {
            if (i >= xeno.Comp.MaxTargets)
                break;

            var ent = targets[i];
            if (_hook.TryHookTarget(hookEnt, ent))
            {
                _pulling.TryStopAllPullsFromAndOn(ent);

                var origin = _transform.GetMoverCoordinates(xeno);
                var target = _transform.GetMoverCoordinates(ent);
                var diff = origin.Position - target.Position;
                if (!origin.TryDistance(EntityManager, target, out var dis))
                    return;
                diff = diff.Normalized() * Math.Max(dis - 2, 0.5f); // Lands right in front

                //TODO RMC14 Camera shake

                _slow.TrySlowdown(ent, slowTime, ignoreDurationModifier: true);
                _slow.TryRoot(ent, rootTime);
                _stutter.DoStutter(ent, dazeTime, true);
                _stun.TryParalyze(ent, stunTime, true);

                _throwing.TryThrow(ent, diff, 10, user: xeno);
            }
        }
    }

    private void CleanUpTiles(Entity<XenoAbductComponent> xeno)
    {
        if (_net.IsClient)
            return;

        foreach (var til in xeno.Comp.Tiles)
        {
            QueueDel(til);
        }

        xeno.Comp.Tiles.Clear();
        Dirty(xeno);

    }

    private void DoCooldown(Entity<XenoAbductComponent> xeno)
    {
        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoAbductActionEvent)
                _actions.SetCooldown(actionId, xeno.Comp.Cooldown);
        }
    }
}
