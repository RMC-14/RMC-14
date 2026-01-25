using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Interaction;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.NPC;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.Homing;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Sentry;

public sealed class SentrySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCInteractionSystem _rmcInteraction = default!;
    [Dependency] private readonly SharedRMCNPCSystem _rmcNpc = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<SentryComponent, MapInitEvent>(OnSentryMapInit);
        SubscribeLocalEvent<SentryComponent, PickupAttemptEvent>(OnSentryPickupAttempt);
        SubscribeLocalEvent<SentryComponent, UseInHandEvent>(OnSentryUseInHand);
        SubscribeLocalEvent<SentryComponent, SentryDeployDoAfterEvent>(OnSentryDeployDoAfter);
        SubscribeLocalEvent<SentryComponent, ActivateInWorldEvent>(OnSentryActivateInWorld);
        SubscribeLocalEvent<SentryComponent, AmmoShotEvent>(OnSentryAmmoShot);
        SubscribeLocalEvent<SentryComponent, AttemptShootEvent>(OnSentryAttemptShoot);
        SubscribeLocalEvent<SentryComponent, InteractUsingEvent>(OnSentryInteractUsing);
        SubscribeLocalEvent<SentryComponent, SentryInsertMagazineDoAfterEvent>(OnSentryInsertMagazineDoAfter);
        SubscribeLocalEvent<SentryComponent, SentryDisassembleDoAfterEvent>(OnSentryDisassembleDoAfter);
        SubscribeLocalEvent<SentryComponent, ExaminedEvent>(OnSentryExamined);
        SubscribeLocalEvent<SentryComponent, CombatModeShouldHandInteractEvent>(OnSentryShouldInteract);

        SubscribeLocalEvent<SentrySpikesComponent, AttackedEvent>(OnSentrySpikesAttacked);

        Subs.BuiEvents<SentryComponent>(SentryUiKey.Key,
            subs =>
            {
                subs.Event<SentryUpgradeBuiMsg>(OnSentryUpgradeBuiMsg);
            });
    }

    private void OnSentryMapInit(Entity<SentryComponent> sentry, ref MapInitEvent args)
    {
        _toUpdate.Add(sentry);

        if (sentry.Comp.StartingMagazine is { } magazine)
            TrySpawnInContainer(magazine, sentry, sentry.Comp.ContainerSlotId, out _);

        UpdateState(sentry);
    }

    private void OnSentryPickupAttempt(Entity<SentryComponent> sentry, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (sentry.Comp.Mode != SentryMode.Item)
            args.Cancel();
    }

    private void OnSentryUseInHand(Entity<SentryComponent> sentry, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (!CanDeployPopup(sentry, args.User, out _, out _))
            return;

        var ev = new SentryDeployDoAfterEvent();
        var delay = sentry.Comp.DeployDelay * _skills.GetSkillDelayMultiplier(args.User, sentry.Comp.DelaySkill);
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, sentry)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSentryDeployDoAfter(Entity<SentryComponent> sentry, ref SentryDeployDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (!CanDeployPopup(sentry, args.User, out var coordinates, out var angle))
            return;

        sentry.Comp.Mode = SentryMode.Off;
        Dirty(sentry);

        var xform = Transform(sentry);
        _transform.SetCoordinates(sentry, xform, coordinates, angle);
        _transform.AnchorEntity(sentry, xform);

        _rmcInteraction.SetMaxRotation(sentry.Owner, angle, sentry.Comp.MaxDeviation);

        if (_gunIFF.TryGetFaction(args.User, out var faction))
        {
            _gunIFF.SetUserFaction(sentry.Owner, faction);
        }

        UpdateState(sentry);
    }

    private void OnSentryActivateInWorld(Entity<SentryComponent> sentry, ref ActivateInWorldEvent args)
    {
        ref var mode = ref sentry.Comp.Mode;
        if (mode == SentryMode.Item)
            return;

        args.Handled = true;

        var user = args.User;
        switch (mode)
        {
            case SentryMode.Off:
            {
                foreach (var defense in _entityLookup.GetEntitiesInRange<SentryComponent>(_transform.GetMapCoordinates(sentry), sentry.Comp.DefenseCheckRange)) // TODO RMC14 more general defense check
                {
                    if (sentry != defense && defense.Comp.Mode == SentryMode.On)
                    {
                        var ret = Loc.GetString("rmc-sentry-too-close", ("defense", defense));
                        _popup.PopupClient(ret, sentry, user);
                        return;
                    }
                }
                mode = SentryMode.On;
                var msg = Loc.GetString("rmc-sentry-on", ("sentry", sentry));
                _popup.PopupClient(msg, sentry, user);
                break;
            }
            default:
            {
                mode = SentryMode.Off;
                var msg = Loc.GetString("rmc-sentry-off", ("sentry", sentry));
                _popup.PopupClient(msg, sentry, user);
                break;
            }
        }

        Dirty(sentry);
        UpdateState(sentry);
    }

    private void OnSentryAttemptShoot(Entity<SentryComponent> ent, ref AttemptShootEvent args)
    {
        if (args.User != ent.Owner)
            args.Cancelled = true;
    }

    private void OnSentryAmmoShot(Entity<SentryComponent> ent, ref AmmoShotEvent args)
    {
        if(!ent.Comp.HomingShots)
            return;

        //Make projectiles shot from a sentry gun homing.
        foreach (var projectile in args.FiredProjectiles)
        {
            if(!TryComp(projectile, out TargetedProjectileComponent? targeted))
                return;

            var homing = EnsureComp<HomingProjectileComponent>(projectile);
            homing.Target = targeted.Target;
        }
    }

    private void OnSentryInteractUsing(Entity<SentryComponent> sentry, ref InteractUsingEvent args)
    {
        var user = args.User;
        var used = args.Used;
        if (TryComp(used, out SentryUpgradeItemComponent? upgrade))
        {
            OpenUpgradeMenu(sentry, (used, upgrade), user);
            return;
        }

        if (HasComp<MultitoolComponent>(used))
        {
            StartDisassemble(sentry, user);
            return;
        }

        if (_tools.HasQuality(used, "Screwing"))
        {
            if (sentry.Comp.Mode == SentryMode.Off)
            {
                _transform.SetWorldRotation(sentry, _transform.GetWorldRotation(sentry) + Angle.FromDegrees(90));
                _rmcInteraction.SetMaxRotation(sentry.Owner, Transform(sentry).LocalRotation.GetCardinalDir().ToAngle(), sentry.Comp.MaxDeviation);
                UpdateState(sentry);
                _audio.PlayPredicted(sentry.Comp.ScrewdriverSound, sentry, user);
                var selfMsg = Loc.GetString("rmc-sentry-rotate-self", ("sentry", sentry));
                var othersMsg = Loc.GetString("rmc-sentry-rotate-others", ("user", user), ("sentry", sentry));
                _popup.PopupPredicted(selfMsg, othersMsg, user, user);
                args.Handled = true;
            }
            else
            {
                string ret;
                if(sentry.Comp.Mode == SentryMode.On)
                    ret = Loc.GetString("rmc-sentry-active-norot", ("sentry", sentry));
                else
                    ret = Loc.GetString("rmc-sentry-item-norot", ("sentry", sentry));
                _popup.PopupClient(ret, sentry, user);
            }
            return;
        }

        if (!HasComp<BallisticAmmoProviderComponent>(used))
            return;

        if (sentry.Comp.MagazineTag is { } magazineTag &&
            !_tag.HasTag(used, magazineTag))
        {
            var msg = Loc.GetString("rmc-sentry-magazine-does-not-fit", ("sentry", sentry), ("magazine", used));
            _popup.PopupClient(msg, sentry, user, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        if (!CanInsertMagazinePopup(sentry, user, used, out _))
            return;

        var delay = sentry.Comp.MagazineDelay * _skills.GetSkillDelayMultiplier(user, sentry.Comp.Skill);
        var ev = new SentryInsertMagazineDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, sentry, used: used)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-sentry-magazine-swap-start-user", ("magazine", used), ("sentry", sentry));
            var othersMsg = Loc.GetString("rmc-sentry-magazine-swap-start-others", ("user", user), ("magazine", used), ("sentry", sentry));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnSentryInsertMagazineDoAfter(Entity<SentryComponent> sentry, ref SentryInsertMagazineDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        args.Handled = true;

        var user = args.User;
        if (!CanInsertMagazinePopup(sentry, user, used, out var slot))
            return;

        _container.EmptyContainer(slot, destination: _transform.GetMoverCoordinates(user));
        if (!_container.Insert(used, slot))
            return;

        var selfMsg = Loc.GetString("rmc-sentry-magazine-swap-finish-user", ("magazine", used), ("sentry", sentry));
        var othersMsg = Loc.GetString("rmc-sentry-magazine-swap-finish-others", ("user", user), ("magazine", used), ("sentry", sentry));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);

        _audio.PlayPredicted(sentry.Comp.MagazineSwapSound, sentry, user);
    }

    private void OnSentryDisassembleDoAfter(Entity<SentryComponent> sentry, ref SentryDisassembleDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        sentry.Comp.Mode = SentryMode.Item;

        RemCompDeferred<MaxRotationComponent>(sentry);
        _transform.Unanchor(sentry.Owner, Transform(sentry));

        UpdateState(sentry);

        var selfMsg = Loc.GetString("rmc-sentry-disassemble-finish-self", ("sentry", sentry));
        var othersMsg = Loc.GetString("rmc-sentry-disassemble-finish-others", ("user", user), ("sentry", sentry));
        _popup.PopupPredicted(selfMsg, othersMsg, sentry, user);
    }

    private void OnSentryExamined(Entity<SentryComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(SentryComponent)))
        {
            if (ent.Comp.MaxDeviation < Angle.FromDegrees(180))
            {
                var rot = Loc.GetString("rmc-sentry-limited-rotation", ("degrees", (int)ent.Comp.MaxDeviation.Degrees));
                args.PushMarkup(rot);
            }

            var msg = Loc.GetString("rmc-sentry-disassembled-with-multitool");
            args.PushMarkup(msg);

            if (ent.Comp.Mode == SentryMode.Off)
            {
                var scw = Loc.GetString("rmc-sentry-rotate-with-screwdriver");
                args.PushMarkup(scw);
            }
        }
    }

    private void OnSentryShouldInteract(Entity<SentryComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        args.Cancelled = true;
    }

    private void OnSentryUpgradeBuiMsg(Entity<SentryComponent> oldSentry, ref SentryUpgradeBuiMsg args)
    {
        _ui.CloseUi(oldSentry.Owner, SentryUiKey.Key);

        var user = args.Actor;
        var upgrade = args.Upgrade;
        Entity<SentryUpgradeItemComponent> item = default;
        if (upgrade == default ||
            !CanUpgradePopup(oldSentry, ref item, user, upgrade))
        {
            return;
        }

        if (_net.IsClient)
            return;

        var coordinates = _transform.GetMapCoordinates(oldSentry);
        var rotation = _transform.GetWorldRotation(oldSentry);

        QueueDel(item);
        QueueDel(oldSentry);

        var newSentry = Spawn(upgrade, coordinates, rotation: rotation);
        var ev = new SentryUpgradedEvent(oldSentry, newSentry, user);
        RaiseLocalEvent(newSentry, ref ev);
    }

    private void UpdateState(Entity<SentryComponent> sentry)
    {
        var fixture = sentry.Comp.DeployFixture is { } fixtureId && TryComp(sentry, out FixturesComponent? fixtures)
            ? _fixture.GetFixtureOrNull(sentry, fixtureId, fixtures)
            : null;

        switch (sentry.Comp.Mode)
        {
            case SentryMode.Item:
                if (fixture != null)
                    _physics.SetHard(sentry, fixture, false);

                _rmcNpc.SleepNPC(sentry);
                _appearance.SetData(sentry, SentryLayers.Layer, SentryMode.Item);
                _pointLight.SetEnabled(sentry, false);
                break;
            case SentryMode.Off:
                if (fixture != null)
                    _physics.SetHard(sentry, fixture, true);

                _rmcNpc.SleepNPC(sentry);
                _appearance.SetData(sentry, SentryLayers.Layer, SentryMode.Off);
                _pointLight.SetEnabled(sentry, false);
                break;
            case SentryMode.On:
                if (fixture != null)
                    _physics.SetHard(sentry, fixture, true);

                _rmcNpc.WakeNPC(sentry);
                _appearance.SetData(sentry, SentryLayers.Layer, SentryMode.On);
                _pointLight.SetEnabled(sentry, true);
                break;
        }
    }

    private bool CanDeployPopup(
        Entity<SentryComponent> sentry,
        EntityUid user,
        out EntityCoordinates coordinates,
        out Angle rotation)
    {
        coordinates = default;
        rotation = default;

        var moverCoordinates = _transform.GetMoverCoordinateRotation(user, Transform(user));
        coordinates = moverCoordinates.Coords;
        rotation = moverCoordinates.worldRot.GetCardinalDir().ToAngle();

        var direction = rotation.GetCardinalDir();
        coordinates = coordinates.Offset(direction.ToVec());
        if (!_rmcMap.CanBuildOn(coordinates))
        {
            var msg = Loc.GetString("rmc-sentry-need-open-area", ("sentry", sentry));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private bool CanInsertMagazinePopup(
        Entity<SentryComponent> sentry,
        EntityUid user,
        EntityUid used,
        [NotNullWhen(true)] out ContainerSlot? slot)
    {
        slot = null;

        slot = _container.EnsureContainer<ContainerSlot>(sentry, sentry.Comp.ContainerSlotId);
        if (!_container.CanInsert(used, slot, true))
        {
            var msg = Loc.GetString("rmc-sentry-magazine-invalid", ("item", used));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        if (TryComp(slot.ContainedEntity, out BallisticAmmoProviderComponent? ammo) &&
            ammo.Count > 0 &&
            !HasComp<BypassInteractionChecksComponent>(user))
        {
            var msg = Loc.GetString("rmc-sentry-magazine-swap-not-empty");
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void OpenUpgradeMenu(
        Entity<SentryComponent> sentry,
        Entity<SentryUpgradeItemComponent> upgrade,
        EntityUid user)
    {
        if (!CanUpgradePopup(sentry, ref upgrade, user, null))
            return;

        _ui.OpenUi(sentry.Owner, SentryUiKey.Key, user);
    }

    private bool CanUpgradePopup(
        Entity<SentryComponent> sentry,
        ref Entity<SentryUpgradeItemComponent> upgradeItem,
        EntityUid user,
        EntProtoId? upgrade)
    {
        if (sentry.Comp.Upgrades is not { Length: > 0 } upgrades)
        {
            var msg = Loc.GetString("rmc-sentry-upgrade-not-upgradeable", ("sentry", sentry));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        if (sentry.Comp.Mode != SentryMode.Item)
        {
            var msg = Loc.GetString("rmc-sentry-upgrade-not-item", ("sentry", sentry));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        if (upgradeItem == default)
        {
            if (!_hands.TryGetActiveItem(user, out var active) ||
                !TryComp(active, out SentryUpgradeItemComponent? upgradeComp))
            {
                var msg = Loc.GetString("rmc-sentry-upgrade-not-holding", ("sentry", sentry));
                _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
                return false;
            }

            upgradeItem = (active.Value, upgradeComp);
        }

        if (upgrade != null && !upgrades.Contains(upgrade.Value))
        {
            Log.Warning(
                $"{ToPrettyString(user)} tried to upgrade sentry {ToPrettyString(sentry)} to invalid upgrade {upgrade.Value}");
            return false;
        }

        return true;
    }

    private void StartDisassemble(Entity<SentryComponent> sentry, EntityUid user)
    {
        if (sentry.Comp.Mode == SentryMode.Item)
            return;

        var ev = new SentryDisassembleDoAfterEvent();
        var delay = sentry.Comp.UndeployDelay * _skills.GetSkillDelayMultiplier(user, sentry.Comp.DelaySkill);
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, sentry)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-sentry-disassemble-start-self", ("sentry", sentry));
            var othersMsg = Loc.GetString("rmc-sentry-disassemble-start-others", ("user", user), ("sentry", sentry));
            _popup.PopupPredicted(selfMsg, othersMsg, sentry, user);
        }
    }

    public bool TrySetMode(Entity<SentryComponent> sentry, SentryMode mode)
    {
        if (sentry.Comp.Mode == mode)
            return false;

        sentry.Comp.Mode = mode;
        UpdateState(sentry);
        Dirty(sentry, sentry.Comp);
        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
        {
            _toUpdate.Clear();
            return;
        }

        try
        {
            foreach (var id in _toUpdate)
            {
                if (TryComp(id, out SentryComponent? sentry))
                    UpdateState((id, sentry));
            }
        }
        finally
        {
            _toUpdate.Clear();
        }
    }

    private void OnSentrySpikesAttacked(Entity<SentrySpikesComponent> sentry, ref AttackedEvent args)
    {
        if (!TryComp<SentryComponent>(sentry, out var senComp) || senComp.Mode != SentryMode.On)
            return;

        _damageableSystem.TryChangeDamage(args.User, sentry.Comp.SpikeDamage, origin: sentry, tool: sentry);
        var self = Loc.GetString("rmc-sentry-spikes-self");
        var others = Loc.GetString("rmc-sentry-spikes-others", ("target", args.User));
        _popup.PopupPredicted(self, others, sentry, args.User, PopupType.SmallCaution);
    }
}
