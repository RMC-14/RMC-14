using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Network;

namespace Content.Shared._CM14.Scoping;

public abstract partial class SharedScopeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        InitializeUser();
        SubscribeLocalEvent<ScopeComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<ScopeComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<ScopeComponent, HandDeselectedEvent>(OnDeselectHand);

        SubscribeLocalEvent<ScopeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ScopeComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<ScopeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScopeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ScopeComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnEquip(Entity<ScopeComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.User = args.User;
        Dirty(ent.Owner, ent.Comp);

        if(_net.IsServer)
            EnsureComp<ScopeUserComponent>(args.User);
    }

    private void OnUnequip(Entity<ScopeComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopScopingHelper(ent.Owner, ent.Comp, args.User);
    }

    private void OnDeselectHand(Entity<ScopeComponent> ent, ref HandDeselectedEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.IsScoping)
            StopScoping(ent.Owner, ent.Comp, args.User);
    }

    private void OnGetActions(Entity<ScopeComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
    }

    private void OnToggleAction(Entity<ScopeComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_handsSystem.TryGetActiveItem(args.Performer, out var heldItem) || heldItem != ent.Owner)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-hold", ("scope", Name(ent.Owner)));
            _popupSystem.PopupClient(msgError, args.Performer, args.Performer);

            return;
        }

        if (ent.Comp.IsScoping)
            StopScoping(ent.Owner, ent.Comp, args.Performer);
        else
            StartScoping(ent.Owner, ent.Comp, args.Performer);

        args.Handled = true;
    }

    private void OnShutdown(Entity<ScopeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.User != null)
        {
            _actionsSystem.RemoveProvidedActions(ent.Comp.User.Value, ent.Owner);
            StopScopingHelper(ent.Owner, ent.Comp, ent.Comp.User.Value);
        }
    }

    public bool StartScoping(EntityUid item, ScopeComponent component, EntityUid user)
    {
        if (component.IsScoping)
            return false;

        if (TryComp(user, out ScopeUserComponent? scopeUserComp))
        {
            scopeUserComp.ScopingItem = item;
            Dirty(user, scopeUserComp);
        }

        var msgUser = Loc.GetString("cm-action-popup-scoping-user", ("scope", Name(item)));
        _popupSystem.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(component.ScopingToggleActionEntity, true);

        StartScopingCamera(user, component);

        component.IsScoping = true;
        Dirty(item, component);

        return true;
    }

    public bool StopScoping(EntityUid item, ScopeComponent component, EntityUid user)
    {
        if (!component.IsScoping)
            return false;

        if (TryComp(user, out ScopeUserComponent? scopeUserComp))
        {
            scopeUserComp.ScopingItem = null;
            Dirty(user, scopeUserComp);
        }

        var msgUser = Loc.GetString("cm-action-popup-scoping-stopping-user", ("scope", Name(item)));
        _popupSystem.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(component.ScopingToggleActionEntity, false);

        StopScopingCamera(user, component);

        component.IsScoping = false;
        Dirty(item, component);

        return true;
    }

    private void StopScopingHelper(EntityUid item, ScopeComponent component, EntityUid user)
    {
        if (component.IsScoping)
            StopScoping(item, component, user);

        if (!TryComp(user, out HandsComponent? hands))
            return;

        foreach (var heldItem in _handsSystem.EnumerateHeld(user, hands))
        {
            if (HasComp<ScopeComponent>(heldItem))
            {
                return;
            }
        }

        RemComp<ScopeUserComponent>(user);
        component.User = null;
    }

    protected abstract void StartScopingCamera(EntityUid user, ScopeComponent scopeComponent);
    protected abstract void StopScopingCamera(EntityUid user, ScopeComponent scopeComponent);
}
