using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Spray;

public sealed class XenoSprayAcidSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private static readonly ProtoId<ReagentPrototype> AcidRemovedBy = "Water";

    private EntityQuery<BarricadeComponent> _barricadeQuery;
    private EntityQuery<XenoSprayAcidComponent> _xenoSprayAcidQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();
        _xenoSprayAcidQuery = GetEntityQuery<XenoSprayAcidComponent>();

        SubscribeLocalEvent<XenoSprayAcidComponent, XenoSprayAcidActionEvent>(OnSprayAcidAction);
        SubscribeLocalEvent<XenoSprayAcidComponent, XenoSprayAcidDoAfter>(OnSprayAcidDoAfter);

        SubscribeLocalEvent<SprayAcidedComponent, MapInitEvent>(OnSprayAcidedMapInit);
        SubscribeLocalEvent<SprayAcidedComponent, ComponentRemove>(OnSprayAcidedRemove);
        SubscribeLocalEvent<SprayAcidedComponent, VaporHitEvent>(OnSprayAcidedVaporHit);

        SubscribeLocalEvent<XenoAcidSplatterComponent, ExtinguishFireAttemptEvent>(OnAcidSplatterExtinguishFireAttempt);
    }

    private void OnSprayAcidAction(Entity<XenoSprayAcidComponent> xeno, ref XenoSprayAcidActionEvent args)
    {
        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var target = GetNetCoordinates(args.Target);

        var xenoCoords = _transform.GetMoverCoordinates(xeno);

        var length = (target.Position - xenoCoords.Position).Length();

        if (length > xeno.Comp.Range)
        {
            var direction = (target.Position - xenoCoords.Position).Normalized();
            var newTile = direction * xeno.Comp.Range;
            target = new NetCoordinates(GetNetEntity(args.Target.EntityId), xenoCoords.Position + newTile);
        }

        var ev = new XenoSprayAcidDoAfter(target);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.DoAfter, ev, xeno) { BreakOnMove = true };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSprayAcidDoAfter(Entity<XenoSprayAcidComponent> xeno, ref XenoSprayAcidDoAfter args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        if (_net.IsClient)
            return;

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoSprayAcidActionEvent>(xeno))
        {
            _actions.StartUseDelay(action.AsNullable());
        }

        var start = xeno.Owner.ToCoordinates();
        var end = GetCoordinates(args.Coordinates);
        var tiles = _line.DrawLine(start, end, xeno.Comp.Delay, xeno.Comp.Range, out var blocker);
        var active = EnsureComp<ActiveAcidSprayingComponent>(xeno);
        active.Blocker = blocker;
        active.Acid = xeno.Comp.Acid;
        active.Spawn = tiles;
        Dirty(xeno, active);
    }

    private void OnSprayAcidedMapInit(Entity<SprayAcidedComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, SprayAcidedVisuals.Acided, true);
    }

    private void OnSprayAcidedRemove(Entity<SprayAcidedComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent))
            _appearance.SetData(ent, SprayAcidedVisuals.Acided, false);
    }

    private void OnSprayAcidedVaporHit(Entity<SprayAcidedComponent> ent, ref VaporHitEvent args)
    {
        // this would use tile reactions if those had any way of telling what caused a reaction, imagine that
        var solEnt = args.Solution;
        foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((solEnt, solEnt)))
        {
            if (!solution.Comp.Solution.ContainsReagent(AcidRemovedBy, null))
                continue;

            RemCompDeferred<SprayAcidedComponent>(ent);
            break;
        }
    }

    private void OnAcidSplatterExtinguishFireAttempt(Entity<XenoAcidSplatterComponent> ent, ref ExtinguishFireAttemptEvent args)
    {
        if (ent.Comp.Xeno == args.Target)
            args.Cancelled = true;
    }

    private void TryAcid(Entity<XenoSprayAcidComponent> acid, RMCAnchoredEntitiesEnumerator anchored)
    {
        while (anchored.MoveNext(out var uid))
        {
            TryAcid(acid, uid);
        }
    }

    private void TryAcid(Entity<XenoSprayAcidComponent> acid, EntityUid target)
    {
        var time = _timing.CurTime;
        if (!_barricadeQuery.HasComp(target))
            return;

        var comp = EnsureComp<SprayAcidedComponent>(target);
        comp.Damage = acid.Comp.BarricadeDamage;
        comp.ExpireAt = time + acid.Comp.BarricadeDuration;
        Dirty(target, comp);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var spraying = EntityQueryEnumerator<ActiveAcidSprayingComponent>();
        while (spraying.MoveNext(out var uid, out var active))
        {
            active.Chain ??= _onCollide.SpawnChain();
            for (var i = active.Spawn.Count - 1; i >= 0; i--)
            {
                var acid = active.Spawn[i];
                if (time < acid.At)
                    continue;

                var spawned = Spawn(active.Acid, acid.Coordinates);
                var splatter = EnsureComp<XenoAcidSplatterComponent>(spawned);
                _hive.SetSameHive(uid, spawned);
                splatter.Xeno = uid;
                Dirty(spawned, splatter);

                if (_xenoSprayAcidQuery.TryComp(uid, out var xenoSprayAcid))
                {
                    var spray = new Entity<XenoSprayAcidComponent>(uid, xenoSprayAcid);

                    // Same tile
                    TryAcid(spray, _rmcMap.GetAnchoredEntitiesEnumerator(spawned));

                    if (active.Spawn.Count <= 1 && active.Blocker != null)
                    {
                        TryAcid(spray, active.Blocker.Value);
                        active.Blocker = null;
                        Dirty(uid, active);
                    }
                }

                _onCollide.SetChain(spawned, active.Chain.Value);

                active.Spawn.RemoveAt(i);
            }

            if (active.Spawn.Count == 0)
                RemCompDeferred<ActiveAcidSprayingComponent>(uid);
        }

        var acidedQuery = EntityQueryEnumerator<SprayAcidedComponent>();
        while (acidedQuery.MoveNext(out var uid, out var acided))
        {
            if (time >= acided.ExpireAt)
            {
                RemCompDeferred<SprayAcidedComponent>(uid);
                continue;
            }

            if (time < acided.NextDamageAt)
                continue;

            acided.NextDamageAt = time + acided.DamageEvery;
            _damageable.TryChangeDamage(uid, acided.Damage, origin: uid);
        }
    }
}
