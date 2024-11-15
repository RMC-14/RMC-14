using System.Collections.Immutable;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Ladder;
using Content.Shared._RMC14.Map;
using Content.Shared.Construction.Components;
using Content.Shared.Coordinates;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction.Upgrades;

public sealed class RMCUpgradeSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public ImmutableArray<EntityPrototype> UpgradePrototypes { get; private set; }

    private EntityQuery<RMCConstructionUpgradeItemComponent> _upgradeItemQuery;

    public override void Initialize()
    {
        _upgradeItemQuery = GetEntityQuery<RMCConstructionUpgradeItemComponent>();

        SubscribeLocalEvent<RMCConstructionUpgradeTargetComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<RMCConstructionUpgradeTargetComponent>(RMCConstructionUpgradeUiKey.Key,
            subs =>
            {
                subs.Event<RMCConstructionUpgradeBuiMsg>(OnUpgradeBuiMsg);
            });

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RefreshUpgradePrototypes();
    }

    private void OnInteractUsing(Entity<RMCConstructionUpgradeTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        var used = args.Used;

        if (_upgradeItemQuery.HasComp(used))
        {
            _ui.OpenUi(ent.Owner, RMCConstructionUpgradeUiKey.Key, user);
            args.Handled = true;
        }
    }

    private void OnUpgradeBuiMsg(Entity<RMCConstructionUpgradeTargetComponent> ent, ref RMCConstructionUpgradeBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, RMCConstructionUpgradeUiKey.Key);

        var user = args.Actor;

        if (!_prototypes.TryIndex(args.Upgrade, out var upgradeProto))
            return;

        if (!TryGetUpgrade(args.Upgrade, out var upgrade))
            return;

        var upgradeComp = upgrade.Comp;

        EntityUid? upgradeItem = null;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (hand.HeldEntity == null)
                continue;

            if (_upgradeItemQuery.HasComp(hand.HeldEntity))
            {
                upgradeItem = hand.HeldEntity;
                break;
            }
        }

        if (upgradeItem == null)
            return;

        if (upgradeComp.Material != null)
        {
            if (TryComp<StackComponent>(upgradeItem, out var stack) && stack.StackTypeId == upgradeComp.Material)
            {
                if (!_stack.Use(upgradeItem.Value, upgradeComp.Amount, stack))
                {
                    return;
                }
            }
        }

        if (_net.IsClient)
            return;
    }

    public bool TryGetUpgrade(EntProtoId prototype, out Entity<RMCConstructionUpgradeComponent> upgrade)
    {
        var upgradeQuery = EntityQueryEnumerator<RMCConstructionUpgradeComponent, MetaDataComponent>();
        while (upgradeQuery.MoveNext(out var uid, out var comp, out var metaData))
        {
            if (metaData.EntityPrototype?.ID != prototype.Id)
                continue;

            upgrade = (uid, comp);
            return true;
        }

        upgrade = default;
        return false;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            RefreshUpgradePrototypes();
    }

    private void RefreshUpgradePrototypes()
    {
        var entBuilder = ImmutableArray.CreateBuilder<EntityPrototype>();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.HasComponent<RMCConstructionUpgradeComponent>())
                entBuilder.Add(entity);
        }

        UpgradePrototypes = entBuilder.ToImmutable();
    }
}
