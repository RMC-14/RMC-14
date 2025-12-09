using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Scoping;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Weapons.Ranged.Overheat;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Emplacements;

public abstract class SharedWeaponMountSystem : EntitySystem
{
    [Dependency] protected readonly SharedXenoAcidSystem XenoAcid = default!;

    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BarricadeSystem _barricade = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedScopeSystem _scope = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private const string AmmoExamineColor = "yellow";
    private const string FireRateExamineColor = "yellow";
    private const string ModeExamineColor = "cyan";
    private const string ToolExamineColor = "cyan";
    private const string MagazineKey = "gun_magazine";

    public override void Initialize()
    {
        SubscribeLocalEvent<WeaponMountComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WeaponMountComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<WeaponMountComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<WeaponMountComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<WeaponMountComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WeaponMountComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<WeaponMountComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<WeaponMountComponent, UnstrappedEvent>(OnUnStrapped);
        SubscribeLocalEvent<WeaponMountComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WeaponMountComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeaponMountComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<WeaponMountComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<WeaponMountComponent, DamageModifyEvent>(OnDamageModified);
        SubscribeLocalEvent<WeaponMountComponent, RMCCheckTileFreeEvent>(OnCheckTileFree);
        SubscribeLocalEvent<WeaponMountComponent, GetIFFGunUserEvent>(OnGetGunUser);
        SubscribeLocalEvent<WeaponMountComponent, InteractHandEvent>(OnInteractHand, before: new[] { typeof(SharedBuckleSystem) });

        // Relayed events
        SubscribeLocalEvent<WeaponMountComponent, MountableWeaponRelayedEvent<OverheatedEvent>>(OnWeaponOverheated);
        SubscribeLocalEvent<WeaponMountComponent, MountableWeaponRelayedEvent<HeatGainedEvent>>(OnWeaponHeatGained);

        // Mount Assembly/Disassembly
        SubscribeLocalEvent<WeaponMountComponent, AttachToMountDoAfterEvent>(OnAttachToMount);
        SubscribeLocalEvent<WeaponMountComponent, SecureToMountDoAfterEvent>(OnSecureToMount);
        SubscribeLocalEvent<WeaponMountComponent, DetachFromMountDoAfterEvent>(OnDetachFromMount);
        SubscribeLocalEvent<WeaponMountComponent, MountDeployDoafterEvent>(OnMountDeploy);
        SubscribeLocalEvent<WeaponMountComponent, MountUnDeployDoAfterEvent>(OnMountUndeploy);
    }

    private void OnMapInit(Entity<WeaponMountComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.FixedWeaponPrototype == null)
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.WeaponSlotId);
        container.OccludesLight = false;

        if (container.ContainedEntities.Count > 0)
            return;

        var weapon = SpawnInContainerOrDrop(ent.Comp.FixedWeaponPrototype, ent, ent.Comp.WeaponSlotId);
        ent.Comp.MountedEntity = weapon;
        if (!TryComp(weapon, out MountableWeaponComponent? mountedWeapon))
            return;

        mountedWeapon.MountedTo = GetNetEntity(ent.Owner);
        Dirty(ent);
        Dirty(weapon, mountedWeapon);
    }

    private void OnInteractUsing(Entity<WeaponMountComponent> ent, ref InteractUsingEvent args)
    {
        // Reload the weapon that is attached to the mount
        if (ent.Comp.MountedEntity != null &&
            _slots.TryGetSlot(ent.Comp.MountedEntity.Value, MagazineKey, out var itemSlot) &&
            TryComp(args.Used, out BallisticAmmoProviderComponent? ballistic))
        {
            if (!TryComp(args.User, out HandsComponent? hands) ||
                !_slots.CanInsert(ent.Comp.MountedEntity.Value, args.Used, args.User, itemSlot, true) ||
                !_hands.TryDrop(args.User, args.Used))
                return;

            if (itemSlot.Item != null)
                _hands.TryPickupAnyHand(args.User, itemSlot.Item.Value, handsComp: hands);

            _slots.TryInsert(ent.Comp.MountedEntity.Value, MagazineKey, args.Used, args.User, null, true);

            var ammoSpriteKey = WeaponMountComponentVisualLayers.MountedAmmo;
            if (TryComp(ent, out FoldableComponent? foldableComp) && foldableComp.IsFolded)
                ammoSpriteKey = WeaponMountComponentVisualLayers.FoldedAmmo;

            _appearance.SetData(ent, ammoSpriteKey, ballistic.Count > 0);
            return;
        }

        if (ent.Comp.IsWeaponLocked || TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
            return;

        // Attach equipment to the mount
        if (HasComp<MountableWeaponComponent>(args.Used) && Transform(ent).Anchored && ent.Comp.MountedEntity == null)
        {
            TryAttachToMount(ent, args.User, args.Used);
            return;
        }

        // Rotate the mount.
        if (_tool.HasQuality(args.Used, ent.Comp.RotationTool))
            RotateMount(ent, args.User);

        // Detach equipment from the mount.
        else if (_tool.HasQuality(args.Used, ent.Comp.DismantlingTool) &&
                 _container.TryGetContainer(ent, ent.Comp.WeaponSlotId, out var container) &&
                 container.ContainedEntities.Count > 0 &&
                 !ent.Comp.IsWeaponSecured)
        {
            TryDetachFromMount(ent, args.User, args.Used);
        }
    }

    private void OnUseInHand(Entity<WeaponMountComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.MountedEntity == null)
            return;

        if (!CanDeployPopup(ent, args.User, out _, out _))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.AssembleDelay,
            new MountDeployDoafterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
    }

    private void OnFoldAttempt(Entity<WeaponMountComponent> ent, ref FoldAttemptEvent args)
    {
        if (Transform(ent).Anchored || ent.Comp.MountedEntity != null)
        {
            args.Cancelled = true;
        }
    }

    private void OnAnchorAttempt(Entity<WeaponMountComponent> ent, ref AnchorAttemptEvent args)
    {
        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
        {
            args.Cancel();
        }
    }

    private void OnUnanchorAttempt(Entity<WeaponMountComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
        {
            args.Cancel();
            return;
        }

        if (ent.Comp.MountedEntity != null)
        {
            args.Cancel();
            if (!ent.Comp.IsWeaponSecured)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    ent.Comp.AssembleDelay,
                    new SecureToMountDoAfterEvent(),
                    ent,
                    ent,
                    args.Tool)
                {
                    NeedHand = true,
                    BreakOnMove = true,
                    BreakOnHandChange = true,
                };

                _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
                return;
            }

            if (foldable == null)
                return;

            TryUndeployMount(ent, args.User, args.Tool);
        }
    }

    private void OnAttachToMount(Entity<WeaponMountComponent>ent, ref AttachToMountDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(args.Used, out MountableWeaponComponent? weapon) || args.Used == null)
            return;

        if (!CanAssembleMount(ent, args.User))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.WeaponSlotId);
        container.OccludesLight = false;
        if (container.ContainedEntities.Count > 0)
            return;

        if (!_container.Insert(args.Used.Value, container))
            return;

        weapon.MountedTo = GetNetEntity(ent);
        ent.Comp.MountedEntity = args.Used;
        _collisionWake.SetEnabled(ent, false);
        _item.SetSize(ent, ent.Comp.MountedWeaponSize);

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.RotateSound, ent, args.User);
    }

    private void OnSecureToMount(Entity<WeaponMountComponent>ent, ref SecureToMountDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.IsWeaponSecured = true;
        _buckle.StrapSetEnabled(ent, true);

        if (TryComp(ent.Comp.MountedEntity, out MetaDataComponent? mountedMeta) && mountedMeta.EntityPrototype != null)
        {
            _metaData.SetEntityName(ent, mountedMeta.EntityName);
            _metaData.SetEntityDescription(ent, Loc.GetString( "emplacement-mount-" + mountedMeta.EntityPrototype.ID + "-description-mounted"));
        }

        _audio.PlayPredicted(ent.Comp.SecureSound, ent, args.User);
    }

    private void OnDetachFromMount(Entity<WeaponMountComponent>ent, ref DetachFromMountDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.WeaponSlotId, out var container))
            return;

        _container.EmptyContainer(container);
        if (TryComp(ent.Comp.MountedEntity, out MountableWeaponComponent? attachedWeapon))
        {
            attachedWeapon.MountedTo = GetNetEntity(ent);
        }

        if (TryComp(ent.Comp.MountedEntity, out MetaDataComponent? mountedMeta) && mountedMeta.EntityPrototype != null)
        {
            _metaData.SetEntityName(ent, Loc.GetString("emplacement-mount-" + mountedMeta.EntityPrototype.ID + "-name"));
            _metaData.SetEntityDescription(ent, Loc.GetString("emplacement-mount-" + mountedMeta.EntityPrototype.ID + "-description"));
        }

        ent.Comp.MountedEntity = null;
        _buckle.StrapSetEnabled(ent, false);
        _collisionWake.SetEnabled(ent, true);
        _item.SetSize(ent, ent.Comp.MountSize);

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.DetachSound, ent, args.User);
    }

    private void OnMountDeploy(Entity<WeaponMountComponent>ent, ref MountDeployDoafterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanDeployPopup(ent, args.User, out var coordinates, out var rotation))
            return;

        if (TryComp(ent, out FoldableComponent? foldable))
            _foldable.SetFolded(ent, foldable, false);

        if (TryComp(ent.Comp.MountedEntity, out MetaDataComponent? mountedMeta) && ent.Comp.IsWeaponLocked && mountedMeta.EntityPrototype != null)
            _metaData.SetEntityDescription(ent, Loc.GetString( "emplacement-mount-" + mountedMeta.EntityPrototype.ID + "-description-mounted"));

        var xform = Transform(ent);
        _transform.SetCoordinates(ent, xform, coordinates, rotation);
        _transform.AnchorEntity(ent, xform);
        _collisionWake.SetEnabled(ent, false);

        if (ent.Comp.MountOnDeploy && ent.Comp.MountedEntity != null)
        {
            var ammoCountEvent = new GetAmmoCountEvent();
            RaiseLocalEvent(ent.Comp.MountedEntity.Value, ref ammoCountEvent);
            if (ammoCountEvent.Count > 0)
                _buckle.TryBuckle(args.User, args.User, ent, popup: false);
        }

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.DeploySound, ent, args.User);
    }

    private void OnMountUndeploy(Entity<WeaponMountComponent>ent, ref MountUnDeployDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(ent, out FoldableComponent? foldable))
            return;

        UndeployMount(ent, args.User, foldable);
    }

    public void UndeployMount(Entity<WeaponMountComponent> ent, EntityUid? user = null, FoldableComponent? foldable = null)
    {
        if (TryComp(ent.Comp.MountedEntity, out MetaDataComponent? mountedMeta) && ent.Comp.IsWeaponLocked && mountedMeta.EntityPrototype != null)
            _metaData.SetEntityDescription(ent, Loc.GetString("emplacement-mount-" + mountedMeta.EntityPrototype.ID + "-description"));

        ent.Comp.IsWeaponSecured = false;
        _transform.Unanchor(ent);

        if (foldable != null)
            _foldable.SetFolded(ent, foldable, true);

        _buckle.StrapSetEnabled(ent, false);
        _collisionWake.SetEnabled(ent, true);

        if (TryComp(user, out HandsComponent? hands))
            _hands.TryPickupAnyHand(user.Value, ent, handsComp: hands);

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.UndeploySound, ent, user);
    }

    private bool CanDeployPopup(Entity<WeaponMountComponent> ent, EntityUid user, out EntityCoordinates coordinates, out Angle rotation)
    {
        var moverCoordinates = _transform.GetMoverCoordinateRotation(user, Transform(user));
        coordinates = moverCoordinates.Coords;
        rotation = moverCoordinates.worldRot.GetCardinalDir().ToAngle();

        if (ent.Comp.Broken)
        {
            var msg = Loc.GetString("emplacement-mount-deploy-broken", ("mount", ent));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        var direction = rotation.GetCardinalDir();
        coordinates = coordinates.Offset(direction.ToVec());
        if (!_rmcMap.CanBuildOn(coordinates))
        {
            var msg = Loc.GetString("rmc-sentry-need-open-area", ("sentry", ent));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        var grid = _transform.GetGrid((user, Transform(user)));
        if (TryComp(grid, out MapGridComponent? mapGrid))
        {
            if (HasWeaponMountsNearbyPopup((grid.Value, mapGrid), user, coordinates, ent.Comp.MountExclusionAreaSize))
                return false;

            if (ent.Comp.BarricadeExclusionAreaSize != 0 &&
                _barricade.HasBarricadeNearbyPopup((grid.Value, mapGrid), user, coordinates, ent.Comp.BarricadeExclusionAreaSize))
                return false;
        }
        return true;
    }

    private void OnInteractHand(Entity<WeaponMountComponent> ent, ref InteractHandEvent args)
    {
        if (!_combatMode.IsInCombatMode(args.User))
            return;

        args.Handled = true;
    }

    private void OnStrapAttempt(Entity<WeaponMountComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.User == args.Buckle)
            return;

        args.Cancelled = true;
    }

    private void OnStrapped(Entity<WeaponMountComponent> ent, ref StrappedEvent args)
    {
        ent.Comp.User = args.Buckle;
        if (ent.Comp.MountedEntity == null)
            return;

        var weaponController = EnsureComp<WeaponControllerComponent>(args.Buckle);
        weaponController.ControlledWeapon = GetNetEntity(ent.Comp.MountedEntity.Value);

        if (TryComp(ent.Comp.MountedEntity.Value, out ScopeComponent? scope))
        {
            _scope.StartScoping((ent.Comp.MountedEntity.Value, scope), args.Buckle);
        }

        _actions.AddAction(args.Buckle, ref ent.Comp.DismountActionEntity, ent.Comp.DismountAction, args.Buckle);
    }

    private void OnUnStrapped(Entity<WeaponMountComponent> ent, ref UnstrappedEvent args)
    {
        ent.Comp.User = null;
        RemComp<WeaponControllerComponent>(args.Buckle);

        if (TryComp(ent.Comp.MountedEntity, out ScopeComponent? scope))
        {
            _scope.Unscope((ent.Comp.MountedEntity.Value, scope));
        }

        _actions.RemoveAction(ent.Comp.DismountActionEntity);
    }

    private void OnExamine(Entity<WeaponMountComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || HasComp<XenoComponent>(args.Examiner))
            return;

        // Weapon stats
        if (TryComp(ent.Comp.MountedEntity, out GunComponent? gunComponent))
        {
            if (TryGetWeaponAmmo(ent, out var ammoCount, out _))
            {
                args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", ammoCount)));
                args.PushMarkup(Loc.GetString("gun-selected-mode-examine", ("color", ModeExamineColor),
                    ("mode",Loc.GetString($"gun-{Enum.GetName(typeof(SelectiveFire), gunComponent.SelectedMode)}"))), priority: 4);
                args.PushMarkup(Loc.GetString("gun-fire-rate-examine", ("color", FireRateExamineColor),
                    ("fireRate", $"{gunComponent.FireRateModified:0.0}")), priority: 3);
            }
        }

        // Broken information
        if (ent.Comp.Broken)
            args.PushMarkup(Loc.GetString("emplacement-mount-broken-examine"), priority: 0);

        if (ent.Comp.IsWeaponLocked)
            return;

        // Assembly/Dismantle instructions
        using (args.PushGroup(nameof(WeaponMountComponent)))
        {
            string? message = null;
            if (!Transform(ent).Anchored && !_foldable.IsFolded(ent))
                message = "emplacement-mount-unanchored-examine";
            else if (ent.Comp.MountedEntity == null && Transform(ent).Anchored)
                message = "emplacement-mount-anchored-examine";
            else if (!ent.Comp.IsWeaponSecured && ent.Comp.MountedEntity != null && !_foldable.IsFolded(ent))
                message = "emplacement-mount-weapon-unsecured-examine";
            else if (ent.Comp.IsWeaponSecured && Transform(ent).Anchored)
                message = "emplacement-mount-weapon-secured-examine";

            if (message != null)
                args.PushMarkup(Loc.GetString(message, ("color", ToolExamineColor)), priority: 1);
        }
    }

    private void OnAltVerb(EntityUid uid, WeaponMountComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp(component.MountedEntity, out GunComponent? gun))
            return;

        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        var nextMode = _gun.GetNextMode(gun);

        // Add the fire mode change verb
        if (gun.SelectedMode != gun.AvailableModes)
        {
            AlternativeVerb verb = new()
            {
                Act = () => _gun.SelectFire(component.MountedEntity.Value, gun, nextMode, args.User),
                Text = Loc.GetString("gun-selector-verb", ("mode",Loc.GetString($"gun-{Enum.GetName(typeof(SelectiveFire), nextMode)}"))),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
                Priority = 3,
            };

            args.Verbs.Add(verb);
        }

        // Add the Undeploy verb
        if (component.IsWeaponLocked && TryComp(uid, out FoldableComponent? foldable) && !foldable.IsFolded)
        {
            AlternativeVerb dismantleVerb = new()
            {
                Act = () => TryUndeployMount((uid, component), args.User),
                Text = Loc.GetString("emplacement-mount-undeploy"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
                Priority = 3,
            };
            args.Verbs.Add(dismantleVerb);
        }

        // Add the ammo eject verb
        if (TryComp(component.MountedEntity, out ItemSlotsComponent? itemSlots) && HasComp<FoldableComponent>(uid))
        {
            foreach (var slot in itemSlots.Slots.Values)
            {
                if (slot.EjectOnInteract || slot.DisableEject)
                    continue;

                if (!_slots.CanEject(uid, args.User, slot))
                    continue;

                if (!_actionBlockerSystem.CanPickup(args.User, slot.Item!.Value))
                    continue;

                var verbSubject = slot.Name != string.Empty
                    ? Loc.GetString(slot.Name)
                    : Comp<MetaDataComponent>(slot.Item.Value).EntityName;

                AlternativeVerb verb = new()
                {
                    IconEntity = GetNetEntity(slot.Item),
                    Act = () => EjectMagazine(component.MountedEntity.Value, slot, args.User, uid)
                };

                if (slot.EjectVerbText == null)
                {
                    verb.Text = verbSubject;
                    verb.Category = VerbCategory.Eject;
                }
                else
                {
                    verb.Text = Loc.GetString(slot.EjectVerbText);
                }

                verb.Priority = 3;
                args.Verbs.Add(verb);
            }
        }
    }

    /// <summary>
    ///     Check if a weapon mount is anchored near the given coordinates..
    /// </summary>
    /// <param name="grid">The map grid being checked.</param>
    /// <param name="user">The entity performing the search</param>
    /// <param name="coordinates">The coordinates used as the center</param>
    /// <param name="range">The radius of the search area</param>
    /// <returns>True if an anchored entity with a <see cref="WeaponMountComponent"/> within the specified range</returns>
    public bool HasWeaponMountsNearbyPopup(Entity<MapGridComponent> grid, EntityUid user, EntityCoordinates coordinates, int range)
    {
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var checkArea = new Box2(position.X - range, position.Y - range, position.X + range, position.Y + range);
        var enumerable = _mapSystem.GetLocalAnchoredEntities(grid, grid, checkArea);

        foreach (var anchored in enumerable)
        {
            if (TryComp(anchored, out WeaponMountComponent? mount) && mount.MountedEntity != null)
            {
                var msg = Loc.GetString("emplacement-mount-too-close", ("mount", anchored));
                _popup.PopupClient(msg, user, user, PopupType.SmallCaution );
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     Checks if the mount can be assembled at its current location.
    /// </summary>
    /// <param name="ent">The mount being assembled</param>
    /// <param name="user">The entity attempting to assemble the mount.</param>
    /// <returns>True if the mount can be assembled</returns>

    private bool CanAssembleMount(Entity<WeaponMountComponent> ent, EntityUid user)
    {
        if (ent.Comp.MountExclusionAreaSize == 0)
            return true;

        var grid = _transform.GetGrid((ent, Transform(ent)));
        if (!TryComp(grid, out MapGridComponent? mapGrid))
            return true;

        if (HasWeaponMountsNearbyPopup((grid.Value, mapGrid), user, _transform.GetMoverCoordinates(ent), ent.Comp.MountExclusionAreaSize))
            return false;

        return true;
    }

    /// <summary>
    ///     Rotate the mount.
    /// </summary>
    /// <param name="ent">The entity being rotated</param>
    /// <param name="user">The entity rotating the mount</param>
    /// <param name="rotationDegrees">The rotation of the mount in degrees</param>
    public void RotateMount(Entity<WeaponMountComponent> ent, EntityUid? user, int rotationDegrees = 90)
    {
        _transform.SetLocalRotation(ent, _transform.GetWorldRotation(ent) + Angle.FromDegrees(rotationDegrees));
        _audio.PlayPredicted(ent.Comp.RotateSound, ent, user);

        if (ent.Comp.User == null || !TryComp(ent.Comp.MountedEntity, out ScopeComponent? scope))
            return;

        _scope.StartScoping((ent.Comp.MountedEntity.Value, scope), ent.Comp.User.Value);
    }

    /// <summary>
    ///     Try to start a DoAfter that will attach an entity to the mount if it succeeds.
    /// </summary>
    /// <param name="ent">The mount the entity is being attached to</param>
    /// <param name="user">The entity that tried to attach</param>
    /// <param name="used">The entity being attached</param>
    /// <returns></returns>
    private bool TryAttachToMount(Entity<WeaponMountComponent> ent, EntityUid user, EntityUid used)
    {
        if (!CanAssembleMount(ent, user))
            return false;

        if (!IsViableWeapon(used, ent))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.AssembleDelay,
            new AttachToMountDoAfterEvent(),
            ent,
            ent,
            used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
        return true;
    }

    /// <summary>
    ///     Try to start a DoAfter that removes the attached entity from the mount.
    /// </summary>
    /// <param name="ent">The mount that is being detached from</param>
    /// <param name="user">The entity that tried to detach</param>
    /// <param name="used">The entity being used to detach</param>
    /// <returns></returns>
    private bool TryDetachFromMount(Entity<WeaponMountComponent> ent, EntityUid user, EntityUid? used = null)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.DisassembleDelay,
            new DetachFromMountDoAfterEvent(),
            ent,
            ent,
            used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
        return true;
    }

    private bool TryUndeployMount(Entity<WeaponMountComponent> ent, EntityUid user, EntityUid? used = null)
    {
        var undeployDoAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.DisassembleDelay,
            new MountUnDeployDoAfterEvent(),
            ent,
            ent,
            used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(undeployDoAfterArgs));
        return true;
    }

    private void EjectMagazine(EntityUid uid, ItemSlot slot, EntityUid user, EntityUid mount)
    {
        if (_slots.TryEjectToHands(uid, slot, user, excludeUserAudio: true))
        {
            var ammoSpriteKey = WeaponMountComponentVisualLayers.MountedAmmo;
            if (TryComp(mount, out FoldableComponent? foldableComp) && foldableComp.IsFolded)
                ammoSpriteKey = WeaponMountComponentVisualLayers.FoldedAmmo;

            _appearance.SetData(mount, ammoSpriteKey, false);
        }
    }

    private void OnBreak(Entity<WeaponMountComponent> ent, ref BreakageEventArgs args)
    {
        TryComp(ent, out FoldableComponent? foldable);
        ent.Comp.Broken = true;
        Dirty(ent);

        // The mount can be melted while broken
        if (TryComp(ent, out CorrodibleComponent? corrodible))
            XenoAcid.SetCorrodible(corrodible, true);

        UndeployMount(ent, null, foldable);
        UpdateAppearance(ent);
    }

    private void OnDamageModified(Entity<WeaponMountComponent> ent, ref DamageModifyEvent args)
    {
        // Set all damage received to 0 if the mount is folded.
        if (TryComp(ent, out FoldableComponent? foldable) && foldable.IsFolded)
            args.Damage = new DamageSpecifier();
    }

    private void OnCheckTileFree(Entity<WeaponMountComponent> ent, ref RMCCheckTileFreeEvent args)
    {
        if (!HasComp<BarricadeComponent>(args.AnchoredEntity))
            return;

        args.IsTileFree = true;
    }

    /// <summary>
    ///     Checks if the entity is able to be attached to the mount.
    /// </summary>
    /// <param name="weapon">The entity being attached</param>
    /// <param name="mount">The mount being attached to</param>
    /// <param name="weaponMountComponent">The <see cref="WeaponMountComponent"/> of the mount</param>
    /// <returns>True if the weapon is able to be attached to the mount</returns>
    public bool IsViableWeapon(EntityUid weapon, EntityUid mount, WeaponMountComponent? weaponMountComponent = null)
    {
        if (!Resolve(mount, ref weaponMountComponent, false))
            return false;

        return _whitelist.IsWhitelistPassOrNull(weaponMountComponent.MountableWhitelist, weapon);
    }

    private void OnWeaponOverheated(Entity<WeaponMountComponent> ent, ref MountableWeaponRelayedEvent<OverheatedEvent> args)
    {
        if (args.Args.Damage == null)
            return;

        _damage.TryChangeDamage(ent, args.Args.Damage);
        if (ent.Comp.MountedEntity != null)
        {
            _popup.PopupClient(Loc.GetString("emplacement-mounted-weapon-overheated",
                    ("weapon", ent.Comp.MountedEntity.Value)),
                ent,
                ent.Comp.User,
                PopupType.SmallCaution);
        }
    }

    private void OnWeaponHeatGained(Entity<WeaponMountComponent> ent, ref MountableWeaponRelayedEvent<HeatGainedEvent> args)
    {
        UpdateAppearance(ent);
    }

    private void OnGetGunUser(Entity<WeaponMountComponent> ent, ref GetIFFGunUserEvent args)
    {
        args.GunUser = ent.Comp.User;
    }

    public void UpdateAppearance(EntityUid mount, WeaponMountComponent? mountComponent = null)
    {
        if (!Resolve(mount, ref mountComponent, false))
            return;

        if (TryComp(mount, out FoldableComponent? foldable))
        {
            //Deployed with weapon attached
            _appearance.SetData(mount, WeaponMountComponentVisualLayers.Mounted, !foldable.IsFolded && mountComponent.MountedEntity != null);
            //Folded with weapon attached
            _appearance.SetData(mount, WeaponMountComponentVisualLayers.Folded, foldable.IsFolded && mountComponent.MountedEntity != null);
            //Broken while folded
            _appearance.SetData(mount, WeaponMountComponentVisualLayers.Broken, mountComponent.Broken);
        }

        if (mountComponent.MountedEntity == null || !TryComp(mountComponent.MountedEntity.Value, out OverheatComponent? overheat))
            return;

        // Overheat visual that gradually increases/decreases its visibility depending on the amount of heat stacks
        _appearance.TryGetData<Color>(mount, WeaponMountComponentVisualLayers.Overheated, out var color);

        var showHeated = foldable == null || !foldable.IsFolded;
        _appearance.SetData(mount, WeaponMountComponentVisualLayers.Overheated, showHeated);

        var alpha = Math.Clamp(overheat.Heat / overheat.MaxHeat, 0f, 1f);
        _appearance.SetData(mount, WeaponMountComponentVisualLayers.Overheated, color.WithAlpha(alpha));
    }

    /// <summary>
    ///     Tries to get the current amount of ammo and max amount of ammo of the weapon attached to the mount.
    /// </summary>
    /// <param name="mount">The mount</param>
    /// <param name="ammoCount">The current amount of ammo</param>
    /// <param name="ammoCapacity">The maximum amount of ammo</param>
    /// <param name="mountComponent">The <see cref="WeaponMountComponent"/> of the mount</param>
    /// <returns>True if an ammo count is found.</returns>
    public bool TryGetWeaponAmmo(EntityUid mount, [NotNullWhen(true)] out int? ammoCount, [NotNullWhen(true)] out int? ammoCapacity, WeaponMountComponent? mountComponent = null)
    {
        ammoCount = null;
        ammoCapacity = null;
        if (!Resolve(mount, ref mountComponent, false) || mountComponent.MountedEntity == null)
            return false;

        if (!_slots.TryGetSlot(mountComponent.MountedEntity.Value, "gun_magazine", out var itemSlot) ||  itemSlot.Item == null)
            return false;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(itemSlot.Item.Value, ref ammoEv);

        ammoCount = ammoEv.Count;
        ammoCapacity = ammoEv.Capacity;
        return true;
    }
}

/// <summary>
///     DoAfter event for attaching equipment to an equipment mount.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AttachToMountDoAfterEvent : SimpleDoAfterEvent
{

}

/// <summary>
///     DoAfter event for detaching equipment from an equipment mount.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DetachFromMountDoAfterEvent : SimpleDoAfterEvent
{

}

/// <summary>
///     DoAfter event for securing  equipment to an equipment mount.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SecureToMountDoAfterEvent : SimpleDoAfterEvent
{

}

/// <summary>
///     DoAfter event for deploying the mount.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MountDeployDoafterEvent : SimpleDoAfterEvent
{

}

/// <summary>
///     DoAfter event for attempting to pick up a deployed mount.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MountUnDeployDoAfterEvent : SimpleDoAfterEvent
{

}
