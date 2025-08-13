using Content.Shared._RMC14.IdentityManagement;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.StatusEffect;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Synth;

public abstract class SharedSynthSystem : EntitySystem
{
    [Dependency] private readonly RMCRepairableSystem _repairable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly RMCStatusEffectSystem _rmcStatusEffects = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SynthComponent, AttackAttemptEvent>(OnMeleeAttempted);
        SubscribeLocalEvent<SynthComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<SynthComponent, TryingToSleepEvent>(OnSleepAttempt);
        SubscribeLocalEvent<SynthComponent, InteractUsingEvent>(OnSynthInteractUsing);
        SubscribeLocalEvent<SynthComponent, RMCSynthRepairEvent>(OnSynthRepairDoAfter);

        SubscribeLocalEvent<UseOnSynthBlockedComponent, BeforeRangedInteractEvent>(OnSynthBlockedBeforeRangedInteract);
    }

    private void OnMapInit(Entity<SynthComponent> ent, ref MapInitEvent args)
    {
        MakeSynth(ent);
    }

    protected virtual void MakeSynth(Entity<SynthComponent> ent)
    {
        if (ent.Comp.AddComponents != null)
            EntityManager.AddComponents(ent.Owner, ent.Comp.AddComponents);

        if (ent.Comp.RemoveComponents != null)
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.RemoveComponents);

        if (ent.Comp.StunResistance != null)
            _rmcStatusEffects.GiveStunResistance(ent.Owner, ent.Comp.StunResistance.Value);

        if (TryComp<FixedIdentityComponent>(ent.Owner, out var fixedIdentity))
        {
            fixedIdentity.Name = ent.Comp.FixedIdentityReplacement;
            Dirty(ent.Owner, fixedIdentity);
        }

        if (TryComp<MobThresholdsComponent>(ent.Owner, out var thresholds))
            _mobThreshold.SetMobStateThreshold(ent.Owner, ent.Comp.CritThreshold, MobState.Critical, thresholds);

        if (TryComp<RMCHealthIconsComponent>(ent.Owner, out var healthIcons))
        {
            healthIcons.Icons = ent.Comp.HealthIconOverrides;
            Dirty(ent.Owner, healthIcons);
        }

        RemCompDeferred<RMCRevivableComponent>(ent.Owner);
        RemCompDeferred<SlowOnDamageComponent>(ent.Owner);
    }

    private void OnMeleeAttempted(Entity<SynthComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Owner != args.Uid)
            return;

        if (ent.Comp.CanUseMeleeWeapons)
            return;

        if (args.Weapon == null)
            return;

        args.Cancel();
        DoSynthUnableToUsePopup(ent, args.Weapon.Value.Owner);
    }

    private void OnShotAttempted(Entity<SynthComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.CanUseGuns)
            return;

        args.Cancel();
        DoSynthUnableToUsePopup(ent, args.Used);
    }

    private void OnSleepAttempt(Entity<SynthComponent> ent, ref TryingToSleepEvent args)
    {
        args.Cancelled = true; // Synths dont sleep
    }

    private void OnSynthInteractUsing(Entity<SynthComponent> synth, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // TODO
        // When limb damage is released, make this system re-used for prosthetic limbs. They use the exact same values in CM13.
        // Give synths robot limbs

        var used = args.Used;
        var user = args.User;
        var selfRepair = args.User == synth.Owner;

        var ev = new RMCSynthRepairEvent();
        var repairTime = selfRepair ? synth.Comp.SelfRepairTime : synth.Comp.RepairTime;
        var doAfter = new DoAfterArgs(EntityManager, user, repairTime, ev, synth, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (HasComp<BlowtorchComponent>(used) && _tool.HasQuality(used, synth.Comp.RepairQuality))
        {
            if (HasDamage(synth, synth.Comp.WelderDamageGroup) && _repairable.UseFuel(args.Used, args.User, 5, true))
            {
                args.Handled = true;

                if (_doAfter.TryStartDoAfter(doAfter))
                {
                    var selfMsg = Loc.GetString("rmc-synth-repair-brute-start-self", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
                    var othersMsg = Loc.GetString("rmc-synth-repair-brute-start-others", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));

                    if (!selfRepair)
                        return;

                    _popup.PopupPredicted(selfMsg, othersMsg, user, user);
                }
            }
            else
            {
                _popup.PopupClient(Loc.GetString("rmc-repairable-not-damaged", ("target", synth)), user, user, PopupType.SmallCaution);
            }
        }
        else if (HasComp<RMCCableCoilComponent>(used))
        {
            args.Handled = true;

            if (HasDamage(synth, synth.Comp.CableCoilDamageGroup))
            {
                if (_doAfter.TryStartDoAfter(doAfter))
                {
                    var selfMsg = Loc.GetString("rmc-synth-repair-burn-start-self", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
                    var othersMsg = Loc.GetString("rmc-synth-repair-burn-start-others", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));

                    if (!selfRepair)
                        return;

                    _popup.PopupPredicted(selfMsg, othersMsg, user, user);
                }
            }
            else
            {
                _popup.PopupClient(Loc.GetString("rmc-repairable-not-damaged", ("target", synth)), user, user, PopupType.SmallCaution);
            }
        }
    }

    private void OnSynthRepairDoAfter(Entity<SynthComponent> synth, ref RMCSynthRepairEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var used = args.Used;
        var user = args.User;

        if (used == null)
            return;

        if (HasComp<BlowtorchComponent>(used) && _repairable.UseFuel(used.Value, user, 5))
        {
            if (synth.Comp.WelderDamageToRepair != null)
                _damageable.TryChangeDamage(synth, synth.Comp.WelderDamageToRepair, true, false, origin: user);

            var selfMsg = Loc.GetString("rmc-synth-repair-brute-finish-self", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
            var othersMsg = Loc.GetString("rmc-synth-repair-brute-finish", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
        else if (HasComp<RMCCableCoilComponent>(args.Used) && _stack.Use(args.Used.Value, 1))
        {
            if (synth.Comp.CableCoilDamageToRepair != null)
                _damageable.TryChangeDamage(synth, synth.Comp.CableCoilDamageToRepair, true, false, origin: args.User);

            var selfMsg = Loc.GetString("rmc-synth-repair-burn-finish-self", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
            var othersMsg = Loc.GetString("rmc-synth-repair-burn-finish", ("user", user), ("target", synth), ("tool", used), ("limb", "chest"));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnSynthBlockedBeforeRangedInteract(Entity<UseOnSynthBlockedComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach)
            return;

        if (args.Target == null)
            return;

        if (!_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return; // Whitelist is so you dont get the popup by clicking on a random object

        if (HasComp<SynthComponent>(args.Target) && !ent.Comp.Reversed)
            args.Handled = true;
        else if (!HasComp<SynthComponent>(args.Target) && ent.Comp.Reversed)
            args.Handled = true;

        if (args.Handled)
        {
            var msg = Loc.GetString(ent.Comp.Popup, ("user", args.User), ("used", args.Used), ("target", args.Target));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
        }
    }

    public bool HasDamage(EntityUid synth, ProtoId<DamageGroupPrototype> group)
    {
        if (!TryComp<DamageableComponent>(synth, out var damageable))
            return false;

        if (damageable.Damage.Empty)
            return false;

        var damage = damageable.Damage.GetDamagePerGroup(_prototypes);
        var groupDmg = damage.GetValueOrDefault(group);

        if (groupDmg <= FixedPoint2.Zero)
            return false;

        return true;
    }

    public void DoSynthUnableToUsePopup(EntityUid synth, EntityUid tool)
    {
        var msg = Loc.GetString("rmc-species-synth-programming-prevents-use", ("user", synth), ("tool", tool));
        _popup.PopupClient(msg, synth, synth, PopupType.SmallCaution);
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCSynthRepairEvent : SimpleDoAfterEvent;
