using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item.Deploy;

public sealed partial class DeployableEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDeployableEquipmentComponent, UseInHandEvent>(OnDeployableUseInHand);
        SubscribeLocalEvent<RMCDeployableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCDeployableEquipmentComponent, PickupAttemptEvent>(OnDeployablePickupAttempt);
        SubscribeLocalEvent<RMCDeployableEquipmentComponent, EquipmentDeployDoAfterEvent>(OnEquipmentDeployDoAfter);
        SubscribeLocalEvent<RMCDeployableEquipmentComponent, EquipmentUnDeployDoAfterEvent>(OnEquipmentUnDeployDoAfter);
    }

    private void OnDeployableUseInHand(Entity<RMCDeployableEquipmentComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (!CanDeployPopup(ent, args.User, out _, out _))
            return;

        var ev = new EquipmentDeployDoAfterEvent();
        var delay = ent.Comp.DeployDelay;
        if (ent.Comp.DelaySkill is { } delaySkill)
            delay *= _skills.GetSkillDelayMultiplier(args.User, delaySkill);

        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, ent)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnInteractUsing(Entity<RMCDeployableEquipmentComponent> ent, ref InteractUsingEvent args)
    {
        if (!_tool.HasQuality(args.Used, ent.Comp.UndeployQuality))
            return;

        if (ent.Comp.DeployedState == DeployedState.Undeployed)
            return;

        var user = args.User;
        var ev = new EquipmentUnDeployDoAfterEvent();
        var delay = ent.Comp.UndeployDelay;
        if (ent.Comp.DelaySkill is { } delaySkill)
            delay *= _skills.GetSkillDelayMultiplier(user, delaySkill);

        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-sentry-disassemble-start-self", ("sentry", ent));
            var othersMsg = Loc.GetString("rmc-sentry-disassemble-start-others", ("user", user), ("sentry", ent));
            _popup.PopupPredicted(selfMsg, othersMsg, ent, user);
        }
    }

    private void OnDeployablePickupAttempt(Entity<RMCDeployableEquipmentComponent> sentry, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (sentry.Comp.DeployedState != DeployedState.Undeployed)
            args.Cancel();
    }

    private void OnEquipmentDeployDoAfter(Entity<RMCDeployableEquipmentComponent> ent, ref EquipmentDeployDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanDeployPopup(ent, args.User, out var coordinates, out var angle))
            return;

        var xform = Transform(ent);
        _transform.SetCoordinates(ent, xform, coordinates, angle);
        if (ent.Comp.AnchorOnDeploy)
            _transform.AnchorEntity(ent, xform);

        ent.Comp.DeployedState = DeployedState.Deployed;
        Dirty(ent);

        var ev = new EquipmentDeployedEvent(args.User, angle);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnEquipmentUnDeployDoAfter(Entity<RMCDeployableEquipmentComponent> ent, ref EquipmentUnDeployDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        if (ent.Comp.DeployedState == DeployedState.Undeployed)
            return;

        _transform.Unanchor(ent.Owner, Transform(ent));

        ent.Comp.DeployedState = DeployedState.Undeployed;
        Dirty(ent);

        var ev = new EquipmentUnDeployedEvent();
        RaiseLocalEvent(ent, ref ev);

        var selfMsg = Loc.GetString("rmc-sentry-disassemble-finish-self", ("sentry", ent));
        var othersMsg = Loc.GetString("rmc-sentry-disassemble-finish-others", ("user", user), ("sentry", ent));
        _popup.PopupPredicted(selfMsg, othersMsg, ent, user);
    }

    private bool CanDeployPopup(Entity<RMCDeployableEquipmentComponent> deployable, EntityUid user, out EntityCoordinates coordinates, out Angle rotation)
    {
        coordinates = default;
        rotation = default;

        var moverCoordinates = _transform.GetMoverCoordinateRotation(user, Transform(user));
        coordinates = moverCoordinates.Coords;
        rotation = moverCoordinates.worldRot.GetCardinalDir().ToAngle();

        var direction = rotation.GetCardinalDir();
        coordinates = coordinates.Offset(direction.ToVec() * deployable.Comp.DeployDistance);
        if (!_rmcMap.CanBuildOn(coordinates))
        {
            var msg = Loc.GetString("rmc-sentry-need-open-area", ("sentry", deployable));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        if (deployable.Comp.PlaceableBlacklist is { } blacklist && TryFindAnchoredEntityNearby(coordinates, blacklist, out var blockingEntity, deployable.Comp.PlaceableCheckRange))
        {
            var msg = Loc.GetString("emplacement-mount-too-close", ("mount", blockingEntity));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution );
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks for anchored entities matching a whitelist within a square area around the given coordinates.
    /// </summary>
    /// <param name="coordinates">The center coordinates to search around.</param>
    /// <param name="whitelist">Whitelist used to determine which entities count as a match.</param>
    /// <param name="foundEntity">The first matching anchored entity found in the search area.</param>
    /// <param name="range"> The tile range around the coordinates to search. A range of 1 only checks the given coordinates, a range of 2 a 3x3 area, etc.</param>
    /// <returns>True if a matching anchored entity is found within the specified range.</returns>
    public bool TryFindAnchoredEntityNearby(EntityCoordinates coordinates, EntityWhitelist whitelist, [NotNullWhen(true)] out EntityUid? foundEntity, float range = 1)
    {
        foundEntity = null;
        if (range == 0)
            return false;

        var grid = _transform.GetGrid(coordinates);
        if (!TryComp(grid, out MapGridComponent? mapGrid))
            return false;

        var position = _mapSystem.LocalToTile(grid.Value, mapGrid, coordinates);
        var checkArea = new Box2(position.X - range + 1, position.Y - range + 1, position.X + range, position.Y + range);
        var enumerable = _mapSystem.GetLocalAnchoredEntities(grid.Value, mapGrid, checkArea);

        foreach (var anchored in enumerable)
        {
            if (!_whitelist.IsValid(whitelist, anchored))
                continue;

            foundEntity = anchored;
            return true;
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed partial class EquipmentDeployDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EquipmentUnDeployDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public record struct EquipmentDeployedEvent(EntityUid User, Angle Direction, bool Handled = false);

[ByRefEvent]
public record struct EquipmentUnDeployedEvent(bool Handled = false);
