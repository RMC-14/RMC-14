using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Repairable;

public sealed class RMCRepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly INetManager _net = default!;

    const string SOLUTION_WELDER = "Welder";
    const string REAGANT_WELDER = "WeldingFuel";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCRepairableComponent, InteractUsingEvent>(OnRepairableInteractUsing);
        SubscribeLocalEvent<RMCRepairableComponent, RMCRepairableDoAfterEvent>(OnRepairableDoAfter);
    }

    private void OnRepairableInteractUsing(Entity<RMCRepairableComponent> repairable, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (!_tool.HasQuality(used, repairable.Comp.Quality))
            return;

        args.Handled = true;

        var user = args.User;
        if (!HasComp<BlowtorchComponent>(used))
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-need-blowtorch"), user, user, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(repairable, out DamageableComponent? damageable) ||
            damageable.TotalDamage <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-not-damaged", ("target", repairable)), user, user, PopupType.SmallCaution);
            return;
        }

        if (repairable.Comp.SkillRequired > 0 &&
            !_skills.HasSkill(user, repairable.Comp.Skill, repairable.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-not-trained", ("target", repairable)), user, user, PopupType.SmallCaution);
            return;
        }

        if (!UseFuel(args.Used, args.User, repairable.Comp.FuelUsed, true))
            return;

        var delay = repairable.Comp.Delay * _skills.GetSkillDelayMultiplier(args.User, repairable.Comp.Skill);

        var ev = new RMCRepairableDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, repairable, used: args.Used)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-repairable-start-self", ("target", repairable));
            var othersMsg = Loc.GetString("rmc-repairable-start-others", ("user", user), ("target", repairable));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnRepairableDoAfter(Entity<RMCRepairableComponent> repairable, ref RMCRepairableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used == null || !UseFuel(args.Used.Value, args.User, repairable.Comp.FuelUsed))
            return;

        var heal = _rmcDamageable.DistributeTypes(repairable.Owner, repairable.Comp.Heal);
        _damageable.TryChangeDamage(repairable, heal);

        var user = args.User;
        var selfMsg = Loc.GetString("rmc-repairable-finish-self", ("target", repairable));
        var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", user), ("target", repairable));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        _audio.PlayPredicted(repairable.Comp.Sound, repairable, user);
    }

    public bool UseFuel(EntityUid tool, EntityUid user, FixedPoint2 fuelUsed, bool attempt = false)
    {
        if (!TryComp<SolutionContainerManagerComponent>(tool, out var welderCon))
            return false;

        if (!TryComp<ItemToggleComponent>(tool, out var toggle) || !toggle.Activated)
        {
            _popup.PopupClient(Loc.GetString("welder-component-welder-not-lit-message"), user, PopupType.SmallCaution);
            return false;
        }

        if (!_solution.TryGetSolution((tool, welderCon), SOLUTION_WELDER, out var solutionComp, out var solution))
            return false;

        if (solution.GetTotalPrototypeQuantity(REAGANT_WELDER) == 0 || solution.GetTotalPrototypeQuantity(REAGANT_WELDER) < fuelUsed)
        {
            _popup.PopupClient(Loc.GetString("welder-component-no-fuel-message"), user, PopupType.SmallCaution);
            return false;
        }

        if (!attempt && _net.IsServer)
        {
            _solution.RemoveReagent(solutionComp.Value, REAGANT_WELDER, fuelUsed);
            Dirty(solutionComp.Value);
        }

        return true;
    }
}
