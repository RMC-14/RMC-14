using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids.ClawSharpness;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Repairable;

public sealed class RMCRepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly EntProtoId<SkillDefinitionComponent> ConstructionSkill = "RMCSkillConstruction";

    const string SOLUTION_WELDER = "Welder";
    const string REAGENT_WELDER = "WeldingFuel";

    private EntityUid? _forceRepairUser;
    private EntityUid? _forceRepairTarget;
    private EntityUid? _forceRepairUsed;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCRepairableComponent, InteractUsingEvent>(OnRepairableInteractUsing);
        SubscribeLocalEvent<RMCRepairableComponent, RMCRepairableDoAfterEvent>(OnRepairableDoAfter);
        SubscribeLocalEvent<RMCRepairableComponent, GetVerbsEvent<AlternativeVerb>>(OnRepairableAlternativeVerb);

        SubscribeLocalEvent<NailgunRepairableComponent, InteractUsingEvent>(OnNailgunRepairableInteractUsing);
        SubscribeLocalEvent<NailgunRepairableComponent, RMCNailgunRepairableDoAfterEvent>(OnNailgunRepairableDoAfter);

        SubscribeLocalEvent<ReagentTankComponent, InteractUsingEvent>(OnWelderInteractUsing);
    }

    private void OnRepairableInteractUsing(Entity<RMCRepairableComponent> repairable, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        var hasQuality = _tool.HasQuality(used, repairable.Comp.Quality);
        if (_tags.HasTag(repairable.Owner, WallTag) &&
            hasQuality &&
            !ShouldForceRepair(args.User, repairable.Owner, used))
        {
            return;
        }

        if (!hasQuality)
            return;

        args.Handled = true;

        var user = args.User;
        if (!HasComp<BlowtorchComponent>(used))
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-need-blowtorch"), user, user, PopupType.SmallCaution);
            return;
        }

        var hasReplace = TryComp(repairable, out RMCRepairableReplaceComponent? replace);

        if (!TryComp(repairable, out DamageableComponent? damageable))
            return;

        if (!hasReplace && damageable.TotalDamage <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-not-damaged", ("target", repairable)),
                user,
                user,
                PopupType.SmallCaution);
            return;
        }

        if (repairable.Comp.RepairableDamageLimit > 0 && damageable.TotalDamage > repairable.Comp.RepairableDamageLimit)
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-too-damaged", ("target", repairable)),
                user,
                user,
                PopupType.SmallCaution);
            return;
        }

        if (repairable.Comp.SkillRequired > 0 &&
            !_skills.HasSkill(user, repairable.Comp.Skill, repairable.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-not-trained", ("target", repairable)), user, user, PopupType.SmallCaution);
            return;
        }

        if (!CanRepairPopup(user, repairable))
            return;

        if (!UseFuel(args.Used, args.User, repairable.Comp.FuelUsed, true))
            return;

        var delay = hasReplace ? (float) repairable.Comp.Delay.TotalSeconds : GetWeldRepairDelaySeconds(user, damageable.TotalDamage);
        var ev = new RMCRepairableDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(delay), ev, repairable, used: args.Used)
        {
            NeedHand = true,
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

    private void OnRepairableAlternativeVerb(Entity<RMCRepairableComponent> repairable, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using == null)
            return;

        if (!_tags.HasTag(repairable.Owner, WallTag))
            return;

        if (!_tool.HasQuality(args.Using.Value, repairable.Comp.Quality))
            return;

        if (!HasComp<BlowtorchComponent>(args.Using.Value))
            return;

        var user = args.User;
        var used = args.Using.Value;
        var target = repairable.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-repairable-alt-repair"),
            Priority = 1,
            Act = () => TryAltRepair(user, target, used)
        });
    }

    private void TryAltRepair(EntityUid user, EntityUid target, EntityUid used)
    {
        if (!_net.IsServer)
            return;

        _forceRepairUser = user;
        _forceRepairTarget = target;
        _forceRepairUsed = used;

        try
        {
            var interact = new InteractUsingEvent(user, used, target, Transform(target).Coordinates);
            RaiseLocalEvent(target, interact, true);
        }
        finally
        {
            _forceRepairUser = null;
            _forceRepairTarget = null;
            _forceRepairUsed = null;
        }
    }

    private bool ShouldForceRepair(EntityUid user, EntityUid target, EntityUid used)
    {
        return _forceRepairUser == user && _forceRepairTarget == target && _forceRepairUsed == used;
    }

    private void OnRepairableDoAfter(Entity<RMCRepairableComponent> repairable, ref RMCRepairableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!CanRepairPopup(args.User, repairable))
            return;

        if (args.Used == null ||
            !UseFuel(args.Used.Value, args.User, repairable.Comp.FuelUsed))
        {
            return;
        }

        var user = args.User;
        if (TryComp(repairable, out RMCRepairableReplaceComponent? replace))
        {
            var selfMsg = Loc.GetString("rmc-repairable-finish-self", ("target", repairable));
            var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", user), ("target", repairable));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
            _audio.PlayPredicted(repairable.Comp.Sound, repairable, user);

            if (_net.IsServer)
            {
                var xform = Transform(repairable);
                var coords = xform.Coordinates;
                var rotation = xform.LocalRotation;
                var anchored = xform.Anchored;

                var restored = Spawn(replace.Prototype, coords);
                var restoredXform = Transform(restored);
                restoredXform.LocalRotation = rotation;
                restoredXform.Anchored = anchored;
                QueueDel(repairable);
            }

            return;
        }

        var heal = -_rmcDamageable.DistributeTypesTotal(repairable.Owner, repairable.Comp.Heal);
        _damageable.TryChangeDamage(repairable, heal, true);

        var repairSelfMsg = Loc.GetString("rmc-repairable-finish-self", ("target", repairable));
        var repairOthersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", user), ("target", repairable));
        _popup.PopupPredicted(repairSelfMsg, repairOthersMsg, user, user);
        _audio.PlayPredicted(repairable.Comp.Sound, repairable, user);

        if (TryComp(repairable, out DamageableComponent? damageable) &&
            damageable.TotalDamage > FixedPoint2.Zero &&
            heal.GetTotal() != FixedPoint2.Zero)
        {
            if (args.Used is not { } used)
                return;

            var delay = TimeSpan.FromSeconds(GetWeldRepairDelaySeconds(args.User, damageable.TotalDamage));
            var ev = new RMCRepairableDoAfterEvent();
            var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, repairable, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
                BlockDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameEvent
            };

            _doAfter.TryStartDoAfter(doAfter);
        }
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

        if (solution.GetTotalPrototypeQuantity(REAGENT_WELDER) == 0 || solution.GetTotalPrototypeQuantity(REAGENT_WELDER) < fuelUsed)
        {
            _popup.PopupClient(Loc.GetString("welder-component-no-fuel-message"), user, PopupType.SmallCaution);
            return false;
        }

        if (!attempt && _net.IsServer)
        {
            _solution.RemoveReagent(solutionComp.Value, REAGENT_WELDER, fuelUsed);
            Dirty(solutionComp.Value);
        }

        return true;
    }

    private void OnNailgunRepairableInteractUsing(Entity<NailgunRepairableComponent> repairable,
        ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;

        var user = args.User;

        if (!TryComp(used, out NailgunComponent? nailgunComp))
            return;

        if (!TryComp(user, out HandsComponent? handsComp))
            return;

        args.Handled = true;
        if (!TryComp(repairable, out DamageableComponent? damageable) ||
            damageable.TotalDamage <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("rmc-repairable-not-damaged", ("target", repairable)), user, user, PopupType.SmallCaution);
            return;
        }

        var getAmmoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(used, ref getAmmoCountEv);

        if (getAmmoCountEv.Count < 4)
        {
            _popup.PopupClient(Loc.GetString("rmc-nailgun-no-nails-message"), user, PopupType.SmallCaution);
            return;
        }

        var repairValue = GetRepairValue(repairable, (user, handsComp), nailgunComp, out EntityUid? held);

        if (held == null || repairValue <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("rmc-nailgun-no-material-message",  ("target", repairable)), user, PopupType.SmallCaution);
            return;
        }

        var delay = nailgunComp.NailingSpeed;

        var ev = new RMCNailgunRepairableDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, repairable, used: args.Used)
        {
            NeedHand = true,
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

    private void OnNailgunRepairableDoAfter(Entity<NailgunRepairableComponent> repairable,
        ref RMCNailgunRepairableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        //Check conditions again
        if (args.Used is not {} used)
            return;

        var user = args.User;

        if (!TryComp(used, out NailgunComponent? nailgunComponent))
            return;

        var getAmmoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(used, ref getAmmoCountEv);

        if (getAmmoCountEv.Count < 4)
        {
            _popup.PopupClient(Loc.GetString("rmc-nailgun-no-nails-message"), user, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(user, out HandsComponent? handsComp))
            return;

        var repairValue = GetRepairValue(repairable, (user, handsComp), nailgunComponent, out EntityUid? held);
        if (held == null || repairValue <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("rmc-nailgun-lost-stack"), user, PopupType.SmallCaution);
            return;
        }

        //Checks passed, do repair actions
        var heal = -_rmcDamageable.DistributeTypesTotal(repairable.Owner, repairValue);
        _damageable.TryChangeDamage(repairable, heal, true);

        if (TryComp(held, out StackComponent? stack))
        {
            _stack.SetCount((EntityUid)held, stack.Count - nailgunComponent.MaterialPerRepair);
        }

        var ammo = new List<(EntityUid? Entity, IShootable Shootable)>();
        var ev = new TakeAmmoEvent(4, ammo, Transform(user).Coordinates, user);
        RaiseLocalEvent(used, ev);

        foreach (var (bullet, _) in ev.Ammo)
        {
            QueueDel(bullet);
        }

        var selfMsg = Loc.GetString("rmc-nailgun-finish-self", ("material", held), ("target", repairable));
        var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", user), ("material", held), ("target", repairable));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        _audio.PlayPredicted(nailgunComponent.RepairSound, repairable, user);
    }

    private float GetRepairValue(Entity<NailgunRepairableComponent> repairable,
        Entity<HandsComponent?> hands,
        NailgunComponent nailgunComponent,
        out EntityUid? heldStack)
    {
        heldStack = null;
        if (!TryGetNailgunRepairStack(hands, nailgunComponent.MaterialPerRepair, out var stackUid, out var stackComponent))
            return 0;

        heldStack = stackUid;

        var stackType = stackComponent.StackTypeId;
        if (_tags.HasTag(repairable.Owner, WallTag))
        {
            if (!TryComp(repairable.Owner, out ReceiverXenoClawsComponent? receiver))
                return 0;

            return GetWallNailgunRepairValue(stackType, receiver.MaxHealth);
        }

        if (stackType == "CMSteel")
            return repairable.Comp.RepairMetal;

        if (stackType == "CMPlasteel")
            return repairable.Comp.RepairPlasteel;

        if (stackType == "RMCWood")
            return repairable.Comp.RepairWood;

        return 0;
    }

    private float GetWeldRepairDelaySeconds(EntityUid user, FixedPoint2 totalDamage)
    {
        var multiplier = _skills.GetSkillDelayMultiplier(user, ConstructionSkill);
        var damage = totalDamage.Float();
        var scaled = MathF.Floor(damage / 200f) * multiplier;
        return MathF.Max(2f, scaled);
    }

    private float GetWallNailgunRepairValue(string stackType, float damageCap)
    {
        if (damageCap <= 0)
            return 0;

        if (stackType == "RMCWood")
            return damageCap * 0.075f;

        if (stackType == "CMSteel")
            return damageCap * 0.15f;

        if (stackType == "CMPlasteel")
            return damageCap * 0.3f;

        return 0;
    }

    public bool TryGetNailgunRepairStack(Entity<HandsComponent?> hands, int required, out EntityUid stackUid, out StackComponent stack)
    {
        stackUid = default;
        stack = default!;

        foreach (var held in _hands.EnumerateHeld(hands))
        {
            if (!TryComp(held, out StackComponent? stackComponent))
                continue;

            if (!IsValidNailgunStack(stackComponent))
                continue;

            if (stackComponent.Count < required)
                continue;

            stackUid = held;
            stack = stackComponent;
            return true;
        }

        return false;
    }

    private static bool IsValidNailgunStack(StackComponent stackComponent)
    {
        return stackComponent.StackTypeId == "CMSteel" ||
               stackComponent.StackTypeId == "CMPlasteel" ||
               stackComponent.StackTypeId == "RMCWood";
    }

    private void OnWelderInteractUsing(Entity<ReagentTankComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        var target = args.Target;

        if (!TryComp<WelderComponent>(used, out var welder))
            return;

        if (ent.Comp.TankType == ReagentTankType.Fuel
            && _solution.TryGetDrainableSolution(target, out var targetSoln, out var targetSolution)
            && _solution.TryGetSolution(used, welder.FuelSolutionName, out var solutionComp, out var welderSolution))
        {
            var trans = FixedPoint2.Min(welderSolution.AvailableVolume, targetSolution.Volume);

            if (welder.Enabled)
            {
                _popup.PopupClient(Loc.GetString("rmc-welder-component-danger"), used, args.User, PopupType.MediumCaution);
            }
            else if (trans > 0)
            {
                var drained = _solution.Drain(target, targetSoln.Value, trans);
                _solution.TryAddSolution(solutionComp.Value, drained);
                _audio.PlayPredicted(welder.WelderRefill, used, user: args.User);
                _popup.PopupClient(Loc.GetString("welder-component-after-interact-refueled-message"), used, args.User);
            }
            else if (welderSolution.AvailableVolume <= 0)
            {
                _popup.PopupClient(Loc.GetString("welder-component-already-full"), used, args.User);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("welder-component-no-fuel-in-tank", ("owner", args.Target)), used, args.User);
            }

            args.Handled = true;
        }
    }

    private bool CanRepairPopup(EntityUid user, EntityUid target)
    {
        var ev = new RMCRepairableTargetAttemptEvent(user, target);
        RaiseLocalEvent(target, ref ev);
        if (!ev.Cancelled)
            return true;

        _popup.PopupClient(ev.Popup, user, user, PopupType.MediumCaution);
        return false;
    }
}
