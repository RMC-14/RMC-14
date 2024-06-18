using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._CM14.Anchor;

public sealed class DeployableItemSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<DeployableItemComponent>> _deployables = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<DeployableItemComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DeployableItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DeployableItemComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<DeployableItemComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<DeployableItemComponent, DragDropDraggedEvent>(OnDragDropDragged);

        SubscribeLocalEvent<HandsComponent, CanDropTargetEvent>(OnCanDropTarget);
    }

    private void OnCanDrag(Entity<DeployableItemComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDragged(Entity<DeployableItemComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!Transform(ent).Anchored)
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnCanDropTarget(Entity<HandsComponent> ent, ref CanDropTargetEvent args)
    {
        if (ent.Owner != args.User ||
            !CanPickup(args.Dragged, args.User))
        {
            return;
        }

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragDropDragged(Entity<DeployableItemComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.User != args.Target || !CanPickup(ent, args.User))
            return;

        args.Handled = true;

        _physics.SetBodyType(ent, BodyType.Dynamic);
        if (!_hands.TryPickupAnyHand(args.User, ent))
        {
            _physics.SetBodyType(ent, BodyType.Static);
            return;
        }

        ent.Comp.Position = DeployableItemPosition.None;
        _appearance.SetData(ent, DeployableItemVisuals.Deployed, false);
        Dirty(ent);
    }

    private void OnAfterInteract(Entity<DeployableItemComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        args.Handled = true;
        Deploy(ent, args.User, args.ClickLocation);
    }

    private void OnUseInHand(Entity<DeployableItemComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        Deploy(ent, args.User, _transform.GetMoverCoordinates(ent));
    }

    private void Deploy(Entity<DeployableItemComponent> ent, EntityUid user, EntityCoordinates location)
    {
        location = location.SnapToGrid();
        var transform = Transform(ent);
        var transformEnt = new Entity<TransformComponent?>(ent, transform);
        if (_transform.GetGrid(transformEnt) == null)
            return;

        var map = _transform.GetMapId(transformEnt);
        var worldPos = _transform.ToMapCoordinates(location).Position;
        _deployables.Clear();
        _entityLookup.GetEntitiesInRange(map, worldPos, 0.3f, _deployables);

        var lower = false;
        var upper = false;
        foreach (var deployable in _deployables)
        {
            if (deployable.Owner == ent.Owner)
                continue;

            switch (deployable.Comp.Position)
            {
                case DeployableItemPosition.Lower:
                    lower = true;
                    break;
                case DeployableItemPosition.Upper:
                    upper = true;
                    break;
            }

            if (lower && upper)
            {
                _popup.PopupClient(Loc.GetString("cm-magazine-box-no-space"), user, PopupType.SmallCaution);
                return;
            }
        }

        DeployableItemPosition position;
        Vector2 offset;
        if (!lower)
        {
            position = DeployableItemPosition.Lower;
            offset = new Vector2(0, -0.25f);
        }
        else if (!upper)
        {
            position = DeployableItemPosition.Upper;
            offset = new Vector2(0, 0.25f);
        }
        else
        {
            return;
        }

        location = location.Offset(offset);
        if (!_hands.TryDrop(user, ent, location))
            return;

        _transform.SetCoordinates(ent, location);
        _physics.SetBodyType(ent, BodyType.Static);

        ent.Comp.Position = position;
        _appearance.SetData(ent, DeployableItemVisuals.Deployed, true);
        Dirty(ent);
    }

    private bool CanPickup(EntityUid deployable, EntityUid user)
    {
        return _hands.TryGetEmptyHand(user, out _) &&
               _actionBlocker.CanPickup(user, deployable) &&
               TryComp(deployable, out DeployableItemComponent? deployableComp) &&
               deployableComp.Position != DeployableItemPosition.None;
    }
}
