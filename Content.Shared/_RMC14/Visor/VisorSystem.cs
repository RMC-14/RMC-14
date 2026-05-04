using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Scoping;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Visor;

public sealed class VisorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InventoryComponent, ScopedEvent>(OnInventoryScoped);

        SubscribeLocalEvent<CycleableVisorComponent, GetEquipmentVisualsEvent>(OnCycleableVisorGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<CycleableVisorComponent, GetItemActionsEvent>(OnCycleableVisorGetItemActions);
        SubscribeLocalEvent<CycleableVisorComponent, CycleVisorActionEvent>(OnCycleableVisorAction);
        SubscribeLocalEvent<CycleableVisorComponent, InteractUsingEvent>(OnCycleableVisorInteractUsing, before: [typeof(SharedStorageSystem)]);
        SubscribeLocalEvent<CycleableVisorComponent, InventoryRelayedEvent<ScopedEvent>>(OnCycleableVisorScoped);
        SubscribeLocalEvent<CycleableVisorComponent, GotEquippedEvent>(OnCycleableVisorEquipped);

        SubscribeLocalEvent<VisorComponent, ActivateVisorAttemptEvent>(OnVisorAttemptActivate);
        SubscribeLocalEvent<VisorComponent, ActivateVisorEvent>(OnVisorActivate);
        SubscribeLocalEvent<VisorComponent, DeactivateVisorEvent>(OnVisorDeactivate);
        SubscribeLocalEvent<VisorComponent, PowerCellChangedEvent>(OnCycleableVisorPowerCellChanged, after: [typeof(SharedPowerCellSystem)]);

        SubscribeLocalEvent<ToggleVisorComponent, ActivateVisorAttemptEvent>(OnToggleVisorAttemptActivate);
        SubscribeLocalEvent<ToggleVisorComponent, ActivateVisorEvent>(OnToggleVisorActivate);
        SubscribeLocalEvent<ToggleVisorComponent, DeactivateVisorEvent>(OnToggleVisorDeactivate);

        SubscribeLocalEvent<IntegratedVisorsComponent, MapInitEvent>(OnIntegratedVisorsInit, after: [typeof(SharedItemSystem)]);
    }

    private void OnInventoryScoped(Entity<InventoryComponent> ent, ref ScopedEvent args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnCycleableVisorGetItemActions(Entity<CycleableVisorComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);

        if (ent.Comp.CurrentVisor != null)
        {
            if (!ent.Comp.Containers.TryGetValue(ent.Comp.CurrentVisor.Value, out var currentId))
                return;

            if (!_container.TryGetContainer(ent, currentId, out var currentContainer))
                return;

            if (!TryComp<VisorComponent>(currentContainer.ContainedEntities.FirstOrDefault(), out var visorComp))
                return;

            if (ent.Comp.Action is { } action)
                _actions.SetIcon(action, visorComp.OnIcon);
        }
    }

    private void OnCycleableVisorGetEquipmentVisuals(Entity<CycleableVisorComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.CurrentVisor == null)
            return;

        if (!ent.Comp.Containers.TryGetValue(ent.Comp.CurrentVisor.Value, out var currentId))
            return;

        if (!_container.TryGetContainer(ent, currentId, out var currentContainer))
            return;

        if (!TryComp<VisorComponent>(currentContainer.ContainedEntities.FirstOrDefault(), out var visorComp))
            return;

        if (visorComp.ToggledSprite == null)
            return;

        if (_inventory.TryGetSlot(args.Equipee, args.Slot, out var slot) &&
            (slot.SlotFlags & visorComp.Slot) == 0)
        {
            return;
        }

        var layer = $"enum.{nameof(VisorVisualLayers)}.{VisorVisualLayers.Base}";
        if (args.Layers.Any(l => l.Item1 == layer))
            return;

        args.Layers.Add((layer, new PrototypeLayerData
        {
            RsiPath = visorComp.ToggledSprite.RsiPath.ToString(),
            State = visorComp.ToggledSprite.RsiState,
            Visible = true,
        }));

        if (ent.Comp.Action is { } action)
            _actions.SetIcon(action, visorComp.OnIcon);
    }

    private void OnCycleableVisorAction(Entity<CycleableVisorComponent> ent, ref CycleVisorActionEvent args)
    {
        var containers = new List<ContainerSlot>();
        foreach (var id in ent.Comp.Containers)
        {
            containers.Add(_container.EnsureContainer<ContainerSlot>(ent, id));
        }

        if (containers.Count == 0)
            return;

        if (containers.All(c => c.ContainedEntity == null))
        {
            _popup.PopupClient(Loc.GetString("rmc-no-visors-to-swap"), ent, args.Performer, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        ref var current = ref ent.Comp.CurrentVisor;
        if (current != null &&
            containers.TryGetValue(current.Value, out var currentContainer) &&
            currentContainer.ContainedEntity is { } currentContained)
        {
            var ev = new DeactivateVisorEvent(ent, args.Performer);
            RaiseLocalEvent(currentContained, ref ev);
        }

        bool startedNull = current == null;

        do
        {
            current = current == null ? 0 : current + 1;
            Dirty(ent);

            if (current >= containers.Count)
            {
                current = null;
                break;
            }

            if (current != null &&
                containers.TryGetValue(current.Value, out currentContainer) &&
                currentContainer.ContainedEntity is { } newContained)
            {
                if (!_powerCell.HasDrawCharge(newContained, user: args.Performer))
                    continue;

                var rev = new ActivateVisorAttemptEvent(args.Performer);
                RaiseLocalEvent(newContained, ref rev);

                if (rev.Cancelled)
                    continue;

                var ev = new ActivateVisorEvent(ent, args.Performer);
                RaiseLocalEvent(newContained, ref ev);

                if (!ev.Handled)
                    current = null;
                else
                    break;

            }
            else
                continue;
        } while (current != null);

        if (startedNull && current == null)
            _popup.PopupClient(Loc.GetString("rmc-no-visors-to-swap"), ent, args.Performer, PopupType.SmallCaution);

        if (ent.Comp.Action is { } action && current == null)
            _actions.SetIcon(action, ent.Comp.OffIcon);

        _item.VisualsChanged(ent);
    }

    /// <summary>
    /// Check if we can still use the equipped visor
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnCycleableVisorEquipped(Entity<CycleableVisorComponent> ent, ref GotEquippedEvent args)
    {
        var containers = new List<ContainerSlot>();
        foreach (var id in ent.Comp.Containers)
        {
            containers.Add(_container.EnsureContainer<ContainerSlot>(ent, id));
        }

        if (containers.Count == 0)
            return;

        ref var current = ref ent.Comp.CurrentVisor;

        if (current != null &&
            containers.TryGetValue(current.Value, out var currentContainer) &&
            currentContainer.ContainedEntity is { } newContained)
        {
            var rev = new ActivateVisorAttemptEvent(args.Equipee);
            RaiseLocalEvent(newContained, ref rev);

            if (rev.Cancelled)
            {
                DeactivateVisor(ent, newContained, args.Equipee);

                current = null;
                _item.VisualsChanged(ent);
                if (ent.Comp.Action is { } action)
                    _actions.SetIcon(action, ent.Comp.OffIcon);

                _popup.PopupClient(Loc.GetString("rmc-skills-no-training", ("target", newContained)), args.Equipee, args.Equipee, PopupType.SmallCaution);
            }
        }
    }

    private void OnCycleableVisorInteractUsing(Entity<CycleableVisorComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp(args.Used, out VisorComponent? visor))
        {
            if (AttachVisor(ent, (args.Used, visor), args.User))
                args.Handled = true;

            return;
        }

        foreach (var tool in ent.Comp.RemoveQuality)
        {
            if (!_tool.HasQuality(args.Used, tool))
                return;
        }

        args.Handled = true;

        if (ent.Comp.CurrentVisor != null &&
            ent.Comp.Containers.TryGetValue(ent.Comp.CurrentVisor.Value, out var currentId) &&
            _container.TryGetContainer(ent, currentId, out var currentContainer))
        {
            foreach (var contained in currentContainer.ContainedEntities)
            {
                var ev = new DeactivateVisorEvent(ent, args.User);
                RaiseLocalEvent(contained, ref ev);
            }
        }

        var anyRemoved = false;
        foreach (var id in ent.Comp.Containers)
        {
            if (!_container.TryGetContainer(ent, id, out var container))
                continue;

            bool canRemove = true;

            foreach (var contained in container.ContainedEntities)
            {
                if (HasComp<UnremovableVisorComponent>(contained))
                {
                    canRemove = false;
                    continue;
                }
            }

            if (!canRemove)
                continue;

            if (_container.EmptyContainer(container).Count > 0)
                anyRemoved = true;
        }

        if (anyRemoved)
            _popup.PopupClient("You remove the inserted visors", args.Target, args.User);
        else
            _popup.PopupClient("There are no visors left to take out!", args.Target, args.User);

        ent.Comp.CurrentVisor = null;
        Dirty(ent);
    }

    private void OnCycleableVisorScoped(Entity<CycleableVisorComponent> ent, ref InventoryRelayedEvent<ScopedEvent> args)
    {
        var ev = new VisorRelayedEvent<ScopedEvent>(ent, args.Args);
        foreach (var containerId in ent.Comp.Containers)
        {
            if (!_container.TryGetContainer(ent, containerId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                RaiseLocalEvent(contained, ref ev);
            }
        }

        args.Args = ev.Event;
    }

    private void OnVisorAttemptActivate(Entity<VisorComponent> ent, ref ActivateVisorAttemptEvent args)
    {
        if (ent.Comp.SkillsRequired == null || _skills.HasSkills(args.User, ent.Comp.SkillsRequired))
            return;

        args.Cancel();
    }

    private void OnVisorActivate(Entity<VisorComponent> ent, ref ActivateVisorEvent args)
    {
        if (args.CycleableVisor.Comp.Action is { } action)
            _actions.SetIcon(action, ent.Comp.OnIcon);

        if (!HasComp<PowerCellSlotComponent>(ent))
            return;

        _powerCell.SetDrawEnabled(ent.Owner, true);
    }

    private void OnVisorDeactivate(Entity<VisorComponent> ent, ref DeactivateVisorEvent args)
    {
        _powerCell.SetDrawEnabled(ent.Owner, false);
    }

    private void OnCycleableVisorPowerCellChanged(Entity<VisorComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected && _powerCell.HasDrawCharge(ent))
            return;

        // power cell draw is completely broken upstream and does not work AT ALL if
        // the battery and power cell draw component are on different entities
        // all because client and server systems for power cell draw are DIFFERENT
        // despite some of the code being the same and copy pasted
        // and ALL OF THIS being done JUST to avoid networking a SINGLE NUMBER on battery component
        // so we manually sync the booleans that the client needs not to mispredict
        // because having 3 times the code and 2 booleans is easier than networking one float
        // this is fucking dogshit thank you to whichever soldier did this personal shoutout
        if (TryComp(ent, out PowerCellDrawComponent? powerCellDraw))
        {
            var canDraw = !args.Ejected && _powerCell.HasDrawCharge(ent, powerCellDraw);
            var canUse = !args.Ejected && _powerCell.HasDrawCharge(ent, powerCellDraw);

            powerCellDraw.CanDraw = canDraw;
            powerCellDraw.CanUse = canUse;
            Dirty(ent, powerCellDraw);
        }

        if (!_container.TryGetContainingContainer((ent, null), out var visorContainer) ||
            !TryComp(visorContainer.Owner, out CycleableVisorComponent? cycleable))
        {
            return;
        }

        var ev = new DeactivateVisorEvent((visorContainer.Owner, cycleable), null);
        RaiseLocalEvent(ent, ref ev);

        if (cycleable.CurrentVisor is { } current &&
            current >= 0 &&
            cycleable.Containers.TryGetValue(current, out var container) &&
            visorContainer.ID == container)
        {
            cycleable.CurrentVisor = null;
            Dirty(visorContainer.Owner, cycleable);
        }
    }

    private bool AttachVisor(Entity<CycleableVisorComponent> cycleable,
        Entity<VisorComponent> visor,
        EntityUid? user)
    {
        if (!HasComp<ItemComponent>(visor))
            return false;

        string msg;
        foreach (var id in cycleable.Comp.Containers)
        {
            var container = _container.EnsureContainer<ContainerSlot>(cycleable, id);
            if (_container.Insert(visor.Owner, container))
            {
                msg = $"You connect the {Name(visor)} to {Name(cycleable)}.";
                _popup.PopupClient(msg, cycleable, user);
                return true;
            }
        }

        msg = $"{Name(cycleable)} has used all of its visor attachment sockets.";
        _popup.PopupClient(msg, cycleable, user, PopupType.SmallCaution);
        return true;
    }

    public void DeactivateVisor(Entity<CycleableVisorComponent> cycleable, Entity<VisorComponent?> visor, EntityUid user)
    {
        ref var current = ref cycleable.Comp.CurrentVisor;
        if (current == null)
            return;

        if (current < 0 || current >= cycleable.Comp.Containers.Count)
            return;

        var containerId = cycleable.Comp.Containers[current.Value];
        if (!_container.TryGetContainer(cycleable, containerId, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (contained == visor.Owner)
            {
                var ev = new DeactivateVisorEvent(cycleable, user);
                RaiseLocalEvent(contained, ref ev);

                current = null;
                Dirty(cycleable);
                return;
            }
        }
    }

    /// <summary>
    /// Used to skip a visor if the added components are already present
    /// </summary>
    /// <param name="visor"></param>
    /// <param name="args"></param>
    public void OnToggleVisorAttemptActivate(Entity<ToggleVisorComponent> visor, ref ActivateVisorAttemptEvent args)
    {
        if (visor.Comp.IgnoreRedundancy)
            return;

        if (!TryComp<ComponentTogglerComponent>(visor, out var toggle) || !TryComp<VisorComponent>(visor, out var visComp))
            return;

        foreach (var comp in toggle.Components.Values)
        {
            var type = comp.Component.GetType();

            if (HasComp(args.User, type))
                continue;

            if (_inventory.TryGetContainerSlotEnumerator(args.User, out var containerSlotEnumerator))
            {
                bool itemHasComp = false;
                while (containerSlotEnumerator.NextItem(out var item, out var slot))
                {
                    if ((slot.SlotFlags & visComp.Slot) != 0x0)
                        continue;

                    if (HasComp(item, type))
                    {
                        itemHasComp = true;
                        break;
                    }
                }

                if (itemHasComp)
                    continue;
            }

            return;
        }

        args.Cancel();
    }

    public void OnToggleVisorActivate(Entity<ToggleVisorComponent> visor, ref ActivateVisorEvent args)
    {
        if (!TryComp<ComponentTogglerComponent>(visor, out var toggle))
            return;

        args.Handled = true;
        EntityManager.AddComponents(args.CycleableVisor, toggle.Components);
        if (args.User != null)
            _audio.PlayLocal(visor.Comp.SoundOn, visor, args.User);
    }

    public void OnToggleVisorDeactivate(Entity<ToggleVisorComponent> visor, ref DeactivateVisorEvent args)
    {
        if (!TryComp<ComponentTogglerComponent>(visor, out var toggle))
            return;

        EntityManager.RemoveComponents(args.CycleableVisor, toggle.RemoveComponents ?? toggle.Components);
        if (args.User != null)
         _audio.PlayLocal(visor.Comp.SoundOff, visor, args.User);
    }

    private void OnIntegratedVisorsInit(Entity<IntegratedVisorsComponent> integrated, ref MapInitEvent args)
    {
        if (!TryComp<CycleableVisorComponent>(integrated, out var cycleable))
            return;

        var containers = new List<ContainerSlot>();
        foreach (var id in cycleable.Containers)
        {
            containers.Add(_container.EnsureContainer<ContainerSlot>(integrated, id));
        }

        DebugTools.Assert(cycleable.Containers.Count >= integrated.Comp.VisorsToAdd.Count, $"{integrated} does not have enough slots to fit integrated visors!");

        foreach (var proto in integrated.Comp.VisorsToAdd)
        {
            var vis = SpawnAtPosition(proto, integrated.Owner.ToCoordinates());
            if (!TryComp<VisorComponent>(vis, out var visor))
            {
                QueueDel(vis);
                continue;
            }

            if (!AttachVisor((integrated.Owner, cycleable), (vis, visor), null))
            {
                QueueDel(vis);
                continue;
            }
        }
        //TODO Make this work

        ref var current = ref cycleable.CurrentVisor;

        if (integrated.Comp.StartToggled && containers.Count > 0)
        {
            current = current == null ? 0 : current + 1;
            Dirty(integrated.Owner, cycleable);

            if (current >= containers.Count)
                current = null;

            if (current != null &&
                containers.TryGetValue(current.Value, out var currentContainer) &&
                currentContainer.ContainedEntity is { } newContained)
            {
                if (!_powerCell.HasDrawCharge(newContained))
                {
                    current = null;
                    return;
                }

                var ev = new ActivateVisorEvent((integrated.Owner, cycleable), null);
                RaiseLocalEvent(newContained, ref ev);

                if (!ev.Handled)
                    current = null;

            }

            _item.VisualsChanged(integrated.Owner);
        }
    }
}
