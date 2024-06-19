using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Scoping;

public abstract partial class SharedScopeSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    // TODO CM14 scoping making this too high causes pop-in
    // wait until https://github.com/space-wizards/RobustToolbox/pull/5228 is fixed to increase it
    // cm13 values: 11 tile offset, 24x24 view in 4x | 6 tile offset, normal view in 2x.
    // right now we are doing a mix of both and only one setting.
    private const float SmallestViewpointSize = 15;

    public override void Initialize()
    {
        InitializeUser();

        SubscribeLocalEvent<ScopeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScopeComponent, ComponentRemove>(OnShutdown);
        SubscribeLocalEvent<ScopeComponent, EntityTerminatingEvent>(OnScopeEntityTerminating);
        SubscribeLocalEvent<ScopeComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<ScopeComponent, HandDeselectedEvent>(OnDeselectHand);
        SubscribeLocalEvent<ScopeComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<ScopeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ScopeComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ScopeComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ScopeComponent, ScopeDoAfterEvent>(OnScopeDoAfter);
    }

    private void OnMapInit(Entity<ScopeComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<ScopeComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.User is not { } user)
            return;

        Unscope(ent);
        _actionsSystem.RemoveProvidedActions(user, ent.Owner);
    }

    private void OnScopeEntityTerminating(Entity<ScopeComponent> ent, ref EntityTerminatingEvent args)
    {
        Unscope(ent);
    }

    private void OnUnequip(Entity<ScopeComponent> ent, ref GotUnequippedHandEvent args)
    {
        Unscope(ent);
    }

    private void OnDeselectHand(Entity<ScopeComponent> ent, ref HandDeselectedEvent args)
    {
        Unscope(ent);
    }

    private void OnUnwielded(Entity<ScopeComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (ent.Comp.RequireWielding)
            Unscope(ent);
    }

    private void OnGetActions(Entity<ScopeComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
    }

    private void OnToggleAction(Entity<ScopeComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleScoping(ent, args.Performer);
    }

    private void OnGunShot(Entity<ScopeComponent> ent, ref GunShotEvent args)
    {
        var dir = Transform(args.User).LocalRotation.GetCardinalDir();
        if (ent.Comp.ScopingDirection != dir)
            Unscope(ent);
    }

    private void OnScopeDoAfter(Entity<ScopeComponent> ent, ref ScopeDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            DeleteRelay(ent, args.User);
            return;
        }

        var user = args.User;
        if (!CanScopePopup(ent, user))
        {
            DeleteRelay(ent, args.User);
            return;
        }

        Scope(ent, user, args.Direction);
    }

    private bool CanScopePopup(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (!_hands.TryGetActiveItem(user, out var heldItem) || heldItem != scope)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-hold", ("scope", scope.Owner));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (_pulling.IsPulled(user))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-pulled", ("scope", scope.Owner));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (_container.IsEntityInContainer(user))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-contained", ("scope", scope.Owner));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (scope.Comp.RequireWielding &&
            TryComp(scope, out WieldableComponent? wieldable) &&
            !wieldable.Wielded)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-wield", ("scope", scope.Owner));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        return true;
    }

    protected virtual Direction? StartScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (!CanScopePopup(scope, user))
            return null;

        var xform = Transform(user);
        var cardinalDir = xform.LocalRotation.GetCardinalDir();
        var ev = new ScopeDoAfterEvent(cardinalDir);
        var doAfter = new DoAfterArgs(EntityManager, user, scope.Comp.Delay, ev, scope, null, scope)
        {
            BreakOnMove = true
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            return cardinalDir;

        return null;
    }

    private void Scope(Entity<ScopeComponent> scope, EntityUid user, Direction direction)
    {
        if (TryComp(user, out ScopingComponent? scoping))
            UserStopScoping((user, scoping));

        scope.Comp.User = user;
        scope.Comp.ScopingDirection = direction;

        Dirty(scope);
        scoping = EnsureComp<ScopingComponent>(user);
        scoping.Scope = scope;

        var targetOffset = GetScopeOffset(direction, scope.Comp.Zoom);
        scoping.EyeOffset = targetOffset;

        var msgUser = Loc.GetString("cm-action-popup-scoping-user", ("scope", Name(scope.Owner)));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, true);
        _contentEye.SetZoom(user, Vector2.One * scope.Comp.Zoom, true);
    }

    protected virtual bool Unscope(Entity<ScopeComponent> scope)
    {
        if (scope.Comp.User is not { } user)
            return false;

        RemCompDeferred<ScopingComponent>(user);

        scope.Comp.User = null;
        scope.Comp.ScopingDirection = null;
        Dirty(scope);

        var msgUser = Loc.GetString("cm-action-popup-scoping-stopping-user", ("scope", Name(scope.Owner)));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, false);
        _contentEye.ResetZoom(user);
        return true;
    }

    private void ToggleScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (HasComp<ScopingComponent>(user))
        {
            Unscope(scope);

            if (TryComp(user, out ScopingComponent? scoping))
                UserStopScoping((user, scoping));

            return;
        }

        StartScoping(scope, user);
    }

    protected Vector2 GetScopeOffset(Direction direction, float zoom)
    {
        return direction.ToVec() * ((SmallestViewpointSize * zoom - 1) / 2);
    }

    protected virtual void DeleteRelay(Entity<ScopeComponent> scope, EntityUid? user)
    {
    }
}
