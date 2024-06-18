using Content.Shared._CM14.Input;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared._CM14.Attachable;

public abstract class SharedAttachableHolderSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAttachableWeaponRangedModsSystem _attachableWeaponRangedModsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    
    [Dependency] protected readonly IEntityManager EntityManager = default!;
    
    private EntityQuery<MetaDataComponent> metaQuery;


    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderComponent, GunRefreshModifiersEvent>(OnAttachableHolderRefreshModifiers, after: new[] { typeof(WieldableSystem) });
        SubscribeLocalEvent<AttachableHolderComponent, InteractUsingEvent>(OnAttachableHolderInteractUsing);
        SubscribeLocalEvent<AttachableHolderComponent, BoundUIOpenedEvent>(OnAttachableHolderUiOpened);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableDetachDoAfterEvent>(OnDetachDoAfter);
        SubscribeLocalEvent<AttachableHolderComponent, GetVerbsEvent<InteractionVerb>>(OnAttachableHolderGetVerbs);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableHolderDetachMessage>(OnAttachableHolderDetachMessage);
        SubscribeLocalEvent<AttachableHolderComponent, AttachableHolderAttachToSlotMessage>(OnAttachableHolderAttachToSlotMessage);
        SubscribeLocalEvent<AttachableHolderComponent, EntInsertedIntoContainerMessage>(OnAttached);
        SubscribeLocalEvent<AttachableHolderComponent, EntRemovedFromContainerMessage>(OnDetached);
        SubscribeLocalEvent<AttachableHolderComponent, ItemWieldedEvent>(OnHolderWielded);
        SubscribeLocalEvent<AttachableHolderComponent, ItemUnwieldedEvent>(OnHolderUnwielded);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.CMActivateAttachableBarrel,
                InputCmdHandler.FromDelegate(session =>
                {
                    if(session?.AttachedEntity is { } userUid)
                        ToggleAttachable(userUid, "cm-aslot-barrel");
                }, handle: false))
            .Bind(CMKeyFunctions.CMActivateAttachableRail,
                InputCmdHandler.FromDelegate(session =>
                {
                    if(session?.AttachedEntity is { } userUid)
                        ToggleAttachable(userUid, "cm-aslot-rail");
                }, handle: false))
            .Bind(CMKeyFunctions.CMActivateAttachableStock,
                InputCmdHandler.FromDelegate(session =>
                {
                    if(session?.AttachedEntity is { } userUid)
                        ToggleAttachable(userUid, "cm-aslot-stock");
                }, handle: false))
            .Bind(CMKeyFunctions.CMActivateAttachableUnderbarrel,
                InputCmdHandler.FromDelegate(session =>
                {
                    if(session?.AttachedEntity is { } userUid)
                        ToggleAttachable(userUid, "cm-aslot-underbarrel");
                }, handle: false))
            .Register<SharedAttachableHolderSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<SharedAttachableHolderSystem>();
    }


    private void OnAttachableHolderInteractUsing(Entity<AttachableHolderComponent> holder, ref InteractUsingEvent args)
    {
        if(CanAttach(holder, args.Used))
        {
            StartAttach(holder, args.Used, args.User);
            args.Handled = true;
        }
    }

    private void OnAttachableHolderRefreshModifiers(Entity<AttachableHolderComponent> holder, ref GunRefreshModifiersEvent args)
    {
        foreach(string slotID in holder.Comp.Slots.Keys)
        {
            if(!_containerSystem.TryGetContainer(holder, slotID, out BaseContainer? container) || container.Count <= 0)
                continue;
            
            EntityUid attachableUid = container.ContainedEntities[0];
            
            if(!EntityManager.TryGetComponent<AttachableComponent>(attachableUid, out _))
                continue;
            
            _attachableWeaponRangedModsSystem.ApplyWeaponModifiers(attachableUid, ref args);
        }
    }
    
    private void OnHolderWielded(Entity<AttachableHolderComponent> holder, ref ItemWieldedEvent args)
    {
        ApplyModifiers(holder, AttachableAlteredType.Wielded);
    }
    
    private void OnHolderUnwielded(Entity<AttachableHolderComponent> holder, ref ItemUnwieldedEvent args)
    {
        ApplyModifiers(holder, AttachableAlteredType.Unwielded);
    }
    
    private void OnAttachableHolderDetachMessage(EntityUid holderUid, AttachableHolderComponent holderComponent, AttachableHolderDetachMessage args)
    {
        StartDetach((holderUid, holderComponent), args.Slot, args.Actor);
    }
    
    private void OnAttachableHolderGetVerbs(Entity<AttachableHolderComponent> holder, ref GetVerbsEvent<InteractionVerb> args)
    {
        EnsureSlots(holder);
    }
    
    private void OnAttachableHolderAttachToSlotMessage(EntityUid holderUid, AttachableHolderComponent holderComponent, AttachableHolderAttachToSlotMessage args)
    {
        EntityManager.TryGetComponent<HandsComponent>(args.Actor, out HandsComponent? handsComponent);
        
        if(handsComponent == null)
            return;
        
        _handsSystem.TryGetActiveItem((args.Actor, handsComponent), out EntityUid? attachableUid);
        
        if(attachableUid == null)
            return;
        
        StartAttach((holderUid, holderComponent), attachableUid.Value, args.Actor, args.Slot);
    }
    
    private void OnAttachableHolderUiOpened(EntityUid holderUid, AttachableHolderComponent holderComponent, BoundUIOpenedEvent args)
    {
        UpdateStripUi(holderUid);
    }
    
    //Attaching
    public void StartAttach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, EntityUid userUid, string slotID = "")
    {
        List<string> validSlots = GetValidSlots(holder, attachableUid);
        
        if(validSlots.Count == 0)
            return;
        
        if(String.IsNullOrEmpty(slotID))
        {
            if(validSlots.Count > 1)
            {
                EntityManager.TryGetComponent<UserInterfaceComponent>(holder.Owner, out UserInterfaceComponent? userInterfaceComponent);
                _uiSystem.OpenUi((holder.Owner, userInterfaceComponent), AttachableHolderUiKeys.ChooseSlotKey, userUid);
                
                AttachableHolderChooseSlotUserInterfaceState state = new AttachableHolderChooseSlotUserInterfaceState(validSlots);
                _uiSystem.SetUiState(holder.Owner, AttachableHolderUiKeys.ChooseSlotKey, state);
                return;
            }
            slotID = validSlots[0];
        }
        
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            userUid,
            EntityManager.GetComponent<AttachableComponent>(attachableUid).AttachDoAfter,
            new AttachableAttachDoAfterEvent(slotID),
            holder,
            target: holder.Owner,
            used: attachableUid)
        {
            NeedHand = true,
            BreakOnMove = true
        });
    }
    
    protected virtual void OnAttachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableAttachDoAfterEvent args)
    {
        if(args.Cancelled || args.Handled || args.Args.Target == null || args.Args.Used == null)
            return;
        
        if(!HasComp<AttachableHolderComponent>(args.Args.Target) || !HasComp<AttachableComponent>(args.Args.Used))
            return;
        
        if(!Attach((args.Args.Target.Value, EntityManager.GetComponent<AttachableHolderComponent>(args.Args.Target.Value)), args.Args.Used.Value, args.Args.User, args.SlotID))
            return;
        
        args.Handled = true;
    }
    
    public bool Attach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, EntityUid userUid, string slotID = "")
    {
        if(!CanAttach(holder, attachableUid, ref slotID))
            return false;
        
        ContainerSlot container = _containerSystem.EnsureContainer<ContainerSlot>(holder, slotID);
        
        if(container.Count > 0 && !Detach(holder, attachableUid, userUid, slotID))
            return false;
        
        if(!_containerSystem.Insert(attachableUid, container))
            return false;
        
        return true;
    }
    
    protected virtual void OnAttached(Entity<AttachableHolderComponent> holder, ref EntInsertedIntoContainerMessage args)
    {
        if(!GetSlots(holder).Contains(args.Container.ID))
            return;
        
        UpdateStripUi(holder.Owner, holder.Comp);
        
        RaiseLocalEvent(holder, new AttachableHolderAttachablesAlteredEvent(args.Entity, args.Container.ID, AttachableAlteredType.Attached));
        RaiseLocalEvent(args.Entity, new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Attached));
        
        if(EntityManager.TryGetComponent<GunComponent>(holder.Owner, out GunComponent? gunComponent))
            _gunSystem.RefreshModifiers((holder.Owner, gunComponent));
        
        Dirty(holder);
    }
    
    //Detaching
    public void StartDetach(Entity<AttachableHolderComponent> holder, string slotID, EntityUid userUid)
    {
        if(TryGetAttachable(holder, slotID, out Entity<AttachableComponent> attachable))
            StartDetach(holder, attachable.Owner, userUid);
    }
    
    public void StartDetach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, EntityUid userUid)
    {
        double doAfter = EntityManager.GetComponent<AttachableComponent>(attachableUid).AttachDoAfter;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            userUid,
            EntityManager.GetComponent<AttachableComponent>(attachableUid).AttachDoAfter,
            new AttachableDetachDoAfterEvent(),
            holder,
            target: holder.Owner,
            used: attachableUid)
        {
            NeedHand = true,
            BreakOnMove = true
        });
    }
    
    protected virtual void OnDetachDoAfter(EntityUid uid, AttachableHolderComponent component, AttachableDetachDoAfterEvent args)
    {
        if(args.Cancelled || args.Handled || args.Args.Target == null || args.Args.Used == null)
            return;
        
        if(!HasComp<AttachableHolderComponent>(args.Args.Target) || !HasComp<AttachableComponent>(args.Args.Used))
            return;
        
        if(!Detach((args.Args.Target.Value, EntityManager.GetComponent<AttachableHolderComponent>(args.Args.Target.Value)), args.Args.Used.Value, args.Args.User))
            return;
        
        args.Handled = true;
    }
    
    public bool Detach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, EntityUid userUid, string slotID = "")
    {
        if(TerminatingOrDeleted(holder) || !holder.Comp.Running)
            return false;
        
        if(String.IsNullOrEmpty(slotID))
            CanAttach(holder, attachableUid, ref slotID);
        
        if(!_containerSystem.TryGetContainer(holder, slotID, out var container) || container.Count <= 0)
            return false;
        
        if(!TryGetAttachable(holder, slotID, out Entity<AttachableComponent> attachable))
            return false;
        
        _containerSystem.TryRemoveFromContainer(attachable);
        _handsSystem.TryPickupAnyHand(userUid, attachable);
        return true;
    }
    
    protected virtual void OnDetached(Entity<AttachableHolderComponent> holder, ref EntRemovedFromContainerMessage args)
    {
        if(!GetSlots(holder).Contains(args.Container.ID))
            return;
        
        UpdateStripUi(holder.Owner, holder.Comp);
        
        RaiseLocalEvent(holder, new AttachableHolderAttachablesAlteredEvent(args.Entity, args.Container.ID, AttachableAlteredType.Detached));
        RaiseLocalEvent(args.Entity, new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Detached));
        
        if(EntityManager.TryGetComponent<GunComponent>(holder.Owner, out GunComponent? gunComponent))
            _gunSystem.RefreshModifiers((holder.Owner, gunComponent));
        
        Dirty(holder);
    }
    
    
    private bool CanAttach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid)
    {
        string slotID = "";
        return CanAttach(holder, attachableUid, ref slotID);
    }
    
    private bool CanAttach(Entity<AttachableHolderComponent> holder, EntityUid attachableUid, ref string slotID)
    {
        if(!HasComp<AttachableComponent>(attachableUid))
            return false;
        
        if(!String.IsNullOrEmpty(slotID))
            return _whitelistSystem.IsWhitelistPass(holder.Comp.Slots[slotID], attachableUid);
        
        foreach(string key in holder.Comp.Slots.Keys)
        {
            if(_whitelistSystem.IsWhitelistPass(holder.Comp.Slots[key], attachableUid))
            {
                slotID = key;
                return true;
            }
        }
        
        return false;
    }
    
    private Dictionary<string, string?> GetSlotsForStripUi(Entity<AttachableHolderComponent> holder)
    {
        Dictionary<string, string?> result = new Dictionary<string, string?>();
        EntityQuery<MetaDataComponent> metaQuery = EntityManager.GetEntityQuery<MetaDataComponent>();
        
        foreach(string slotID in holder.Comp.Slots.Keys)
        {
            if(TryGetAttachable(holder, slotID, out Entity<AttachableComponent> attachable) && metaQuery.TryGetComponent(attachable.Owner, out MetaDataComponent? metadata) && metadata != null)
                result.Add(slotID, metadata.EntityName);
            else
                result.Add(slotID, null);
        }
        
        return result;
    }
    
    public bool TryGetAttachable(Entity<AttachableHolderComponent> holder, string slotID, out Entity<AttachableComponent> attachable)
    {
        attachable = default;
        
        if(!_containerSystem.TryGetContainer(holder, slotID, out var container) || container.Count <= 0)
            return false;
        
        var ent = container.ContainedEntities[0];
        if(!TryComp(ent, out AttachableComponent? attachableComp))
            return false;
        
        attachable = (ent, attachableComp);
        return true;
    }
    
    private void UpdateStripUi(EntityUid holderUid, AttachableHolderComponent? holderComponent = null)
    {
        if(!Resolve(holderUid, ref holderComponent))
            return;

        AttachableHolderStripUserInterfaceState state = new AttachableHolderStripUserInterfaceState(GetSlotsForStripUi((holderUid, holderComponent)));
        _uiSystem.SetUiState(holderUid, AttachableHolderUiKeys.StripKey, state);
    }
    
    private void EnsureSlots(Entity<AttachableHolderComponent> holder)
    {
        foreach(string slotID in holder.Comp.Slots.Keys)
            _containerSystem.EnsureContainer<ContainerSlot>(holder, slotID);
    }
    
    private List<string> GetValidSlots(Entity<AttachableHolderComponent> holder, EntityUid attachableUid)
    {
        List<string> list = new List<string>();
        
        if(!HasComp<AttachableComponent>(attachableUid))
            return list;
        
        foreach(string slotID in holder.Comp.Slots.Keys)
            if(_whitelistSystem.IsWhitelistPass(holder.Comp.Slots[slotID], attachableUid))
                list.Add(slotID);
        
        return list;
    }
    
    private List<string> GetSlots(Entity<AttachableHolderComponent> holder)
    {
        return new List<string>(holder.Comp.Slots.Keys);
    }
    
    public void ApplyModifiers(Entity<AttachableHolderComponent> holder, AttachableAlteredType attachableAltered)
    {
        foreach(string slotID in holder.Comp.Slots.Keys)
        {
            if(!_containerSystem.TryGetContainer(holder, slotID, out BaseContainer? container) || container.Count <= 0)
                continue;
            
            EntityUid attachableUid = container.ContainedEntities[0];
            
            if(!EntityManager.TryGetComponent<AttachableComponent>(attachableUid, out _))
                continue;
            
            RaiseLocalEvent(attachableUid, new AttachableAlteredEvent(holder.Owner, attachableAltered));
        }
    }
    
    private void ToggleAttachable(EntityUid userUid, string slotID)
    {
        if(!EntityManager.TryGetComponent<HandsComponent>(userUid, out HandsComponent? handsComponent) ||
            !EntityManager.TryGetComponent<AttachableHolderComponent>(handsComponent.ActiveHandEntity, out AttachableHolderComponent? holderComponent))
            return;
        
        if(!holderComponent.Running || !_actionBlockerSystem.CanInteract(userUid, handsComponent.ActiveHandEntity))
            return;
        
        if(!_containerSystem.TryGetContainer(handsComponent.ActiveHandEntity.Value, slotID, out BaseContainer? container) || container.Count <= 0)
            return;
        
        EntityUid attachableUid = container.ContainedEntities[0];
        
        if(!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent))
            return;
        
        RaiseLocalEvent(attachableUid, new AttachableToggleStartedEvent((handsComponent.ActiveHandEntity.Value, holderComponent), userUid, slotID));
    }
    
    public void SetSupercedingAttachable(Entity<AttachableHolderComponent> holder, EntityUid? supercedingAttachable)
    {
        holder.Comp.SupercedingAttachable = supercedingAttachable;
    }
    
    public bool TryGetSlotID(EntityUid holderUid, EntityUid attachableUid, [NotNullWhen(true)] out string? slotID)
    {
        slotID = null;
        
        if(!EntityManager.TryGetComponent<AttachableHolderComponent>(holderUid, out AttachableHolderComponent? holderComponent) || 
            !EntityManager.TryGetComponent<AttachableComponent>(attachableUid, out _))
            return false;
        
        foreach(string id in holderComponent.Slots.Keys)
        {
            if(!_containerSystem.TryGetContainer(holderUid, id, out BaseContainer? container) || container.Count <= 0)
                continue;
            
            if(container.ContainedEntities[0] != attachableUid)
                continue;
            
            slotID = id;
            return true;
        }
        return false;
    }
}
