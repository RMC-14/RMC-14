using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Overwatch;

public sealed class RMCOverwatchTripodCameraSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCConstructionSystem _construction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly RMCMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, RMCOverwatchTripodDeployDoAfterEvent>(OnDeployDoAfter);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, RMCOverwatchTripodPickupDoAfterEvent>(OnPickupDoAfter);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, ExplosionReceivedEvent>(OnExplosionReceived);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<RMCOverwatchTripodCameraComponent, CombatModeShouldHandInteractEvent>(OnCombatModeShouldHandInteract);
    }

    private void OnUseInHand(Entity<RMCOverwatchTripodCameraComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Deployed)
            return;

        args.Handled = true;
        TryStartDeploy(ent, args.User);
    }

    private void OnInteractHand(Entity<RMCOverwatchTripodCameraComponent> ent, ref InteractHandEvent args)
    {
        if (!ent.Comp.Deployed)
            return;

        args.Handled = true;
        TryStartPickup(ent, args.User);
    }

    private void OnGetVerbs(Entity<RMCOverwatchTripodCameraComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("rmc-overwatch-tripod-camera-rename"),
            Act = () => OpenRename(ent, user),
        });

        if (ent.Comp.Deployed)
        {
            args.Verbs.Add(new ActivationVerb
            {
                Text = Loc.GetString("rmc-overwatch-tripod-camera-pick-up"),
                Act = () => TryStartPickup(ent, user),
            });
        }
        else
        {
            args.Verbs.Add(new ActivationVerb
            {
                Text = Loc.GetString("rmc-overwatch-tripod-camera-deploy"),
                Act = () => TryStartDeploy(ent, user),
            });
        }
    }

    private void OpenRename(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid user)
    {
        var ev = new RMCOverwatchTripodRenameInputEvent(GetNetEntity(ent), GetNetEntity(user));
        _dialog.OpenInput(user, Loc.GetString("rmc-overwatch-tripod-camera-rename-prompt"), ev, characterLimit: 32);
    }

    private void TryStartDeploy(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid user)
    {
        if (!CanDeploy(ent, user))
            return;

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DeployDelay, new RMCOverwatchTripodDeployDoAfterEvent(), ent)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true,
            RequireCanInteract = true,
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnDeployDoAfter(Entity<RMCOverwatchTripodCameraComponent> ent, ref RMCOverwatchTripodDeployDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            !CanDeploy(ent, args.User, out var coordinates, out var rotation) ||
            !_hands.TryDrop(args.User, ent, coordinates))
        {
            return;
        }

        args.Handled = true;
        _transform.SetCoordinates(ent, Transform(ent), coordinates, rotation);
        _transform.AnchorEntity(ent);
        ent.Comp.Deployed = true;
        ent.Comp.XenoSlashes = 0;
        ent.Comp.Squad = CompOrNull<SquadMemberComponent>(args.User)?.Squad;
        EnsureComp<OverwatchCameraComponent>(ent);
        _appearance.SetData(ent, RMCOverwatchTripodCameraVisuals.Deployed, true);
        _audio.PlayPredicted(ent.Comp.DeploySound, ent, args.User);
        Dirty(ent);
        _popup.PopupPredicted(Loc.GetString("rmc-overwatch-tripod-camera-deployed"), ent, args.User);
    }

    private void TryStartPickup(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid user)
    {
        if (!CanPickup(ent, user))
            return;

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.PickupDelay, new RMCOverwatchTripodPickupDoAfterEvent(), ent, ent)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
            RequireCanInteract = true,
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnPickupDoAfter(Entity<RMCOverwatchTripodCameraComponent> ent, ref RMCOverwatchTripodPickupDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !CanPickup(ent, args.User))
            return;

        args.Handled = true;
        Collapse(ent, args.User);
    }

    private void OnAttacked(Entity<RMCOverwatchTripodCameraComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.Deployed ||
            !TryComp(args.User, out XenoComponent? xeno) ||
            xeno.Tier == 0)
        {
            return;
        }

        ent.Comp.XenoSlashes++;
        if (ent.Comp.XenoSlashes < ent.Comp.SlashesToBreak)
        {
            Dirty(ent);
            return;
        }

        _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-collapsed"), ent);
        Collapse(ent);
    }

    private void OnCombatModeShouldHandInteract(
        Entity<RMCOverwatchTripodCameraComponent> ent,
        ref CombatModeShouldHandInteractEvent args)
    {
        if (HasComp<XenoComponent>(args.User))
            args.Cancelled = true;
    }

    private void OnExplosionReceived(Entity<RMCOverwatchTripodCameraComponent> ent, ref ExplosionReceivedEvent args)
    {
        if (!ent.Comp.Deployed)
            return;

        var damage = args.Damage.GetTotal().Float();
        if (damage >= ent.Comp.DestroyExplosionDamage)
        {
            _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-destroyed"), ent);
            QueueDel(ent);
            return;
        }

        if (damage >= ent.Comp.CollapseExplosionDamage)
        {
            _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-collapsed"), ent);
            Collapse(ent);
        }
    }

    public void Collapse(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid? user = null)
    {
        if (!ent.Comp.Deployed)
            return;

        RemComp<OverwatchCameraComponent>(ent);
        _transform.Unanchor(ent);
        ent.Comp.Deployed = false;
        ent.Comp.XenoSlashes = 0;
        ent.Comp.Squad = null;
        _appearance.SetData(ent, RMCOverwatchTripodCameraVisuals.Deployed, false);
        Dirty(ent);

        if (user != null && _hands.TryPickupAnyHand(user.Value, ent))
            _popup.PopupPredicted(Loc.GetString("rmc-overwatch-tripod-camera-picked-up"), ent, user.Value);
    }

    public string GetDisplayLabel(Entity<RMCOverwatchTripodCameraComponent> ent)
    {
        if (ent.Comp.Deployed &&
            ent.Comp.Squad is { } squad &&
            !TerminatingOrDeleted(squad))
        {
            return $"{Name(squad)} - {ent.Comp.Label}";
        }

        return ent.Comp.Label;
    }

    public void SetLabel(Entity<RMCOverwatchTripodCameraComponent> ent, string label)
    {
        ent.Comp.Label = label;
        Dirty(ent);
    }

    private void OnExamined(Entity<RMCOverwatchTripodCameraComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("rmc-overwatch-tripod-camera-examine-label",
            ("label", FormattedMessage.EscapeText(GetDisplayLabel(ent)))));

        if (ent.Comp.Deployed && ent.Comp.Squad is { } squad && !TerminatingOrDeleted(squad))
        {
            args.PushMarkup(Loc.GetString("rmc-overwatch-tripod-camera-examine-squad",
                ("squad", FormattedMessage.EscapeText(Name(squad)))));
        }
    }

    private bool CanPickup(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid user)
    {
        return ent.Comp.Deployed &&
               _mobState.IsAlive(user) &&
               !_mobState.IsIncapacitated(user) &&
               _actionBlocker.CanInteract(user, ent) &&
               _interaction.InRangeUnobstructed(user, ent.Owner);
    }

    private bool CanDeploy(Entity<RMCOverwatchTripodCameraComponent> ent, EntityUid user)
    {
        return CanDeploy(ent, user, out _, out _);
    }

    private bool CanDeploy(
        Entity<RMCOverwatchTripodCameraComponent> ent,
        EntityUid user,
        out EntityCoordinates coordinates,
        out Angle rotation)
    {
        var moverCoordinates = _transform.GetMoverCoordinateRotation(user, Transform(user));
        coordinates = _map.SnapToGrid(moverCoordinates.Coords);
        rotation = moverCoordinates.worldRot.GetCardinalDir().ToAngle();

        if (ent.Comp.Deployed ||
            !_mobState.IsAlive(user) ||
            _mobState.IsIncapacitated(user) ||
            !_actionBlocker.CanInteract(user, ent))
        {
            return false;
        }

        if (_hands.GetActiveItem(user) != ent.Owner)
        {
            _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-active-hand"), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (_container.IsEntityInContainer(user) || HasComp<VehicleInteriorOccupantComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-invalid-location"), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (_transform.GetGrid(coordinates) is null ||
            !_map.CanBuildOn(coordinates) ||
            !_construction.CanBuildAt(coordinates, Name(ent), out _, anchoring: true))
        {
            _popup.PopupClient(Loc.GetString("rmc-overwatch-tripod-camera-invalid-location"), ent, user, PopupType.SmallCaution);
            return false;
        }

        var attempt = new RMCOverwatchTripodDeployAttemptEvent(user);
        RaiseLocalEvent(ent.Owner, ref attempt);
        return !attempt.Cancelled;
    }
}
