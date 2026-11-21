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
            args.PushMarkup(Loc.GetString("rmc-pill-bottle-broken-examine"));
            args.PushMarkup(Loc.GetString("rmc-pill-bottle-broken-examine-repair"));
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
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-must-hold", ("bottle", Name(ent))), ent, user, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-already-broken", ("bottle", Name(ent))), ent, user, PopupType.SmallCaution);
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

        var selfMsg = Loc.GetString("rmc-pill-bottle-break-start-self",
            ("tool", Name(used)), ("bottle", Name(ent)), ("canOpen", CanOpenNormally(ent, user) ? "yes" : "no"));

        _popup.PopupPredicted(
            selfMsg,
            Loc.GetString("rmc-pill-bottle-break-start-others", ("user", Identity.Name(user, EntityManager)), ("tool", Name(used)), ("bottle", Name(ent))),
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
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-break-cancel", ("bottle", Name(ent))), ent, user);
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
                Loc.GetString("rmc-pill-bottle-break-spill-self", ("bottle", Name(ent))),
                Loc.GetString("rmc-pill-bottle-break-spill-others", ("user", userName), ("bottle", Name(ent))),
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
                Loc.GetString("rmc-pill-bottle-break-self", ("bottle", Name(ent))),
                Loc.GetString("rmc-pill-bottle-break-others", ("user", userName), ("bottle", Name(ent))),
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
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-must-hold", ("bottle", Name(ent))), ent, user, PopupType.SmallCaution);
            return;
        }

        if (!ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-not-broken", ("bottle", Name(ent))), ent, user, PopupType.SmallCaution);
            return;
        }

        var skill = _skills.GetSkill(user, ent.Comp.RepairSkill);
        if (skill < 1 || ent.Comp.RepairDelays.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-repair-no-skill"), ent, user, PopupType.SmallCaution);
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
        var bottleName = Name(ent);
        var toolName = Name(used);
        var (selfMsg, othersMsg) = tier switch
        {
            1 => (Loc.GetString("rmc-pill-bottle-repair-start-self-1", ("bottle", bottleName), ("tool", toolName)),
                Loc.GetString("rmc-pill-bottle-repair-start-others-1", ("user", userName), ("bottle", bottleName), ("tool", toolName))),
            2 => (Loc.GetString("rmc-pill-bottle-repair-start-self-2", ("bottle", bottleName), ("tool", toolName)),
                Loc.GetString("rmc-pill-bottle-repair-start-others-2", ("user", userName), ("bottle", bottleName), ("tool", toolName))),
            _ => (Loc.GetString("rmc-pill-bottle-repair-start-self-3", ("bottle", bottleName), ("tool", toolName)),
                Loc.GetString("rmc-pill-bottle-repair-start-others-3", ("user", userName), ("bottle", bottleName), ("tool", toolName))),
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
            _popup.PopupClient(Loc.GetString("rmc-pill-bottle-repair-cancel", ("bottle", Name(ent))), ent, user);
            return;
        }

        if (!ent.Comp.Broken)
            return;

        SetBroken(ent, false);
        _popup.PopupPredicted(
            Loc.GetString("rmc-pill-bottle-repair-self", ("bottle", Name(ent))),
            Loc.GetString("rmc-pill-bottle-repair-others", ("user", Identity.Name(user, EntityManager)), ("bottle", Name(ent))),
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
        _popup.PopupClient(Loc.GetString("rmc-pill-bottle-broken", ("bottle", Name(ent))), ent, user, PopupType.SmallCaution);
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
