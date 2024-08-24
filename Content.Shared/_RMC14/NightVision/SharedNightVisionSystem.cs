using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Inventory.Events;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.NightVision;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnNightVisionStartup);
        SubscribeLocalEvent<NightVisionComponent, MapInitEvent>(OnNightVisionMapInit);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnNightVisionAfterHandle);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnNightVisionRemove);
        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionAlertEvent>(OnNightVisionToggle);

        SubscribeLocalEvent<NightVisionItemComponent, GetItemActionsEvent>(OnNightVisionItemGetActions);
        SubscribeLocalEvent<NightVisionItemComponent, ToggleActionEvent>(OnNightVisionItemToggle);
        SubscribeLocalEvent<NightVisionItemComponent, GotEquippedEvent>(OnNightVisionItemGotEquipped);
        SubscribeLocalEvent<NightVisionItemComponent, GotUnequippedEvent>(OnNightVisionItemGotUnequipped);
        SubscribeLocalEvent<NightVisionItemComponent, ActionRemovedEvent>(OnNightVisionItemActionRemoved);
        SubscribeLocalEvent<NightVisionItemComponent, ComponentRemove>(OnNightVisionItemRemove);
        SubscribeLocalEvent<NightVisionItemComponent, EntityTerminatingEvent>(OnNightVisionItemTerminating);
    }

    private void OnNightVisionStartup(Entity<NightVisionComponent> ent, ref ComponentStartup args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionAfterHandle(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionMapInit(Entity<NightVisionComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnNightVisionRemove(Entity<NightVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Alert is { } alert)
            _alerts.ClearAlert(ent, alert);

        NightVisionRemoved(ent);
    }

    private void OnNightVisionToggle(Entity<NightVisionComponent> ent, ref ToggleNightVisionAlertEvent args)
    {
        Toggle((ent, ent));
    }

    private void OnNightVisionItemGetActions(Entity<NightVisionItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands || !ent.Comp.Toggleable)
            return;

        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnNightVisionItemToggle(Entity<NightVisionItemComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleNightVisionItem(ent, args.Performer);
    }

    private void OnNightVisionItemGotEquipped(Entity<NightVisionItemComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        EnableNightVisionItem(ent, args.Equipee);
    }

    private void OnNightVisionItemGotUnequipped(Entity<NightVisionItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        DisableNightVisionItem(ent, args.Equipee);
    }

    private void OnNightVisionItemActionRemoved(Entity<NightVisionItemComponent> ent, ref ActionRemovedEvent args)
    {
        DisableNightVisionItem(ent, ent.Comp.User);
    }

    private void OnNightVisionItemRemove(Entity<NightVisionItemComponent> ent, ref ComponentRemove args)
    {
        DisableNightVisionItem(ent, ent.Comp.User);
    }

    private void OnNightVisionItemTerminating(Entity<NightVisionItemComponent> ent, ref EntityTerminatingEvent args)
    {
        DisableNightVisionItem(ent, ent.Comp.User);
    }

    public void Toggle(Entity<NightVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            NightVisionState.Off => NightVisionState.Half,
            NightVisionState.Half => NightVisionState.Full,
            NightVisionState.Full => NightVisionState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAlert((ent, ent.Comp));
    }

    private void UpdateAlert(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Alert is { } alert)
        {
            var level = MathF.Max((int) NightVisionState.Off, (int) ent.Comp.State);
            var max = _alerts.GetMaxSeverity(alert);
            var severity = max - ContentHelpers.RoundToLevels(level, (int) NightVisionState.Full, max + 1);
            _alerts.ShowAlert(ent, alert, (short) severity);
        }

        NightVisionChanged(ent);
    }

    private void ToggleNightVisionItem(Entity<NightVisionItemComponent> item, EntityUid user)
    {
        if (item.Comp.User == user && item.Comp.Toggleable)
        {
            DisableNightVisionItem(item, item.Comp.User);
            return;
        }

        EnableNightVisionItem(item, user);
    }

    private void EnableNightVisionItem(Entity<NightVisionItemComponent> item, EntityUid user)
    {
        DisableNightVisionItem(item, item.Comp.User);

        item.Comp.User = user;
        Dirty(item);

        _appearance.SetData(item, NightVisionItemVisuals.Active, true);

        if (!_timing.ApplyingState)
        {
            var nightVision = EnsureComp<NightVisionComponent>(user);
            nightVision.State = NightVisionState.Full;
            Dirty(user, nightVision);
        }

        _actions.SetToggled(item.Comp.Action, true);
    }

    protected virtual void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
    }

    protected virtual void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
    }

    protected void DisableNightVisionItem(Entity<NightVisionItemComponent> item, EntityUid? user)
    {
        _actions.SetToggled(item.Comp.Action, false);

        item.Comp.User = null;
        Dirty(item);

        _appearance.SetData(item, NightVisionItemVisuals.Active, false);

        if (TryComp(user, out NightVisionComponent? nightVision) &&
            !nightVision.Innate)
        {
            RemCompDeferred<NightVisionComponent>(user.Value);
        }
    }

    public void SetSeeThroughContainers(Entity<NightVisionComponent?> ent, bool see)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.SeeThroughContainers = see;
        Dirty(ent);
    }
}
