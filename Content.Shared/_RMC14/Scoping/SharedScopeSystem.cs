using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Scoping;

public abstract partial class SharedScopeSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

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
        SubscribeLocalEvent<ScopeComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<ScopeComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ScopeComponent, ScopeDoAfterEvent>(OnScopeDoAfter);

        SubscribeLocalEvent<GunScopingComponent, GotUnequippedHandEvent>(OnGunUnequip);
        SubscribeLocalEvent<GunScopingComponent, HandDeselectedEvent>(OnGunDeselectHand);
        SubscribeLocalEvent<GunScopingComponent, ItemUnwieldedEvent>(OnGunUnwielded);
        SubscribeLocalEvent<GunScopingComponent, GunShotEvent>(OnGunGunShot);
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

    private void OnActivateInWorld(Entity<ScopeComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || !ent.Comp.UseInHand)
            return;

        args.Handled = true;
        ToggleScoping(ent, args.User);
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

    private void OnGunUnequip(Entity<GunScopingComponent> ent, ref GotUnequippedHandEvent args)
    {
        UnscopeGun(ent);
    }

    private void OnGunDeselectHand(Entity<GunScopingComponent> ent, ref HandDeselectedEvent args)
    {
        UnscopeGun(ent);
    }

    private void OnGunUnwielded(Entity<GunScopingComponent> ent, ref ItemUnwieldedEvent args)
    {
        UnscopeGun(ent);
    }

    private void OnGunGunShot(Entity<GunScopingComponent> ent, ref GunShotEvent args)
    {
        var dir = Transform(args.User).LocalRotation.GetCardinalDir();
        if (TryComp(ent.Comp.Scope, out ScopeComponent? scope) && scope.ScopingDirection != dir)
            UnscopeGun(ent);
    }

    private bool CanScopePopup(Entity<ScopeComponent> scope, EntityUid user)
    {
        var ent = scope.Owner;
        if (scope.Comp.Attachment && !TryGetActiveEntity(scope, out ent))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-must-attach", ("scope", ent));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (!_hands.TryGetActiveItem(user, out var heldItem) || !scope.Comp.Attachment && heldItem != scope.Owner)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-hold", ("scope", ent));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (_pulling.IsPulled(user))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-pulled", ("scope", ent));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (_container.IsEntityInContainer(user))
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-not-contained", ("scope", ent));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        if (scope.Comp.RequireWielding &&
            TryComp(ent, out WieldableComponent? wieldable) &&
            !wieldable.Wielded)
        {
            var msgError = Loc.GetString("cm-action-popup-scoping-user-must-wield", ("scope", ent));
            _popup.PopupClient(msgError, user, user);
            return false;
        }

        return true;
    }

    protected virtual Direction? StartScoping(Entity<ScopeComponent> scope, EntityUid user)
    {
        if (!CanScopePopup(scope, user))
            return null;

        // TODO RMC14 make this work properly with rotations
        var xform = Transform(user);
        var cardinalDir = xform.LocalRotation.GetCardinalDir();
        var ev = new ScopeDoAfterEvent(cardinalDir);
        var doAfter = new DoAfterArgs(EntityManager, user, scope.Comp.Delay, ev, scope, null, scope)
        {
            BreakOnMove = !scope.Comp.AllowMovement
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
        scoping.AllowMovement = scope.Comp.AllowMovement;
        Dirty(user, scoping);

        if (scope.Comp.Attachment && TryGetActiveEntity(scope, out var active))
        {
            var gunScoping = EnsureComp<GunScopingComponent>(active);
            gunScoping.Scope = scope;
            Dirty(active, gunScoping);
        }

        var targetOffset = GetScopeOffset(scope, direction);
        scoping.EyeOffset = targetOffset;

        var msgUser = Loc.GetString("cm-action-popup-scoping-user", ("scope", scope.Owner));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, true);
        _contentEye.SetZoom(user, Vector2.One * scope.Comp.Zoom, true);
        UpdateOffset(user);
    }

    protected virtual bool Unscope(Entity<ScopeComponent> scope)
    {
        if (scope.Comp.User is not { } user)
            return false;

        RemCompDeferred<ScopingComponent>(user);

        if (scope.Comp.Attachment && TryGetActiveEntity(scope, out var active))
            RemCompDeferred<GunScopingComponent>(active);

        scope.Comp.User = null;
        scope.Comp.ScopingDirection = null;
        Dirty(scope);

        var msgUser = Loc.GetString("cm-action-popup-scoping-stopping-user", ("scope", scope.Owner));
        _popup.PopupClient(msgUser, user, user);

        _actionsSystem.SetToggled(scope.Comp.ScopingToggleActionEntity, false);
        _contentEye.ResetZoom(user);
        return true;
    }

    private void UnscopeGun(Entity<GunScopingComponent> gun)
    {
        if (TryComp(gun.Comp.Scope, out ScopeComponent? scope))
            Unscope((gun.Comp.Scope.Value, scope));
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

    private bool TryGetActiveEntity(Entity<ScopeComponent> scope, out EntityUid active)
    {
        if (!scope.Comp.Attachment)
        {
            active = scope;
            return true;
        }

        if (!_container.TryGetContainingContainer((scope, null), out var container) ||
            !HasComp<GunComponent>(container.Owner))
        {
            active = default;
            return false;
        }

        active = container.Owner;
        return true;
    }

    protected Vector2 GetScopeOffset(Entity<ScopeComponent> scope, Direction direction)
    {
        return direction.ToVec() * ((scope.Comp.Offset * scope.Comp.Zoom - 1) / 2);
    }

    protected virtual void DeleteRelay(Entity<ScopeComponent> scope, EntityUid? user)
    {
    }

    private void UpdateOffset(EntityUid user)
    {
        var ev = new GetEyeOffsetEvent();
        RaiseLocalEvent(user, ref ev);
        _eye.SetOffset(user, ev.Offset);
    }
}
