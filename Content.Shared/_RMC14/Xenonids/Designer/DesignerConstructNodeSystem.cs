using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;
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

    private static readonly TimeSpan ConstructBuildTime = TimeSpan.FromSeconds(4);
    private static readonly FixedPoint2 ConstructNodePlasmaCost = FixedPoint2.New(70);
    private const string XenoStructuresAnimation = "RMCEffect";

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoPlasmaComponent> _plasmaQuery;

    private readonly HashSet<EntityUid> _buildingNodes = new();

    private static readonly SoundSpecifier BuildSound = new SoundCollectionSpecifier("RMCResinBuild")
    {
        Params = AudioParams.Default.WithVolume(-10f),
    };

    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _plasmaQuery = GetEntityQuery<XenoPlasmaComponent>();

        SubscribeLocalEvent<DesignNodeComponent, Content.Shared.Interaction.ActivateInWorldEvent>(OnDesignNodeActivate);
        SubscribeLocalEvent<DesignNodeComponent, GettingInteractedWithAttemptEvent>(OnDesignNodeGettingInteractedWithAttempt);
        SubscribeLocalEvent<DesignNodeComponent, EntityTerminatingEvent>(OnDesignNodeTerminating);
    }

    private void OnDesignNodeGettingInteractedWithAttempt(Entity<DesignNodeComponent> node, ref GettingInteractedWithAttemptEvent args)
    {
        if (_buildingNodes.Contains(node.Owner))
            args.Cancelled = true;
    }

    private void OnDesignNodeActivate(Entity<DesignNodeComponent> node, ref Content.Shared.Interaction.ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        // While the node is mid-build, it should not be interactable.
        if (_buildingNodes.Contains(node.Owner))
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
        if (!_buildingNodes.Add(node.Owner))
            return false;

        if (!_plasmaQuery.TryComp(user, out var plasma) || !_xenoPlasma.TryRemovePlasmaPopup((user, plasma), ConstructNodePlasmaCost))
        {
            _buildingNodes.Remove(node.Owner);
            return false;
        }

        // Lunge the activator towards the node (no tile/arc sprite).
        PlayConstructNodeLunge(user, node.Owner);

        var coords = Transform(node.Owner).Coordinates;
        _audio.PlayPredicted(BuildSound, coords, user);

        if (!_net.IsServer)
            return true;

        // Play the same construction animation used by normal xeno construction.
        // Animation effects are named as RMCEffect + <structure proto id>.
        var isDoor = node.Comp.DesignMark.Contains("door", StringComparison.OrdinalIgnoreCase);
        var isThickVariant = _xenoQuery.TryComp(user, out var xeno) && CanBuildThickFromConstructNode(user, xeno.Role);
        var animationChoice = (isDoor, isThickVariant) switch
        {
            (true, true) => "DoorXenoResinThick",
            (true, false) => "DoorXenoResin",
            (false, true) => "WallXenoResinThick",
            (false, false) => "WallXenoResin",
        };

        var effectId = XenoStructuresAnimation + animationChoice;
        if (_prototype.HasIndex(effectId))
        {
            var effect = Spawn(effectId, coords);
            RaiseNetworkEvent(
                new XenoConstructionAnimationStartEvent(GetNetEntity(effect), GetNetEntity(user), ConstructBuildTime),
                Filter.PvsExcept(effect)
            );
        }

        var nodeUid = node.Owner;
        Timer.Spawn(
            ConstructBuildTime,
            () => CompleteConstructNodeBuild(nodeUid, user)
        );

        return true;
    }

    private void PlayConstructNodeLunge(EntityUid user, EntityUid nodeUid)
    {
        if (!TryComp(user, out TransformComponent? userXform))
            return;

        var mapPos = _transform.ToMapCoordinates(Transform(nodeUid).Coordinates).Position;
        var invMatrix = _transform.GetInvWorldMatrix(userXform);
        var localPos = Vector2.Transform(mapPos, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(user, user, Angle.Zero, localPos, animation: null);
    }

    private void CompleteConstructNodeBuild(EntityUid nodeUid, EntityUid user)
    {
        _buildingNodes.Remove(nodeUid);

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

        // Determine wall variant based on xeno role
        var xeno = Comp<XenoComponent>(user);
        var isThickVariant = CanBuildThickFromConstructNode(user, xeno.Role);

        // If the construct node is on hive weeds, always build thick weedbound resin.
        var coords = Transform(nodeUid).Coordinates;
        if (_transform.GetGrid(coords) is { } gridUid && TryComp<MapGridComponent>(gridUid, out var gridComp))
            isThickVariant |= _weeds.IsOnHiveWeeds((gridUid, gridComp!), coords);

        // Builds walls or doors based on design mark.
        var isDoor = nodeComp.DesignMark.Contains("door", StringComparison.OrdinalIgnoreCase);

        var proto = (isDoor, isThickVariant) switch
        {
            (true, true) => "DoorXenoResinThickWeedbound",
            (true, false) => "DoorXenoResinWeedbound",
            (false, true) => "WallXenoResinThickWeedbound",
            (false, false) => "WallXenoResinWeedbound",
        };

        var spawned = EntityManager.SpawnEntity(proto, coords);

        var weedbound = EnsureComp<WeedboundWallComponent>(spawned);
        weedbound.IsThickVariant = isThickVariant;

        _weedbound.RegisterWeedboundStructure(spawned, boundWeed);

        EntityManager.DeleteEntity(nodeUid);
        _popup.PopupEntity("You infuse the node with plasma", spawned, PopupType.Small);
    }

    private void OnDesignNodeTerminating(Entity<DesignNodeComponent> node, ref EntityTerminatingEvent args)
    {
        _buildingNodes.Remove(node.Owner);
    }

    // Drone evos (except designer) build thick walls/doors from construct nodes.
    private bool CanBuildThickFromConstructNode(EntityUid user, string roleId)
    {
        if (HasComp<ResinWhispererComponent>(user) || roleId == "CMXenoCarrier" || roleId == "CMXenoBurrower" || roleId == "CMXenoQueen")
            return true;

        if (HasComp<DesignerStrainComponent>(user))
            return false;

        return roleId is "CMXenoHivelord";
    }

}
