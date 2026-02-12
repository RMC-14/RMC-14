using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Explosion;

public abstract partial class SharedRMCLandmineSystem : EntitySystem
{
    [Dependency] protected readonly GunIFFSystem GunIff = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCLandmineComponent, ClaymoreDeployDoafterEvent>(OnClaymoreDeploy);
        SubscribeLocalEvent<RMCLandmineComponent, ClaymoreDisarmDoafterEvent>(OnClaymoreDisarm);
        SubscribeLocalEvent<RMCLandmineComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCLandmineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCLandmineComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<RMCLandmineComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<RMCLandmineComponent, CombatModeShouldHandInteractEvent>(OnShouldInteract);
    }

    private void OnClaymoreDeploy(Entity<RMCLandmineComponent> ent, ref ClaymoreDeployDoafterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanDeployPopup(ent, args.User, out var coordinates, out var rotation))
            return;

        var xform = Transform(ent);
        _transform.SetCoordinates(ent, xform, coordinates, rotation);
        _transform.AnchorEntity(ent, xform);
        _collisionWake.SetEnabled(ent, false);
        _physics.SetBodyType(ent, BodyType.Static);

        GunIff.TryGetFaction(args.User, out var faction);
        ent.Comp.Faction = faction;
        ent.Comp.Armed = true;

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.DeploySound, ent, args.User);
    }

    private void OnClaymoreDisarm(Entity<RMCLandmineComponent> ent, ref ClaymoreDisarmDoafterEvent args)
    {
        _transform.Unanchor(ent);
        _collisionWake.SetEnabled(ent, true);
        ent.Comp.Armed = false;
        _physics.SetBodyType(ent, BodyType.Dynamic);

        if (TryComp(args.User, out HandsComponent? hands))
            _hands.TryPickupAnyHand(args.User, ent, handsComp: hands);

        UpdateAppearance(ent);
    }

    private void OnUseInHand(Entity<RMCLandmineComponent> ent, ref UseInHandEvent args)
    {
        if (!CanDeployPopup(ent, args.User, out _, out _))
            return;


        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.PlacementDelay,
            new ClaymoreDeployDoafterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
    }

    private void OnInteractUsing(Entity<RMCLandmineComponent> ent, ref InteractUsingEvent args)
    {
        if (!_tool.HasQuality(args.Used, ent.Comp.DisarmTool))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DisarmDelay,
            new ClaymoreDisarmDoafterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(new DoAfterArgs(doAfterArgs));
    }

    private void OnPreventCollide(Entity<RMCLandmineComponent> ent, ref PreventCollideEvent args)
    {
        if (!HasComp<XenoProjectileComponent>(args.OtherEntity) && !HasComp<MobStateComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnBeforeDamageChanged(Entity<RMCLandmineComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (HasComp<ProjectileComponent>(args.Source))
            args.Cancelled = true;
    }

    private void OnShouldInteract(Entity<RMCLandmineComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        if (HasComp<XenoComponent>(args.User))
            args.Cancelled = true;
    }

    private bool CanDeployPopup(Entity<RMCLandmineComponent> ent,
        EntityUid user,
        out EntityCoordinates coordinates,
        out Angle rotation)
    {
        var moverCoordinates = _transform.GetMoverCoordinateRotation(user, Transform(user));
        coordinates = moverCoordinates.Coords;
        rotation = moverCoordinates.worldRot.GetCardinalDir().ToAngle();

        // Can't deploy a mine while inside a container
        if (_container.IsEntityInContainer(user))
        {
            var msg = Loc.GetString("rmc-explosive-deploy-container", ("explosive", ent));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        // Can't deploy a mine on a tile that already has a mine on it
        var query = _rmcMap.GetAnchoredEntitiesEnumerator(moverCoordinates.Coords);
        while (query.MoveNext(out var anchoredUid))
        {
            if (!HasComp<RMCLandmineComponent>(anchoredUid))
                continue;

            var msg = Loc.GetString("rmc-mine-deploy-fail-occupied");
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void UpdateAppearance(Entity<RMCLandmineComponent> ent)
    {
        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Armed);
    }
}

/// <summary>
///     DoAfter event for placing the mine.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClaymoreDeployDoafterEvent : SimpleDoAfterEvent
{

}

/// <summary>
///     DoAfter event for disarming the mine.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClaymoreDisarmDoafterEvent : SimpleDoAfterEvent
{

}
