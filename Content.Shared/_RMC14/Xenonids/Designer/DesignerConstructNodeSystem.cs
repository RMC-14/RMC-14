using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;
using Content.Shared._RMC14.Xenonids.Burrow;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Weeds;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed partial class DesignerConstructNodeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hiveSystem = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;
    [Dependency] private readonly WeedboundWallSystem _weedbound = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoPlasmaComponent> _plasmaQuery;


    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _plasmaQuery = GetEntityQuery<XenoPlasmaComponent>();

        SubscribeLocalEvent<DesignNodeComponent, ActivateInWorldEvent>(OnDesignNodeActivate);
        SubscribeLocalEvent<DesignNodeComponent, GettingInteractedWithAttemptEvent>(OnDesignNodeGettingInteractedWithAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;

        List<(EntityUid Node, EntityUid User)>? completed = null;
        var buildQuery = EntityQueryEnumerator<DesignerConstructNodeBuildingComponent>();
        while (buildQuery.MoveNext(out var node, out var build))
        {
            if (Deleted(node) || Terminating(node))
                continue;

            if (now < build.EndTime)
                continue;

            completed ??= new List<(EntityUid, EntityUid)>(4);
            completed.Add((node, build.User));
        }

        if (completed == null)
            return;

        foreach (var (node, user) in completed)
        {
            CompleteConstructNodeBuild(node, user);
        }
    }

    private void OnDesignNodeGettingInteractedWithAttempt(Entity<DesignNodeComponent> node, ref GettingInteractedWithAttemptEvent args)
    {
        if (HasComp<DesignerConstructNodeBuildingComponent>(node.Owner))
            args.Cancelled = true;
    }

    private void OnDesignNodeActivate(Entity<DesignNodeComponent> node, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        // While the node is mid-build, it should not be interactable.
        if (HasComp<DesignerConstructNodeBuildingComponent>(node.Owner))
            return;

        var user = args.User;

        // Only xenomorphs can use design nodes.
        if (!_xenoQuery.HasComponent(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-designnode-human-interact"), node, user, PopupType.SmallCaution);
            return;
        }

        // Only plasma-using castes should be able to donate effort to construct nodes.
        if (!_plasmaQuery.HasComponent(user))
        {
            _popup.PopupClient(Loc.GetString("rmc-designnode-no-plasma"), node, user, PopupType.SmallCaution);
            return;
        }

        // Bound nodes should only be usable by the same hive as the designer who placed them.
        if (node.Comp.BoundXeno is { } boundXeno && !_hiveSystem.FromSameHive(user, boundXeno))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-use-node-wrong-hive"), node, user, PopupType.SmallCaution);
            return;
        }

        // Only construct-type nodes can be interacted with to build walls
        if (node.Comp.NodeType != DesignNodeType.Construct)
        {
            _popup.PopupClient("This design node cannot be used to build walls.", node, user, PopupType.SmallCaution);
            return;
        }

        // Nodes cannot be used off weeds (and shouldn't exist there in the first place).
        // IMPORTANT: this is weeds-in-general (any Xeno weeds), not "hive weeds".
        if (node.Comp.BoundWeed is not { } boundWeed || !Exists(boundWeed) || !HasComp<Content.Shared._RMC14.Xenonids.Weeds.XenoWeedsComponent>(boundWeed))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-node-needs-weed"), node, user, PopupType.SmallCaution);
            return;
        }

        // Marks interaction as handled to prevent unneeded logs/processing
        if (StartConstructNodeBuild(node, user))
            args.Handled = true;
    }

    private bool StartConstructNodeBuild(Entity<DesignNodeComponent> node, EntityUid user)
    {
        if (HasComp<DesignerConstructNodeBuildingComponent>(node.Owner))
            return false;

        if (!_plasmaQuery.TryComp(user, out var plasma) || !_xenoPlasma.TryRemovePlasmaPopup((user, plasma), node.Comp.ConstructPlasmaCost))
            return false;

        // Lunge the activator towards the node
        PlayConstructNodeLunge(user, node.Owner);

        if (!TryComp(node.Owner, out TransformComponent? nodeTransform))
            return false;

        var coords = nodeTransform.Coordinates;
        _audio.PlayPredicted(node.Comp.ConstructBuildSound, coords, user);

        if (!_net.IsServer)
            return true;

        // Determine build choices at activation time.
        // (So they persist even if the user changes state during the wind-up.)
        var isThickVariant = CanBuildThickFromConstructNode(user);

        var build = EnsureComp<DesignerConstructNodeBuildingComponent>(node.Owner);
        build.User = user;
        build.BuildTime = node.Comp.ConstructBuildTime;
        build.EndTime = _timing.CurTime + node.Comp.ConstructBuildTime;
        build.ThickVariant = isThickVariant;
        Dirty(node.Owner, build);

        // Play the same construction animation used by normal xeno construction.
        // The effect prototypes are provided by the node component (YAML-configurable).
        var effectId = isThickVariant ? node.Comp.ConstructAnimationEffectThick : node.Comp.ConstructAnimationEffect;
        if (_prototype.HasIndex(effectId))
        {
            var effect = Spawn(effectId, coords);
            if (TryGetNetEntity(effect, out var netEffect) && TryGetNetEntity(user, out var netUser))
            {
                RaiseNetworkEvent(
                    new XenoConstructionAnimationStartEvent(netEffect.Value, netUser.Value, node.Comp.ConstructBuildTime),
                    Filter.PvsExcept(effect)
                );
            }
        }

        return true;
    }

    private void PlayConstructNodeLunge(EntityUid user, EntityUid nodeUid)
    {
        if (!TryComp(user, out TransformComponent? userTransform))
            return;

        if (!TryComp(nodeUid, out TransformComponent? nodeTransform))
            return;

        var mapPos = _transform.ToMapCoordinates(nodeTransform.Coordinates).Position;
        var invMatrix = _transform.GetInvWorldMatrix(userTransform);
        var localPos = Vector2.Transform(mapPos, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userTransform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(user, user, Angle.Zero, localPos, animation: null);
    }

    private void CompleteConstructNodeBuild(EntityUid nodeUid, EntityUid user)
    {
        var wasThickVariant = false;
        if (TryComp(nodeUid, out DesignerConstructNodeBuildingComponent? build))
            wasThickVariant = build.ThickVariant;

        RemComp<DesignerConstructNodeBuildingComponent>(nodeUid);

        if (!TryComp(nodeUid, out DesignNodeComponent? nodeComp))
            return;

        if (nodeComp.NodeType != DesignNodeType.Construct)
            return;

        // Verify xeno still exists
        if (!_xenoQuery.HasComponent(user))
            return;

        if (!_plasmaQuery.HasComponent(user))
            return;

        if (nodeComp.BoundXeno is { } boundXeno && !_hiveSystem.FromSameHive(user, boundXeno))
            return;

        // Weeds must still exist for the weedbound structure to be built.
        if (nodeComp.BoundWeed is not { } boundWeed || !Exists(boundWeed) || !HasComp<Content.Shared._RMC14.Xenonids.Weeds.XenoWeedsComponent>(boundWeed))
        {
            EntityManager.DeleteEntity(nodeUid);
            return;
        }

        // Determine wall variant based on the activation-time choice.
        // Hive weeds always force thick regardless.
        var isThickVariant = wasThickVariant;

        // If the construct node is on hive weeds, always build thick weedbound resin.
        if (!TryComp(nodeUid, out TransformComponent? nodeTransform))
            return;

        var coords = nodeTransform.Coordinates;
        if (_transform.GetGrid(coords) is { } gridUid && TryComp<MapGridComponent>(gridUid, out var gridComp))
            isThickVariant |= _weeds.IsOnHiveWeeds((gridUid, gridComp!), coords);

        var proto = isThickVariant ? nodeComp.ConstructWeedboundThick : nodeComp.ConstructWeedbound;
        var spawned = EntityManager.SpawnEntity(proto, coords);

        var weedbound = EnsureComp<WeedboundWallComponent>(spawned);
        weedbound.IsThickVariant = isThickVariant;

        _weedbound.RegisterWeedboundStructure(spawned, boundWeed);

        EntityManager.DeleteEntity(nodeUid);
        _popup.PopupEntity("You infuse the node with plasma", spawned, PopupType.Small);
    }

    // Some castes build thick walls/doors from construct nodes.
    private bool CanBuildThickFromConstructNode(EntityUid user)
    {
        return HasComp<CanBuildThickFromConstructNodeComponent>(user);
    }

}
