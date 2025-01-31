using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableHolderSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedVerbSystem _verbSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderComponent, AttachableAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableDetachDoAfterEvent>(OnDetachDoAfter);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableHolderAttachToSlotMessage>(OnAttachableHolderAttachToSlotMessage);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableHolderDetachMessage>(OnAttachableHolderDetachMessage);
        SubscribeLocalEvent<AttachableHolderComponent, AttemptShootEvent>(OnAttachableHolderAttemptShoot);
        SubscribeLocalEvent<AttachableHolderComponent, GunShotEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, BoundUIOpenedEvent>(OnAttachableHolderUiOpened);
        SubscribeLocalEvent<AttachableHolderComponent, EntInsertedIntoContainerMessage>(OnAttached);
        SubscribeLocalEvent<AttachableHolderComponent, MapInitEvent>(OnHolderMapInit,
            after: new[] { typeof(ContainerFillSystem) });
        SubscribeLocalEvent<AttachableHolderComponent, GetVerbsEvent<EquipmentVerb>>(OnAttachableHolderGetVerbs);
        SubscribeLocalEvent<AttachableHolderComponent, GotEquippedHandEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GotUnequippedHandEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GunRefreshModifiersEvent>(RelayEvent,
            after: new[] { typeof(WieldableSystem) });
        SubscribeLocalEvent<AttachableHolderComponent, InteractUsingEvent>(OnAttachableHolderInteractUsing);
        SubscribeLocalEvent<AttachableHolderComponent, ActivateInWorldEvent>(OnAttachableHolderInteractInWorld,
            before: new [] { typeof(CMGunSystem) });
        SubscribeLocalEvent<AttachableHolderComponent, ItemWieldedEvent>(OnHolderWielded);
        SubscribeLocalEvent<AttachableHolderComponent, ItemUnwieldedEvent>(OnHolderUnwielded);
        SubscribeLocalEvent<AttachableHolderComponent, UniqueActionEvent>(OnAttachableHolderUniqueAction);
        SubscribeLocalEvent<AttachableHolderComponent, GetGunDamageModifierEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GunMuzzleFlashAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, HandDeselectedEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetWieldableSpeedModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetWieldDelayEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, ContainerGettingInsertedAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, ContainerGettingRemovedAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, EntGotRemovedFromContainerMessage>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetItemSizeModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetFireModeValuesEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetFireModesEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetDamageFalloffEvent>(RelayEvent);
        SubscribeLocalEvent<AttachableHolderComponent, GetWeaponAccuracyEvent>(RelayEvent);


        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCActivateAttachableBarrel,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            ToggleAttachable(userUid, "rmc-aslot-barrel");
                    },
                    handle: false))
            .Bind(CMKeyFunctions.RMCActivateAttachableRail,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            ToggleAttachable(userUid, "rmc-aslot-rail");
                    },
                    handle: false))
            .Bind(CMKeyFunctions.RMCActivateAttachableStock,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            ToggleAttachable(userUid, "rmc-aslot-stock");
                    },
                    handle: false))
            .Bind(CMKeyFunctions.RMCActivateAttachableUnderbarrel,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            ToggleAttachable(userUid, "rmc-aslot-underbarrel");
                    },
                    handle: false))
            .Bind(CMKeyFunctions.RMCFieldStripHeldItem,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            FieldStripHeldItem(userUid);
                    },
                    handle: false))
            .Register<AttachableHolderSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<AttachableHolderSystem>();
    }

    private void OnHolderMapInit(Entity<AttachableHolderComponent> holder, ref MapInitEvent args)
    {
        var xform = Transform(holder.Owner);
        var coords = new EntityCoordinates(holder.Owner, Vector2.Zero);

        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            if (holder.Comp.Slots[slotId].StartingAttachable == null)
                continue;

            var container = _container.EnsureContainer<ContainerSlot>(holder, slotId);
            container.OccludesLight = false;

            var attachableUid = Spawn(holder.Comp.Slots[slotId].StartingAttachable, coords);
            if (!_container.Insert(attachableUid, container, containerXform: xform))
                continue;
        }

        Dirty(holder);
    }

    private void OnAttachableHolderInteractUsing(Entity<AttachableHolderComponent> holder, ref InteractUsingEvent args)
    {
        if (CanAttach(holder, args.Used))
        {
            StartAttach(holder, args.Used, args.User);
            args.Handled = true;
        }

        if (holder.Comp.SupercedingAttachable == null)
            return;

        var interactUsingEvent = new InteractUsingEvent(args.User,
            args.Used,
            holder.Comp.SupercedingAttachable.Value,
            args.ClickLocation);
        RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, interactUsingEvent);

        if (interactUsingEvent.Handled)
        {
            args.Handled = true;
            return;
        }

        var afterInteractEvent = new AfterInteractEvent(args.User,
            args.Used,
            holder.Comp.SupercedingAttachable.Value,
            args.ClickLocation,
            true);
        RaiseLocalEvent(args.Used, afterInteractEvent);

        if (afterInteractEvent.Handled)
            args.Handled = true;
    }

    private void OnAttachableHolderInteractInWorld(Entity<AttachableHolderComponent> holder, ref ActivateInWorldEvent args)
    {
        if (args.Handled || holder.Comp.SupercedingAttachable == null)
            return;

        var activateInWorldEvent = new ActivateInWorldEvent(args.User, holder.Comp.SupercedingAttachable.Value, args.Complex);
        RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, activateInWorldEvent);

        args.Handled = activateInWorldEvent.Handled;
    }

    private void OnAttachableHolderAttemptShoot(Entity<AttachableHolderComponent> holder, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (holder.Comp.SupercedingAttachable == null)
            return;

        args.Cancelled = true;

        if (!TryComp<GunComponent>(holder.Owner, out var holderGunComponent) ||
            holderGunComponent.ShootCoordinates == null ||
            !TryComp<GunComponent>(holder.Comp.SupercedingAttachable,
                out var attachableGunComponent))
        {
            return;
        }

        _gun.AttemptShoot(args.User,
            holder.Comp.SupercedingAttachable.Value,
            attachableGunComponent,
            holderGunComponent.ShootCoordinates.Value);
    }

    private void OnAttachableHolderUniqueAction(Entity<AttachableHolderComponent> holder, ref UniqueActionEvent args)
    {
        if (holder.Comp.SupercedingAttachable == null || args.Handled)
            return;

        RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, new UniqueActionEvent(args.UserUid));
        args.Handled = true;
    }

    private void OnHolderWielded(Entity<AttachableHolderComponent> holder, ref ItemWieldedEvent args)
    {
        AlterAllAttachables(holder, AttachableAlteredType.Wielded);
    }

    private void OnHolderUnwielded(Entity<AttachableHolderComponent> holder, ref ItemUnwieldedEvent args)
    {
        AlterAllAttachables(holder, AttachableAlteredType.Unwielded);
    }

    private void OnAttachableHolderDetachMessage(EntityUid holderUid,
        AttachableHolderComponent holderComponent,
        AttachableHolderDetachMessage args)
    {
        StartDetach((holderUid, holderComponent), args.Slot, args.Actor);
    }

    private void OnAttachableHolderGetVerbs(Entity<AttachableHolderComponent> holder, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (HasComp<XenoComponent>(args.User))
            return;

        EnsureSlots(holder);
        var userUid = args.User;

        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            if (_container.TryGetContainer(holder.Owner, slotId, out var container))
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (!TryComp(contained, out AttachableToggleableComponent? toggleableComponent))
                        continue;

                    if (toggleableComponent.UserOnly &&
                        (!TryComp(holder.Owner, out TransformComponent? transformComponent) || !transformComponent.ParentUid.Valid || transformComponent.ParentUid != userUid))
                    {
                        continue;
                    }

                    var verb = new EquipmentVerb()
                    {
                        Text = toggleableComponent.ActionName,
                        IconEntity = GetNetEntity(contained),
                        Act = () =>
                        {
                            var ev = new AttachableToggleStartedEvent(holder.Owner, userUid, slotId);
                            RaiseLocalEvent(contained, ref ev);
                        }
                    };

                    args.Verbs.Add(verb);
                }
            }
        }
    }

    private void OnAttachableHolderAttachToSlotMessage(EntityUid holderUid,
        AttachableHolderComponent holderComponent,
        AttachableHolderAttachToSlotMessage args)
    {
        TryComp<HandsComponent>(args.Actor, out var handsComponent);

        if (handsComponent == null)
            return;

        _hands.TryGetActiveItem((args.Actor, handsComponent), out var attachableUid);

        if (attachableUid == null)
            return;

        StartAttach((holderUid, holderComponent), attachableUid.Value, args.Actor, args.Slot);
    }

    private void OnAttachableHolderUiOpened(EntityUid holderUid,
        AttachableHolderComponent holderComponent,
        BoundUIOpenedEvent args)
    {
        UpdateStripUi(holderUid);
    }

    public void StartAttach(Entity<AttachableHolderComponent> holder,
        EntityUid attachableUid,
        EntityUid userUid,
        string slotId = "")
    {
        if (HasComp<XenoComponent>(userUid))
            return;

        var validSlots = GetValidSlots(holder, attachableUid);

        if (validSlots.Count == 0)
            return;

        if (string.IsNullOrEmpty(slotId))
        {
            if (validSlots.Count > 1)
            {
                TryComp<UserInterfaceComponent>(holder.Owner,
                    out var userInterfaceComponent);
                _ui.OpenUi((holder.Owner, userInterfaceComponent), AttachmentUI.ChooseSlotKey, userUid);

                var state =
                    new AttachableHolderChooseSlotUserInterfaceState(validSlots);
                _ui.SetUiState(holder.Owner, AttachmentUI.ChooseSlotKey, state);
                return;
            }

            slotId = validSlots[0];
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            userUid,
            Comp<AttachableComponent>(attachableUid).AttachDoAfter,
            new AttachableAttachDoAfterEvent(slotId),
            holder,
            target: holder.Owner,
            used: attachableUid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });
    }

    private void OnAttachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target || args.Used is not { } used)
            return;

        if (!TryComp(args.Target, out AttachableHolderComponent? holder) ||
            !HasComp<AttachableComponent>(args.Used))
            return;

        if (Attach((target, holder), used, args.User, args.SlotId))
            args.Handled = true;
    }

    public bool Attach(Entity<AttachableHolderComponent> holder,
        EntityUid attachableUid,
        EntityUid userUid,
        string slotId = "")
    {
        if (!CanAttach(holder, attachableUid, ref slotId))
            return false;

        var container = _container.EnsureContainer<ContainerSlot>(holder, slotId);
        container.OccludesLight = false;

        if (container.Count > 0 && !Detach(holder, container.ContainedEntities[0], userUid, slotId))
            return false;

        if (!_container.Insert(attachableUid, container))
            return false;

        if(_hands.IsHolding(userUid, holder.Owner))
        {
            var addEv = new GrantAttachableActionsEvent(userUid);
            RaiseLocalEvent(attachableUid, ref addEv);
        }

        Dirty(holder);

        _audio.PlayPredicted(Comp<AttachableComponent>(attachableUid).AttachSound,
            holder,
            userUid);

        return true;
    }

    private void OnAttached(Entity<AttachableHolderComponent> holder, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out AttachableComponent? attachableComponent) || !holder.Comp.Slots.ContainsKey(args.Container.ID))
            return;

        UpdateStripUi(holder.Owner, holder.Comp);

        var ev = new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Attached);
        RaiseLocalEvent(args.Entity, ref ev);

        var holderEv = new AttachableHolderAttachablesAlteredEvent(args.Entity, args.Container.ID, AttachableAlteredType.Attached);
        RaiseLocalEvent(holder, ref holderEv);
    }

    //Detaching
    public void StartDetach(Entity<AttachableHolderComponent> holder, string slotId, EntityUid userUid)
    {
        if (TryGetAttachable(holder, slotId, out var attachable) && holder.Comp.Slots.ContainsKey(slotId) && !holder.Comp.Slots[slotId].Locked)
            StartDetach(holder, attachable.Owner, userUid);
    }

    public void StartDetach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, EntityUid userUid)
    {
        if (HasComp<XenoComponent>(userUid))
            return;

        var delay = Comp<AttachableComponent>(attachableUid).AttachDoAfter;
        var args = new DoAfterArgs(
            EntityManager,
            userUid,
            delay,
            new AttachableDetachDoAfterEvent(),
            holder,
            holder.Owner,
            attachableUid)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnDetachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        if (!TryComp(args.Target, out AttachableHolderComponent? holderComponent) || !HasComp<AttachableComponent>(args.Used))
            return;

        if (!Detach((args.Target.Value, holderComponent), args.Used.Value, args.User))
            return;

        args.Handled = true;
    }

    public bool Detach(Entity<AttachableHolderComponent> holder,
        EntityUid attachableUid,
        EntityUid userUid,
        string? slotId = null)
    {
        if (TerminatingOrDeleted(holder) || !holder.Comp.Running)
            return false;

        if (string.IsNullOrEmpty(slotId) && !TryGetSlotId(holder.Owner, attachableUid, out slotId))
            return false;

        if (!_container.TryGetContainer(holder, slotId, out var container) || container.Count <= 0)
            return false;

        if (!TryGetAttachable(holder, slotId, out var attachable))
            return false;

        if (!_container.Remove(attachable.Owner, container, force: true))
            return false;

        UpdateStripUi(holder.Owner, holder.Comp);

        var ev = new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Detached, userUid);
        RaiseLocalEvent(attachableUid, ref ev);

        var holderEv = new AttachableHolderAttachablesAlteredEvent(attachableUid, slotId, AttachableAlteredType.Detached);
        RaiseLocalEvent(holder.Owner, ref holderEv);

        var removeEv = new RemoveAttachableActionsEvent(userUid);
        RaiseLocalEvent(attachableUid, ref removeEv);

        _audio.PlayPredicted(Comp<AttachableComponent>(attachableUid).DetachSound,
            holder,
            userUid);

        Dirty(holder);
        _hands.TryPickupAnyHand(userUid, attachable);
        return true;
    }

    private bool CanAttach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid)
    {
        var slotId = "";
        return CanAttach(holder, attachableUid, ref slotId);
    }

    private bool CanAttach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, ref string slotId)
    {
        if (!HasComp<AttachableComponent>(attachableUid))
            return false;

        if (!string.IsNullOrWhiteSpace(slotId))
            return _whitelist.IsWhitelistPass(holder.Comp.Slots[slotId].Whitelist, attachableUid);

        foreach (var key in holder.Comp.Slots.Keys)
        {
            if (_whitelist.IsWhitelistPass(holder.Comp.Slots[key].Whitelist, attachableUid))
            {
                slotId = key;
                return true;
            }
        }

        return false;
    }

    private Dictionary<string, (string?, bool)> GetSlotsForStripUi(Entity<AttachableHolderComponent> holder)
    {
        var result = new Dictionary<string, (string?, bool)>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();

        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            if (TryGetAttachable(holder, slotId, out var attachable) &&
                metaQuery.TryGetComponent(attachable.Owner, out var metadata))
            {
                result.Add(slotId, (metadata.EntityName, holder.Comp.Slots[slotId].Locked));
            }
            else
            {
                result.Add(slotId, (null, holder.Comp.Slots[slotId].Locked));
            }
        }

        return result;
    }

    public bool TryGetAttachable(Entity<AttachableHolderComponent> holder,
        string slotId,
        out Entity<AttachableComponent> attachable)
    {
        attachable = default;

        if (!_container.TryGetContainer(holder, slotId, out var container) || container.Count <= 0)
            return false;

        var ent = container.ContainedEntities[0];
        if (!TryComp(ent, out AttachableComponent? attachableComp))
            return false;

        attachable = (ent, attachableComp);
        return true;
    }

    private void UpdateStripUi(EntityUid holderUid, AttachableHolderComponent? holderComponent = null)
    {
        if (!Resolve(holderUid, ref holderComponent))
            return;

        var state =
            new AttachableHolderStripUserInterfaceState(GetSlotsForStripUi((holderUid, holderComponent)));
        _ui.SetUiState(holderUid, AttachmentUI.StripKey, state);
    }

    private void EnsureSlots(Entity<AttachableHolderComponent> holder)
    {
        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            var container = _container.EnsureContainer<ContainerSlot>(holder, slotId);
            container.OccludesLight = false;
        }
    }

    private List<string> GetValidSlots(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, bool ignoreLock = false)
    {
        var list = new List<string>();

        if (!HasComp<AttachableComponent>(attachableUid))
            return list;

        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            if (_whitelist.IsWhitelistPass(holder.Comp.Slots[slotId].Whitelist, attachableUid) && (!ignoreLock || !holder.Comp.Slots[slotId].Locked))
                list.Add(slotId);
        }

        return list;
    }

    private void ToggleAttachable(EntityUid userUid, string slotId)
    {
        if (!TryComp<HandsComponent>(userUid, out var handsComponent) ||
            !TryComp<AttachableHolderComponent>(handsComponent.ActiveHandEntity, out var holderComponent))
        {
            return;
        }

        var active = handsComponent.ActiveHandEntity;
        if (!holderComponent.Running || !_actionBlocker.CanInteract(userUid, active))
            return;

        if (!_container.TryGetContainer(active.Value,
                slotId,
                out var container) || container.Count <= 0)
            return;

        var attachableUid = container.ContainedEntities[0];

        if (!HasComp<AttachableToggleableComponent>(attachableUid))
            return;

        if (!TryComp<AttachableToggleableComponent>(attachableUid, out var toggleableComponent))
            return;

        var ev = new AttachableToggleStartedEvent(active.Value, userUid, slotId);
        RaiseLocalEvent(attachableUid, ref ev);
    }

    private void FieldStripHeldItem(EntityUid userUid)
    {
        if (!TryComp<HandsComponent>(userUid, out var handsComponent) ||
            !TryComp<AttachableHolderComponent>(handsComponent.ActiveHandEntity, out var holderComponent))
        {
            return;
        }

        EntityUid holderUid = handsComponent.ActiveHandEntity.Value;

        if (!holderComponent.Running || !_actionBlocker.CanInteract(userUid, holderUid))
            return;

        foreach (var verb in _verbSystem.GetLocalVerbs(holderUid, userUid, typeof(Verb)))
        {
            if (!verb.Text.Equals(Loc.GetString("rmc-verb-strip-attachables")))
                continue;

            _verbSystem.ExecuteVerb(verb, userUid, holderUid);
            break;
        }
    }

    public void SetSupercedingAttachable(Entity<AttachableHolderComponent> holder, EntityUid? supercedingAttachable)
    {
        holder.Comp.SupercedingAttachable = supercedingAttachable;
        Dirty(holder);
    }

    public bool TryGetSlotId(EntityUid holderUid, EntityUid attachableUid, [NotNullWhen(true)] out string? slotId)
    {
        slotId = null;

        if (!TryComp<AttachableHolderComponent>(holderUid, out var holderComponent) ||
            !TryComp<AttachableComponent>(attachableUid, out _))
        {
            return false;
        }

        foreach (var id in holderComponent.Slots.Keys)
        {
            if (!_container.TryGetContainer(holderUid, id, out var container) || container.Count <= 0)
                continue;

            if (container.ContainedEntities[0] != attachableUid)
                continue;

            slotId = id;
            return true;
        }

        return false;
    }

    public bool HasSlot(Entity<AttachableHolderComponent?> holder, string slotId)
    {
        if (holder.Comp == null)
        {
            if (!TryComp(holder.Owner, out AttachableHolderComponent? holderComponent))
                return false;

            holder.Comp = holderComponent;
        }

        return holder.Comp.Slots.ContainsKey(slotId);
    }

    public bool TryGetHolder(EntityUid attachable, [NotNullWhen(true)] out EntityUid? holderUid)
    {
        if (!TryComp(attachable, out TransformComponent? transformComponent) ||
            !transformComponent.ParentUid.Valid ||
            !HasComp<AttachableHolderComponent>(transformComponent.ParentUid))
        {
            holderUid = null;
            return false;
        }

        holderUid = transformComponent.ParentUid;
        return true;
    }

    public bool TryGetUser(EntityUid attachable, [NotNullWhen(true)] out EntityUid? userUid)
    {
        userUid = null;

        if (!TryGetHolder(attachable, out var holderUid))
            return false;

        if (!TryComp(holderUid, out TransformComponent? transformComponent) || !transformComponent.ParentUid.Valid)
            return false;

        userUid = transformComponent.ParentUid;
        return true;
    }

    public void RelayEvent<T>(Entity<AttachableHolderComponent> holder, ref T args) where T : notnull
    {
        var ev = new AttachableRelayedEvent<T>(args, holder.Owner);

        foreach (var slot in holder.Comp.Slots.Keys)
        {
            if (_container.TryGetContainer(holder, slot, out var container))
            {
                foreach (var contained in container.ContainedEntities)
                {
                    RaiseLocalEvent(contained, ev);
                }
            }
        }

        args = ev.Args;
    }

    private void AlterAllAttachables(Entity<AttachableHolderComponent> holder, AttachableAlteredType alteration)
    {
        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            if (!_container.TryGetContainer(holder, slotId, out var container) || container.Count <= 0)
                continue;

            var ev = new AttachableAlteredEvent(holder.Owner, alteration);
            RaiseLocalEvent(container.ContainedEntities[0], ref ev);
        }
    }
}
