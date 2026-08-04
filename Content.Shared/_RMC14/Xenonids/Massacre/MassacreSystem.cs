using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Gibbing;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Gut;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Massacre;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids;

public sealed class MassacreSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCGibSystem _rmcGib = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoMassacreComponent, XenoMassacreActionEvent>(OnXenoMassacreAction);
        SubscribeLocalEvent<XenoMassacreComponent, DoAfterAttemptEvent<XenoMassacreDoafterEvent>>(OnXenoMassacreDoafterAttempt);
        SubscribeLocalEvent<XenoMassacreComponent, XenoMassacreDoafterEvent>(OnXenoMassacreDoafter);
    }

    private void OnXenoMassacreAction(Entity<XenoMassacreComponent> xeno, ref XenoMassacreActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoGutAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (_net.IsClient)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        List<(EntityUid ent, EntityUid effect)> gibs = new();

        foreach (var ent in _lookup.GetEntitiesInRange<MarineComponent>(args.Target, xeno.Comp.GibRange))
        {
            if (!CanGib(xeno, args.Target, ent))
                continue;

            gibs.Add((ent, SpawnAttachedTo(xeno.Comp.Effects, ent.Owner.ToCoordinates())));
        }

        if (gibs.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-massacre-no-targets"), xeno, xeno, PopupType.MediumCaution);
            return;
        }

        xeno.Comp.Targets = gibs;

        var ev = new XenoMassacreDoafterEvent(GetNetCoordinates(args.Target));

        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfter);

        foreach (var gib in gibs)
        {
            _jitter.DoJitter(gib.ent, xeno.Comp.Delay, true, 14f, 5f, true);
        }


        _popup.PopupEntity(Loc.GetString("rmc-xeno-massacre-start-self"), xeno, xeno, PopupType.LargeCaution);

        foreach (var session in Filter.PvsExcept(xeno, entityManager: EntityManager).Recipients)
        {
            if (session.AttachedEntity is not { } viewer)
                continue;

            _popup.PopupEntity(Loc.GetString("rmc-xeno-massacre-start-others", ("user", Identity.Name(xeno, EntityManager, viewer))), xeno, session, PopupType.LargeCaution);
        }
    }

    private void OnXenoMassacreDoafterAttempt(Entity<XenoMassacreComponent> xeno, ref DoAfterAttemptEvent<XenoMassacreDoafterEvent> args)
    {
        if (_net.IsClient)
            return;

        var coords = GetCoordinates(args.Event.Coordinates);
        List<(EntityUid ent, EntityUid effect)> possibleRemovals = new();

        foreach (var gib in xeno.Comp.Targets)
        {
            var (ent, effect) = gib;

            if (CanGib(xeno, coords, ent))
                continue;

            possibleRemovals.Add(gib);
        }

        foreach (var remove in possibleRemovals)
        {
            _statusEffects.TryRemoveStatusEffect(remove.ent, "Jitter");
            QueueDel(remove.effect);
            xeno.Comp.Targets.Remove(remove);
        }

        if (xeno.Comp.Targets.Count == 0)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("rmc-xeno-massacre-no-targets"), xeno, xeno, PopupType.MediumCaution);
        }
    }

    private void OnXenoMassacreDoafter(Entity<XenoMassacreComponent> xeno, ref XenoMassacreDoafterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            foreach (var target in xeno.Comp.Targets)
            {
                _statusEffects.TryRemoveStatusEffect(target.ent, "Jitter");
                QueueDel(target.effect);
            }
            xeno.Comp.Targets.Clear();
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        if (_net.IsServer)
        {
            var coords = GetCoordinates(args.Coordinates);
            foreach (var (gib, effect) in xeno.Comp.Targets)
            {
                if (!CanGib(xeno, coords, gib))
                    continue;

                if (!TryComp<BodyComponent>(gib, out var body))
                    return;

                _rmcGib.ScatterInventoryItems(gib);
                _bodySystem.GibBody(gib, true, body);
                _audio.PlayPvs(xeno.Comp.Sound, xeno);

                if (_hive.GetHive(xeno.Owner) is not { } hive)
                    continue;

                _hive.ChangeBurrowedLarva(hive, xeno.Comp.BurrowedPerGib);
            }
        }

        xeno.Comp.Targets.Clear();

        _popup.PopupClient(Loc.GetString("rmc-xeno-massacre-end-self"), xeno, xeno, PopupType.LargeCaution);

        foreach (var session in Filter.PvsExcept(xeno, entityManager: EntityManager).Recipients)
        {
            if (session.AttachedEntity is not { } viewer)
                continue;

            _popup.PopupEntity(Loc.GetString("rmc-xeno-massacre-end-others", ("user", Identity.Name(xeno, EntityManager, viewer))), xeno, session, PopupType.LargeCaution);
        }

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoMassacreActionEvent>(xeno))
        {
            _actions.SetIfBiggerCooldown(action.AsNullable(), xeno.Comp.Cooldown);
        }
    }

    private bool CanGib(Entity<XenoMassacreComponent> xeno, EntityCoordinates coords, EntityUid target)
    {
        if (HasComp<XenoNestedComponent>(target) ||
            !HasComp<BodyComponent>(target) ||
            HasComp<XenoComponent>(target) ||
            HasComp<SynthComponent>(target) ||
            HasComp<VictimBurstComponent>(target) ||
            !_mob.IsDead(target))
            return false;

        if (!_interact.InRangeUnobstructed(_transform.ToMapCoordinates(coords), target, xeno.Comp.GibRange) ||
            !_interact.InRangeUnobstructed(xeno.Owner, target, xeno.Comp.GibRange * 2))
            return false;

        return true;
    }
}
