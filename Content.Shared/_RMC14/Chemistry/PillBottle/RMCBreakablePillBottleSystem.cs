using System.Linq;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Storage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.PillBottle;

public sealed class RMCBreakablePillBottleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, RMCPillBottleBreakDoAfterEvent>(OnBreakDoAfter);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, RMCPillBottleRepairDoAfterEvent>(OnRepairDoAfter);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt, before: [typeof(RMCStorageSystem)]);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, RMCStorageEjectHandItemEvent>(OnStorageEjectHand);
        SubscribeLocalEvent<RMCBreakablePillBottleComponent, DumpableDoAfterEvent>(OnDumpableDoAfter, before: [typeof(DumpableSystem), typeof(RMCStorageSystem)]);
    }

    private void OnMapInit(Entity<RMCBreakablePillBottleComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, RMCPillBottleVisuals.Broken, ent.Comp.Broken);
    }

    private void OnExamined(Entity<RMCBreakablePillBottleComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Broken)
            return;

        using (args.PushGroup(nameof(RMCBreakablePillBottleComponent)))
        {
            args.PushMarkup("[color=red]The top has been broken open. It is unusable like this.[/color]");
            args.PushMarkup("[color=cyan]If you have the know-how, you'd probably be able to bend the top back into place with something that can pinch it.[/color]");
        }
    }

    private void OnInteractUsing(Entity<RMCBreakablePillBottleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.BreakWhitelist, args.Used))
        {
            args.Handled = true;
            TryStartBreak(ent, args.User, args.Used);
            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.RepairWhitelist, args.Used))
        {
            args.Handled = true;
            TryStartRepair(ent, args.User, args.Used);
        }
    }

    private void TryStartBreak(Entity<RMCBreakablePillBottleComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_hands.IsHolding(user, ent))
        {
            _popup.PopupClient($"The {Name(ent)} must be in your hand to do that.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Broken)
        {
            _popup.PopupClient($"The {Name(ent)} has already been broken open!", ent, user, PopupType.SmallCaution);
            return;
        }

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.BreakDelay, new RMCPillBottleBreakDoAfterEvent(), ent, ent, used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        var selfMsg = $"You place the tip of the {Name(used)} underneath the lid of the {Name(ent)} and begin torquing the lid off";
        selfMsg += CanOpenNormally(ent, user) ? ", though you could probably just open it normally." : ".";

        _popup.PopupPredicted(
            selfMsg,
            $"{Identity.Name(user, EntityManager)} places the tip of the {Name(used)} underneath the lid of the {Name(ent)} and begins torquing the lid off...",
            ent,
            user);

        _audio.PlayPredicted(ent.Comp.StartSound, ent, user);
    }

    private void OnBreakDoAfter(Entity<RMCBreakablePillBottleComponent> ent, ref RMCPillBottleBreakDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (args.Cancelled)
        {
            _popup.PopupClient($"You stop forcing the lid off of the {Name(ent)}.", ent, user);
            return;
        }

        if (ent.Comp.Broken)
            return;

        SetBroken(ent, true);
        _ui.CloseUi(ent.Owner, StorageComponent.StorageUiKey.Key);

        var userName = Identity.Name(user, EntityManager);
        if (TryComp(ent, out StorageComponent? storage) && storage.Container.ContainedEntities.Count > 0)
        {
            _popup.PopupPredicted(
                $"You pop the lid off of the {Name(ent)}, spilling some of its contents everywhere!",
                $"{userName} pops the lid off of the {Name(ent)}, spilling some of its contents everywhere!",
                ent,
                user,
                PopupType.SmallCaution);

            _audio.PlayPredicted(ent.Comp.SpillSound, ent, user);

            if (_net.IsServer)
                SpillContents(ent, storage, user);
        }
        else
        {
            _popup.PopupPredicted(
                $"You pop the lid off of the {Name(ent)}, breaking it in the process.",
                $"{userName} pops the lid off of the {Name(ent)}.",
                ent,
                user);
        }

        _audio.PlayPredicted(ent.Comp.BreakSound, ent, user);
    }

    private void SpillContents(Entity<RMCBreakablePillBottleComponent> ent, StorageComponent storage, EntityUid user)
    {
        var toSpill = storage.Container.ContainedEntities.Take(ent.Comp.SpillAmount).ToList();
        foreach (var item in toSpill)
        {
            if (!_container.Remove(item, storage.Container))
                continue;

            _transform.DropNextTo(item, user);

            var distance = 0;
            for (var i = 0; i < ent.Comp.SpillMaxDistance; i++)
            {
                if (_random.Prob(ent.Comp.SpillStepChance))
                    distance++;
            }

            var direction = _random.NextAngle().ToVec();
            var scatter = _random.NextVector2Box(ent.Comp.SpillScatter, ent.Comp.SpillScatter);
            _throwing.TryThrow(item, direction * distance + scatter, user: user, pushbackRatio: 0, recoil: false, playSound: false);
        }
    }

    private void TryStartRepair(Entity<RMCBreakablePillBottleComponent> ent, EntityUid user, EntityUid used)
    {
        if (!_hands.IsHolding(user, ent))
        {
            _popup.PopupClient($"The {Name(ent)} must be in your hand to do that.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!ent.Comp.Broken)
        {
            _popup.PopupClient($"The {Name(ent)} isn't broken, there's no need to fix it.", ent, user, PopupType.SmallCaution);
            return;
        }

        var skill = _skills.GetSkill(user, ent.Comp.RepairSkill);
        if (skill < 1 || ent.Comp.RepairDelays.Count == 0)
        {
            _popup.PopupClient("You have no idea how to fix this thing.", ent, user, PopupType.SmallCaution);
            return;
        }

        var tier = Math.Min(skill, ent.Comp.RepairDelays.Count);
        var delay = ent.Comp.RepairDelays[tier - 1];
        var args = new DoAfterArgs(EntityManager, user, delay, new RMCPillBottleRepairDoAfterEvent(), ent, ent, used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        var userName = Identity.Name(user, EntityManager);
        var (selfMsg, othersMsg) = tier switch
        {
            1 => ($"After fiddling with it for a moment, you grip the end of the bent lid of the {Name(ent)} with the {Name(used)} and begin bending the plastic rim into useable form.",
                $"{userName} fiddles with the {Name(ent)} for a moment, before bending the rim back into place with the {Name(used)}."),
            2 => ($"You precisely place the tip of the {Name(used)} on the bent rim of the {Name(ent)} and begin bending it back into place.",
                $"{userName} calmly grips the {Name(ent)} with the tip of the {Name(used)}, and begins bending the rim back into place."),
            _ => ($"In one smooth motion, you grip the rim of the {Name(ent)} with the {Name(used)} and bend it back into place.",
                $"In one smooth motion, {userName} grips the bent rim of the {Name(ent)} with the {Name(used)} and bends it back into place."),
        };

        _popup.PopupPredicted(selfMsg, othersMsg, ent, user);

        _audio.PlayPredicted(ent.Comp.StartSound, ent, user);
    }

    private void OnRepairDoAfter(Entity<RMCBreakablePillBottleComponent> ent, ref RMCPillBottleRepairDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (args.Cancelled)
        {
            _popup.PopupClient($"You stop fixing the {Name(ent)}.", ent, user);
            return;
        }

        if (!ent.Comp.Broken)
            return;

        SetBroken(ent, false);
        _popup.PopupPredicted(
            $"You succeed in fixing the {Name(ent)}.",
            $"{Identity.Name(user, EntityManager)} successfully bends the rim of the {Name(ent)} back into place, fixing it.",
            ent,
            user);
    }

    private void OnStorageInteractAttempt(Entity<RMCBreakablePillBottleComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.Broken)
            return;

        args.Cancelled = true;
        if (!args.Silent)
            PopupBroken(ent, args.User);
    }

    private void OnStorageEjectHand(Entity<RMCBreakablePillBottleComponent> ent, ref RMCStorageEjectHandItemEvent args)
    {
        if (args.Handled || !ent.Comp.Broken)
            return;

        args.Handled = true;
        PopupBroken(ent, args.User);
    }

    private void OnDumpableDoAfter(Entity<RMCBreakablePillBottleComponent> ent, ref DumpableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !ent.Comp.Broken)
            return;

        args.Handled = true;
        PopupBroken(ent, args.User);
    }

    private void PopupBroken(Entity<RMCBreakablePillBottleComponent> ent, EntityUid user)
    {
        _popup.PopupClient($"The {Name(ent)} is broken...", ent, user, PopupType.SmallCaution);
    }

    private bool CanOpenNormally(EntityUid bottle, EntityUid user)
    {
        return TryComp(bottle, out StorageSkillRequiredComponent? required) &&
               _skills.HasAllSkills(user, required.Skills);
    }

    private void SetBroken(Entity<RMCBreakablePillBottleComponent> ent, bool broken)
    {
        ent.Comp.Broken = broken;
        Dirty(ent);
        _appearance.SetData(ent, RMCPillBottleVisuals.Broken, broken);
    }
}
