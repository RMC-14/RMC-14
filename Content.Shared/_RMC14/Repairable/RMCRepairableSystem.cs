using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

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
            return;
        }

        var delay = repairable.Comp.Delay * _skills.GetSkillDelayMultiplier(args.User, repairable.Comp.Skill);

        var ev = new RMCRepairableDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, repairable)
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

        var heal = _rmcDamageable.DistributeTypes(repairable.Owner, repairable.Comp.Heal);
        _damageable.TryChangeDamage(repairable, heal);

        var user = args.User;
        var selfMsg = Loc.GetString("rmc-repairable-finish-self", ("target", repairable));
        var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", user), ("target", repairable));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        _audio.PlayPredicted(repairable.Comp.Sound, repairable, user);
    }
}
