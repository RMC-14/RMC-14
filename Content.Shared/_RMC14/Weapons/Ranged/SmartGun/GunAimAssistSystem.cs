using System.Numerics;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Burrow;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

public sealed class GunAimAssistSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly RMCGunBatterySystem _gunBattery = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCProjectileSystem _projectile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _factions = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GunAimAssistComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GunAimAssistComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunAimAssistComponent, GunToggleAimAssistActionEvent>(OnToggleAimAssist);
        SubscribeLocalEvent<GunAimAssistComponent, GunGetBatteryDrainEvent>(OnGetBatteryDrain);
        SubscribeLocalEvent<GunAimAssistComponent, AttemptShootEvent>(OnAttemptShoot,
            after: [typeof(CMGunSystem), typeof(RMCGunBatterySystem)]);
        SubscribeLocalEvent<GunAimAssistComponent, AmmoShotEvent>(OnAmmoShot, after: [typeof(CMGunSystem)]);
        SubscribeLocalEvent<GunAimAssistComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunAimAssistComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<GunAimAssistComponent, DroppedEvent>(OnDropped);
    }

    private void OnMapInit(Entity<GunAimAssistComponent> ent, ref MapInitEvent args)
    {
        _gunBattery.RefreshBatteryDrain(ent.Owner);
    }

    private void OnGetItemActions(Entity<GunAimAssistComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        _actions.SetToggled(ent.Comp.Action, ent.Comp.Active);
        Dirty(ent);
    }

    private void OnToggleAimAssist(Entity<GunAimAssistComponent> ent, ref GunToggleAimAssistActionEvent args)
    {
        if (args.Handled || !_hands.IsHolding(args.Performer, ent.Owner))
            return;

        ent.Comp.Active = !ent.Comp.Active;
        ClearTarget(ent);
        Dirty(ent);
        _actions.SetToggled(ent.Comp.Action, ent.Comp.Active);
        _gunBattery.RefreshBatteryDrain(ent.Owner);
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent.Owner, args.Performer);

        var message = ent.Comp.Active ? "rmc-aim-assist-enabled" : "rmc-aim-assist-disabled";
        _popup.PopupClient(Loc.GetString(message), args.Performer, args.Performer, PopupType.Medium);
        args.Handled = true;
    }

    private void OnGetBatteryDrain(Entity<GunAimAssistComponent> ent, ref GunGetBatteryDrainEvent args)
    {
        if (ent.Comp.Active)
            args.Drain += ent.Comp.BatteryDrain;
    }

    private void OnAttemptShoot(Entity<GunAimAssistComponent> ent, ref AttemptShootEvent args)
    {
        ClearTarget(ent);
        if (args.Cancelled || !ent.Comp.Active || args.ToCoordinates is not { } coordinates ||
            !_hands.IsHolding(args.User, ent.Owner) || !TryComp(ent, out GunComponent? gun))
        {
            return;
        }

        if (gun.Target is { } clicked && !TerminatingOrDeleted(clicked) &&
            !EntityManager.IsQueuedForDeletion(clicked) && TryComp(clicked, out TransformComponent? clickedTransform))
        {
            coordinates = clickedTransform.Coordinates;
        }

        if (GetAssistTarget(ent, args.User, coordinates) is not { } target)
            return;

        ent.Comp.Target = target;
        ent.Comp.Retargeted = target != gun.Target;
        args.ToCoordinates = Transform(target).Coordinates;
    }

    private void OnAmmoShot(Entity<GunAimAssistComponent> ent, ref AmmoShotEvent args)
    {
        if (ent.Comp.Target == null || args.FiredProjectiles.Count == 0)
            return;

        if (ent.Comp.Retargeted)
        {
            foreach (var projectile in args.FiredProjectiles)
            {
                if (TryComp(projectile, out RMCProjectileAccuracyComponent? accuracy))
                {
                    var penalized = MathF.Floor((accuracy.Accuracy * (1 - ent.Comp.AccuracyPenalty)).Float());
                    _projectile.SetProjectileAccuracy(projectile, penalized);
                }
            }
        }

        ent.Comp.Retargeted = false;
    }

    private void OnGunShot(Entity<GunAimAssistComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.Target == null || args.Ammo.Count == 0)
            return;

        ent.Comp.TargetExpires = _timing.CurTime + ent.Comp.TargetDuration;
        Dirty(ent);
    }

    private void OnUnwielded(Entity<GunAimAssistComponent> ent, ref ItemUnwieldedEvent args)
    {
        ClearTarget(ent);
    }

    private void OnDropped(Entity<GunAimAssistComponent> ent, ref DroppedEvent args)
    {
        ClearTarget(ent);
    }

    private void ClearTarget(Entity<GunAimAssistComponent> ent)
    {
        ent.Comp.Retargeted = false;
        if (ent.Comp.Target == null)
            return;

        ent.Comp.Target = null;
        Dirty(ent);
    }

    private EntityUid? GetAssistTarget(Entity<GunAimAssistComponent> ent, EntityUid user, EntityCoordinates coordinates)
    {
        if (HasComp<RMCBlindedComponent>(user) ||
            TryComp(user, out BlindableComponent? blindable) && blindable.IsBlind)
        {
            return null;
        }

        var origin = _transform.GetMapCoordinates(user);
        var cursor = _transform.ToMapCoordinates(coordinates);
        if (origin.MapId != cursor.MapId)
            return null;

        var grid = _transform.GetGrid(coordinates);
        var rotation = grid == null ? Angle.Zero : _transform.GetWorldRotation(grid.Value);
        var bounds = new Box2Rotated(Box2.CenteredAround(cursor.Position, new Vector2(ent.Comp.LockRange * 2)),
            rotation, cursor.Position);
        _targets.Clear();
        _lookup.GetEntitiesIntersecting(cursor.MapId, bounds, _targets, LookupFlags.Uncontained);
        _iff.TryGetFactions(user, _factions);
        var iffEnabled = !TryComp(ent, out GunIFFComponent? iff) || iff.Enabled;
        var visibility = TryComp(user, out EyeComponent? eye) ? eye.VisibilityMask : EyeComponent.DefaultVisibilityMask;
        EntityUid? conscious = null;
        EntityUid? unconscious = null;
        var consciousDistance = float.MaxValue;
        var unconsciousDistance = float.MaxValue;

        foreach (var target in _targets)
        {
            if (target.Owner == user || TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target) ||
                _mobState.IsDead(target) || _container.IsEntityInContainer(target) ||
                HasComp<VentCrawlingComponent>(target) ||
                HasComp<EntityActiveInvisibleComponent>(target) ||
                TryComp(target, out XenoBurrowComponent? burrow) && burrow.Active ||
                (MetaData(target).VisibilityMask & visibility) != MetaData(target).VisibilityMask)
            {
                continue;
            }

            if (iffEnabled && IsFriendly(target.Owner))
                continue;

            var position = _transform.GetMapCoordinates(target);
            var offset = (-rotation).RotateVec(position.Position - cursor.Position);
            if (position.MapId != cursor.MapId || MathF.Abs(offset.X) > ent.Comp.LockRange ||
                MathF.Abs(offset.Y) > ent.Comp.LockRange)
            {
                continue;
            }

            if (!_examine.InRangeUnOccluded(origin, position, 0, (user, target.Owner),
                    static (uid, ignored) => uid == ignored.Item1 || uid == ignored.Item2, ignoreInsideBlocker: false))
            {
                continue;
            }

            var distance = offset.LengthSquared();
            if (_mobState.IsCritical(target) || HasComp<RMCUnconsciousComponent>(target) ||
                HasComp<SleepingComponent>(target))
            {
                if (IsCloser(target.Owner, distance, unconscious, unconsciousDistance))
                {
                    unconscious = target.Owner;
                    unconsciousDistance = distance;
                }
            }
            else if (IsCloser(target.Owner, distance, conscious, consciousDistance))
            {
                conscious = target.Owner;
                consciousDistance = distance;
            }
        }

        return conscious ?? unconscious;
    }

    private bool IsFriendly(EntityUid target)
    {
        foreach (var faction in _factions)
        {
            if (_iff.IsInFaction(target, faction))
                return true;
        }

        return false;
    }

    private bool IsCloser(EntityUid target, float distance, EntityUid? closest, float closestDistance)
    {
        return distance < closestDistance || distance == closestDistance &&
            (closest == null || GetNetEntity(target).Id < GetNetEntity(closest.Value).Id);
    }
}
