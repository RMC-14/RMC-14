using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Events;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Rules;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public sealed class RMCFultonSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRottingSystem _rotting = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCPullingSystem _rmcpulling = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;

    private int _fultonCount;
    private MapId? _fultonMap;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<RMCCanBeFultonedComponent, InteractUsingEvent>(OnCanBeFultonedInteractUsing);
        SubscribeLocalEvent<RMCCanBeFultonedComponent, RMCPrepareFultonDoAfterEvent>(OnCanBeFultonedPrepareFulton);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _fultonCount = 0;
        _fultonMap = null;
    }

    private void OnCanBeFultonedInteractUsing(Entity<RMCCanBeFultonedComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        var target = args.Target;
        var used = args.Used;
        if (!HasComp<RMCFultonComponent>(used))
            return;

        if (_mobState.IsAlive(target) || _mobState.IsCritical(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-fulton-not-dead", ("fulton", used), ("target", target)), target, user);
            return;
        }

        if (HasComp<PerishableComponent>(target) && !_rotting.IsRotten(target) ||
            HasComp<RMCRevivableComponent>(target) && !_unrevivable.IsUnrevivable(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-fulton-not-unrevivable", ("fulton", used), ("target", target)), target, user);
            return;
        }

        if (!_rmcPlanet.IsOnPlanet(target.ToCoordinates()))
        {
            _popup.PopupClient(Loc.GetString("rmc-fulton-not-planet", ("fulton", used)), target, user);
            return;
        }

        if (!_area.CanFulton(target.ToCoordinates()))
        {
            _popup.PopupClient(Loc.GetString("rmc-fulton-underground", ("fulton", used)), target, user);
            return;
        }

        var delay = ent.Comp.Delay * _skills.GetSkillDelayMultiplier(user, ent.Comp.Skill);
        var ev = new RMCPrepareFultonDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, used) { BreakOnMove = true };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-fulton-attach-start-self", ("fulton", used), ("target", target));
            var othersMsg = Loc.GetString("rmc-fulton-attach-start-others", ("user", user), ("fulton", used), ("target", target));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnCanBeFultonedPrepareFulton(Entity<RMCCanBeFultonedComponent> ent, ref RMCPrepareFultonDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var active = EnsureComp<RMCActiveFultonComponent>(target);
        active.ReturnAt = _timing.CurTime + ent.Comp.ReturnDelay;
        active.ReturnTo = _transform.GetMoverCoordinates(ent);
        Dirty(target, active);

        if (TryComp(target, out RMCCanBeFultonedComponent? canBeFultoned))
            _audio.PlayPredicted(canBeFultoned.FultonSound, active.ReturnTo, args.User);

        var name = Name(target);
        _dropshipWeapon.MakeTarget(target, name, false);
        _rmcpulling.TryStopAllPullsFromAndOn(target);

        var mapId = EnsureMap();
        _transform.SetMapCoordinates(target, new MapCoordinates(_fultonCount++ * 50, 0, mapId));

        if (args.Used != null)
            _stack.Use(args.Used.Value, 1);
    }

    private MapId EnsureMap()
    {
        if (!_map.MapExists(_fultonMap))
            _fultonMap = null;

        if (_fultonMap == null)
        {
            _map.CreateMap(out var map);
            _fultonMap = map;
        }

        return _fultonMap.Value;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCActiveFultonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.ReturnAt)
                continue;

            RemComp<DropshipTargetComponent>(uid);
            RemCompDeferred<RMCActiveFultonComponent>(uid);

            _transform.SetCoordinates(uid, comp.ReturnTo);
            _audio.PlayPvs(comp.ReturnSound, comp.ReturnTo);
        }
    }
}
