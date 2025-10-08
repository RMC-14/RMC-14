using System.Numerics;
using Content.Shared._RMC14.Inventory;
using Content.Shared.ActionBlocker; using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Anchor;

public sealed class DeployableItemSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedCMInventorySystem _cmInventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<DeployableItemComponent>> _deployables = new();

    private static readonly ProtoId<TagPrototype> CatwalkTag = "Catwalk";
    private static readonly ProtoId<TagPrototype> StairsTag = "RMCStairs";
    private static readonly ProtoId<TagPrototype> CarpetTag = "Carpet";

    public override void Initialize()
    {
        SubscribeLocalEvent<DeployableItemComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DeployableItemComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<DeployableItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DeployableItemComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<DeployableItemComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<DeployableItemComponent, DragDropDraggedEvent>(OnDragDropDragged);
        SubscribeLocalEvent<DeployableItemComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DeployableItemComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);

        SubscribeLocalEvent<DeployFoldableComponent, UseInHandEvent>(OnFoldableUseInHand);

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
        Pickup(ent, args.User);
    }

    private void OnExamined(Entity<DeployableItemComponent> ent, ref ExaminedEvent args)
    {
        var (filled, total) = _cmInventory.GetItemSlotsFilled(ent.Owner);
        using (args.PushGroup(nameof(DeployableItemComponent)))
        {
            if (ent.Comp.Position == DeployableItemPosition.None)
            {
                args.PushMarkup(Loc.GetString("cm-magazine-box-examine-not-deployed"));

                if (filled == 0)
                    args.PushMarkup(Loc.GetString("cm-magazine-box-examine-empty"));
                else if (filled < total * ent.Comp.AlmostEmptyThreshold)
                    args.PushMarkup(Loc.GetString("cm-magazine-box-examine-almost-empty"));
                else if (filled < total * ent.Comp.HalfFullThreshold)
                    args.PushMarkup(Loc.GetString("cm-magazine-box-examine-half-full"));
                else
                    args.PushMarkup(Loc.GetString("cm-magazine-box-examine-almost-full"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("cm-magazine-box-examine-deployed-click"));
                args.PushMarkup(Loc.GetString("cm-magazine-box-examine-deployed-drag"));
                args.PushMarkup(Loc.GetString("cm-magazine-box-examine-magazines", ("filled", filled), ("total", total)));
            }
        }
    }

    private void OnGetAlternativeVerbs(Entity<DeployableItemComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.Position == DeployableItemPosition.None)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("cm-magazine-box-pick-up"),
            Act = () => Pickup(ent, user),
            Icon = new Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        });
    }

    private void OnAfterInteract(Entity<DeployableItemComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (args.Target != null && !_tag.HasAnyTag(args.Target.Value, CatwalkTag, StairsTag, CarpetTag))
            return;

        args.Handled = true;
        Deploy(ent, args.User, args.ClickLocation);
    }

    private void OnInteractHand(Entity<DeployableItemComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.LeftClickPickup)
            return;

        if (ent.Comp.Position == DeployableItemPosition.None)
            return;

        args.Handled = true;
        Pickup(ent, args.User);
    }

    private void OnUseInHand(Entity<DeployableItemComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        Deploy(ent, args.User, _transform.GetMoverCoordinates(ent));
    }

    private void OnFoldableUseInHand(Entity<DeployFoldableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FoldableComponent>(ent, out var foldable))
            return;

        var coordinates = _transform.GetMoverCoordinates(args.User);
        if (!TryComp(ent.Owner, out PhysicsComponent? anchorBody) ||
            !_anchorable.TileFree(coordinates, anchorBody))
        {
            _popup.PopupPredicted(Loc.GetString("foldable-deploy-fail", ("object", ent)), ent, args.User);
            return;
        }

        if (!TryComp(args.User, out HandsComponent? hands) ||
            !_hands.TryDrop((args.User, hands), ent, targetDropLocation: coordinates))
        {
            return;
        }

        if (!_foldable.TrySetFolded(ent, foldable, false))
        {
            _hands.TryPickup(args.User, ent, handsComp: hands);
            return;
        }

        args.Handled = true;
    }

    private void Deploy(Entity<DeployableItemComponent> ent, EntityUid user, EntityCoordinates location)
    {
        location = _transform.GetMoverCoordinates(location).SnapToGrid();
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
        return !TerminatingOrDeleted(deployable) &&
               _hands.TryGetEmptyHand(user, out _) &&
               _actionBlocker.CanPickup(user, deployable) &&
               TryComp(deployable, out DeployableItemComponent? deployableComp) &&
               deployableComp.Position != DeployableItemPosition.None;
    }

    private void Pickup(Entity<DeployableItemComponent> ent, EntityUid user)
    {
        if (!CanPickup(ent, user))
            return;

        _physics.SetBodyType(ent, BodyType.Dynamic);
        if (!_hands.TryPickupAnyHand(user, ent))
        {
            _physics.SetBodyType(ent, BodyType.Static);
            return;
        }

        ent.Comp.Position = DeployableItemPosition.None;
        _appearance.SetData(ent, DeployableItemVisuals.Deployed, false);
        Dirty(ent);
    }
}
