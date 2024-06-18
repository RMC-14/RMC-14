using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Scoping;

public sealed partial class SharedScopeSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float SmallestViewpointSize = 15;

    public override void Initialize()
    {
        InitializeUser();
        SubscribeLocalEvent<ScopeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScopeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ScopeComponent, EntityTerminatingEvent>(OnScopeEntityTerminating);
        SubscribeLocalEvent<ScopeComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<ScopeComponent, HandDeselectedEvent>(OnDeselectHand);
        SubscribeLocalEvent<ScopeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ScopeComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ScopeComponent, GunShotEvent>(OnGunShot);
    }

    private void OnMapInit(Entity<ScopeComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<ScopeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.User is not { } user)
            return;

        StopScoping(ent);
        _actionsSystem.RemoveProvidedActions(user, ent.Owner);
    }

    private void OnScopeEntityTerminating(Entity<ScopeComponent> ent, ref EntityTerminatingEvent args)
    {
        StopScoping(ent);
    }

    private void OnUnequip(Entity<ScopeComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopScoping(ent);
    }

    private void OnDeselectHand(Entity<ScopeComponent> ent, ref HandDeselectedEvent args)
    {
        StopScoping(ent);
    }

    private void OnGetActions(Entity<ScopeComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ScopingToggleActionEntity, ent.Comp.ScopingToggleAction);
    }

    private void OnToggleAction(Entity<ScopeComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_hands.TryGetActiveItem(args.Performer, out var heldItem) || heldItem != ent.Owner)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-hold", ("scope", ent.Owner));
            _popup.PopupClient(msgError, args.Performer, args.Performer);
            return;
        }

        if (_pulling.IsPulled(args.Performer))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-pulled", ("scope", ent.Owner));
            _popup.PopupClient(msgError, args.Performer, args.Performer);
            return;
        }

        if (_container.IsEntityInContainer(args.Performer))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-contained", ("scope", ent.Owner));
            _popup.PopupClient(msgError, args.Performer, args.Performer);
            return;
        }

        if (ent.Comp.RequireWielding &&
            TryComp(ent, out WieldableComponent? wieldable) &&
            !wieldable.Wielded)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-wield", ("scope", ent.Owner));
            _popup.PopupClient(msgError, args.Performer, args.Performer);
            return;
        }

        ToggleScoping(ent, args.Performer);
        args.Handled = true;
    }

    private void OnGunShot(Entity<ScopeComponent> ent, ref GunShotEvent args)
    {
        var dir = Transform(args.User).LocalRotation.GetCardinalDir();
        if (ent.Comp.ScopingDirection != dir)
            StopScoping(ent);
    }

    private void StartScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        var xform = Transform(user);
        var cardinalDir = xform.LocalRotation.GetCardinalDir();
        scope.Comp.User = user;
        scope.Comp.ScopingDirection = cardinalDir;

        Dirty(scope);
        var scoping = EnsureComp<ScopingComponent>(user);
        scoping.Scope = scope;

        var targetOffset = cardinalDir.ToVec() * ((SmallestViewpointSize * scope.Comp.Zoom - 1) / 2);
        scoping.EyeOffset = targetOffset;

        var msgUser = Loc.GetString("cm-action-popup-scoping-user", ("scope", Name(scope.Owner)));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, true);
        _contentEye.SetZoom(user, Vector2.One * scope.Comp.Zoom, true);
    }

    private void StopScoping(Entity<ScopeComponent> scope)
    {
        if (scope.Comp.User is not { } user)
            return;

        RemCompDeferred<ScopingComponent>(user);

        scope.Comp.User = null;
        scope.Comp.ScopingDirection = null;
        Dirty(scope);

        var msgUser = Loc.GetString("cm-action-popup-scoping-stopping-user", ("scope", Name(scope.Owner)));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, false);
        _contentEye.ResetZoom(user);
        _contentEye.SetZoom(user, Vector2.One * scope.Comp.Zoom, true);
    }

    private void ToggleScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (HasComp<ScopingComponent>(user))
            StopScoping(scope);
        else
            StartScoping(scope, user);
    }
}
