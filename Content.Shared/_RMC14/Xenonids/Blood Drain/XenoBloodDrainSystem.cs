using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Blood_Drain;

public sealed class XenoBloodDrainSystem : EntitySystem
{
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCPullingSystem _rmcpulling = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcblood = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly XenoEvolutionSystem _evo = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBloodDrainComponent, XenoBloodDrainActionEvent>(OnXenoBloodDrainAction);
        SubscribeLocalEvent<XenoBloodDrainComponent, DoAfterAttemptEvent<XenoBloodDrainDoafter>>(OnXenoBloodDrainDoafterAttempt);
        SubscribeLocalEvent<XenoBloodDrainComponent, XenoBloodDrainDoafter>(OnXenoBloodDrainDoafter);
    }

    private void OnXenoBloodDrainAction(Entity<XenoBloodDrainComponent> xeno, ref XenoBloodDrainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (!CanDrain(xeno, args.Target))
            return;

        args.Handled = true;

        var ev = new XenoBloodDrainDoafter();
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.DrainTime, ev, xeno, args.Target)
        {
            BreakOnMove = true,
            BreakOnRest = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BlockDuplicate = true,
            ForceVisible = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doAfter);

        _popup.PopupEntity(Loc.GetString("rmc-xeno-blood-drain-target", ("user", Identity.Name(xeno, EntityManager, args.Target))), args.Target, args.Target, PopupType.MediumCaution);

    }

    private void OnXenoBloodDrainDoafterAttempt(Entity<XenoBloodDrainComponent> xeno, ref DoAfterAttemptEvent<XenoBloodDrainDoafter> args)
    {
        if (args.Event.Target != null && CanDrain(xeno, args.Event.Target.Value))
            return;

        args.Cancel();
    }

    private void OnXenoBloodDrainDoafter(Entity<XenoBloodDrainComponent> xeno, ref XenoBloodDrainDoafter args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target == null || !CanDrain(xeno, args.Target.Value))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var damage = _damage.TryChangeDamage(args.Target.Value, xeno.Comp.BiteDamage, true, origin: xeno, tool: xeno);
        if (damage?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(args.Target.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { args.Target.Value }, filter);
        }

        _xenoHeal.CreateHealStacks(xeno, xeno.Comp.Healing, TimeSpan.Zero, 1, TimeSpan.Zero);

        if (_net.IsServer)
        {
            SpawnAttachedTo(xeno.Comp.HealEffect, xeno.Owner.ToCoordinates());
            SpawnAttachedTo(xeno.Comp.BiteEffect, args.Target.Value.ToCoordinates());
            _audio.PlayPvs(xeno.Comp.DrainSound, xeno);
        }

        if (_rmcblood.TryGetBloodSolution(args.Target.Value, out var blood))
            blood.RemoveSolution(xeno.Comp.BloodDrain);

        var evoBonus = FixedPoint2.Zero;
        var bonuses = EntityQueryEnumerator<EvolutionBonusComponent>();
        while (bonuses.MoveNext(out var comp))
        {
            evoBonus += comp.Amount * xeno.Comp.BonusEvoMult;
        }

        var query = EntityQueryEnumerator<XenoEvolutionComponent>();
        while (query.MoveNext(out var uid, out var evo))
        {
            if (!_hive.FromSameHive(xeno.Owner, uid))
                continue;

            if (_mob.IsDead(uid))
                continue;

            _evo.AddPointsCapped((uid, evo), xeno.Comp.BaseEvoPointsGranted + evoBonus);
        }

        args.Repeat = true;

    }

    private bool CanDrain(Entity<XenoBloodDrainComponent> xeno, EntityUid target)
    {
        if (!_xeno.CanAbilityAttackTarget(xeno, target))
            return false;

        if (!_rmcpulling.IsPulling(xeno.Owner, target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-blood-drain-pull", ("target", Identity.Name(target, EntityManager, xeno))), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (!_standing.IsDown(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-blood-drain-down", ("target", Identity.Name(target, EntityManager, xeno))), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<SynthComponent>(target) || HasComp<XenoComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-blood-drain-bad-blood", ("target", Identity.Name(target, EntityManager, xeno))), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<VictimInfectedComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-blood-drain-infected-blood", ("target", Identity.Name(target, EntityManager, xeno))), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (!_rmcblood.TryGetBloodSolution(target, out var blood) || blood.Volume < xeno.Comp.BloodDrain)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-blood-drain-no-blood", ("target", Identity.Name(target, EntityManager, xeno))), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        return true;
    }
}
