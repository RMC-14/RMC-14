using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Placement;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Item.Deploy;

public sealed partial class DeployableEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCPlacementSystem _placement = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

        if (_placement.TryFindBlocker(coordinates, deployable.Comp.PlacementRestrictions, out var blockingEntity, deployable))
        {
            var msg = Loc.GetString("emplacement-mount-too-close", ("mount", blockingEntity));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution );
            return false;
        }

        if (HasComp<VehicleInteriorOccupantComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("emplacement-mount-deploy-vehicle"), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
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
