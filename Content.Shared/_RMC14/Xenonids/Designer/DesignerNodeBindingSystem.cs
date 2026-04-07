using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Weeds;
using System;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Designer;

// Binds design nodes to weed for weedbound walls/doors.
public sealed class DesignerNodeBindingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly DesignerNodeOverlaySystem _overlay = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _nodesByWeed = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignNodeComponent, ComponentStartup>(OnNodeStartup);
        SubscribeLocalEvent<DesignNodeComponent, MapInitEvent>(OnNodeMapInit);
        SubscribeLocalEvent<DesignNodeComponent, EntityTerminatingEvent>(OnNodeTerminating);
    }

    private void OnNodeStartup(Entity<DesignNodeComponent> node, ref ComponentStartup args)
    {
        BindToWeeds(node);
        _overlay.EnsureOverlay(node.Owner, node.Comp);
    }

    private void OnNodeMapInit(Entity<DesignNodeComponent> node, ref MapInitEvent args)
    {
        BindToWeeds(node);
        _overlay.EnsureOverlay(node.Owner, node.Comp);
    }

    private void OnNodeTerminating(Entity<DesignNodeComponent> node, ref EntityTerminatingEvent args)
    {
        CleanupNode(node.Owner, node.Comp);
    }

    public void CleanupNode(EntityUid uid, DesignNodeComponent nodeComp)
    {
        if (_net.IsClient)
            return;

        _overlay.CleanupOverlay(uid, nodeComp);

        if (nodeComp.BoundXeno is { } placer && TryComp(placer, out DesignerStrainComponent? designer))
        {
            designer.DesignNodes.Remove(uid);
            designer.CurrentDesignNodes = designer.DesignNodes.Count;
            Dirty(placer, designer);

            _ui.SetUiState(placer, XenoChooseStructureUI.Key,
                new XenoChooseStructureBuiState(true, designer.CurrentDesignNodes, designer.MaxDesignNodes));
        }

        if (nodeComp.BoundWeed is not { } boundWeed)
            return;

        if (_nodesByWeed.TryGetValue(boundWeed, out var set))
        {
            set.Remove(uid);
            if (set.Count == 0)
                _nodesByWeed.Remove(boundWeed);
        }
    }

    public void CleanupWeeds(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        if (!_nodesByWeed.TryGetValue(uid, out var nodes))
            return;

        foreach (var node in nodes.ToArray())
        {
            if (Exists(node))
                QueueDel(node);
        }

        _nodesByWeed.Remove(uid);
    }

    private void BindToWeeds(Entity<DesignNodeComponent> node)
    {
        if (!_net.IsServer)
            return;

        // Always refresh; weeds can be spawned before/after the node depending on construction ordering.
        var coords = Transform(node.Owner).Coordinates;
        EntityUid? weed = null;

        using (var enumerator = _rmcMap.GetAnchoredEntitiesEnumerator<XenoWeedsComponent>(coords))
        {
            if (enumerator.MoveNext(out var weedUid))
                weed = weedUid;
        }

        // Update binding + register for automatic cleanup when weeds die.
        var oldWeed = node.Comp.BoundWeed;
        node.Comp.BoundWeed = weed;
        Dirty(node);

        if (oldWeed is { } previous && previous != weed && _nodesByWeed.TryGetValue(previous, out var oldSet))
        {
            oldSet.Remove(node.Owner);
            if (oldSet.Count == 0)
                _nodesByWeed.Remove(previous);
        }

        if (weed is { } bound)
        {
            if (!_nodesByWeed.TryGetValue(bound, out var set))
            {
                set = new HashSet<EntityUid>();
                _nodesByWeed[bound] = set;
            }

            set.Add(node.Owner);
            return;
        }

        QueueDel(node.Owner);
    }
}
