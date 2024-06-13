using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Scoping;

public abstract partial class SharedScopeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
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

    private void OnMapInit(EntityUid uid, ScopeComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ScopingToggleActionEntity, component.ScopingToggleAction);
        Dirty(uid, component);
    }

    private void OnEquip(EntityUid uid, ScopeComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);

        if(_net.IsServer)
            EnsureComp<ScopeUserComponent>(args.User);
    }

    private void OnUnequip(EntityUid uid, ScopeComponent component, GotUnequippedHandEvent args)
    {
        StopScopingHelper(uid, component, args.User);
    }

    private void OnDeselectHand(EntityUid uid, ScopeComponent component, HandDeselectedEvent args)
    {
        if (args.Handled)
            return;

        if (component.IsScoping)
            StopScoping(uid, component, args.User);
    }

    private void OnGetActions(EntityUid uid, ScopeComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ScopingToggleActionEntity, component.ScopingToggleAction);
    }

    private void OnToggleAction(EntityUid uid, ScopeComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_handsSystem.TryGetActiveItem(args.Performer, out var heldItem) || heldItem != uid)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-hold", ("scope", Name(uid)));
            if (_gameTiming.IsFirstTimePredicted && _gameTiming.InPrediction)
            {
                _popupSystem.PopupClient(msgError, args.Performer, args.Performer);
            }

            return;
        }

        if (component.IsScoping)
            StopScoping(uid, component, args.Performer);
        else
            StartScoping(uid, component, args.Performer);

        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, ScopeComponent component, ComponentShutdown args)
    {
        if (component.User != null)
        {
            _actionsSystem.RemoveProvidedActions(component.User.Value, uid);
            StopScopingHelper(uid, component, component.User.Value);
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

        _actionsSystem.SetToggled(component.ScopingToggleActionEntity, true);
        if (_gameTiming.IsFirstTimePredicted && _gameTiming.InPrediction)
        {
            _popupSystem.PopupClient(msgUser, user, user);
        }

        StartScopingCamera(user, component);

        component.IsScoping = true;
        component.LastScopedAt = Transform(user).Coordinates;
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

        _actionsSystem.SetToggled(component.ScopingToggleActionEntity, false);

        if (_gameTiming.IsFirstTimePredicted && _gameTiming.InPrediction)
        {
            _popupSystem.PopupClient(msgUser, user, user);
        }

        StopScopingCamera(user, component);

        component.IsScoping = false;
        Dirty(item, component);

        return true;
    }

    private void StopScopingHelper(EntityUid uid, ScopeComponent component, EntityUid user)
    {
        if (component.IsScoping)
            StopScoping(uid, component, user);

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
