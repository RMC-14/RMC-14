﻿using System.Numerics;
using Content.Shared._RMC14.Dropship.Fabricator;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.Scaling;
using Content.Shared.GameTicking;
using Content.Shared.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Intel.Tech;

public sealed class TechSystem : EntitySystem
{
    [Dependency] private readonly DropshipFabricatorSystem _dropshipFabricator = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRequisitionsSystem _requisitions = default!;
    [Dependency] private readonly ScalingSystem _scaling = default!;

    private MapId? _purchasesMap;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<TechAnnounceEvent>(OnTechAnnounce);
        SubscribeLocalEvent<TechUnlockTierEvent>(OnTechUnlockTier);
        SubscribeLocalEvent<TechRequisitionsBudgetEvent>(OnTechRequisitionsBudget);
        SubscribeLocalEvent<TechDropshipBudgetEvent>(OnTechDropshipBudget);
        SubscribeLocalEvent<TechWarheadEvent>(OnTechWarhead);

        SubscribeLocalEvent<TechControlConsoleComponent, BeforeActivatableUIOpenEvent>(OnControlConsoleBeforeOpen);

        Subs.BuiEvents<TechControlConsoleComponent>(TechControlConsoleUI.Key,
            subs =>
            {
                subs.Event<TechPurchaseOptionBuiMsg>(OnPurchaseOptionMsg);
            });
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _purchasesMap = null;
    }

    private void OnTechAnnounce(TechAnnounceEvent ev)
    {
        var msg = Loc.GetString("rmc-announcement-message-raw", ("author", ev.Author), ("message", ev.Message));
        _marineAnnounce.AnnounceToMarines(msg);
    }

    private void OnTechUnlockTier(TechUnlockTierEvent ev)
    {
        var tree = _intel.EnsureTechTree();
        tree.Comp.Tree.Tier = ev.Tier;
        Dirty(tree);
    }

    private void OnTechRequisitionsBudget(TechRequisitionsBudgetEvent ev)
    {
        var scaling = _scaling.GetAliveHumanoids() / 50;
        scaling = Math.Max(1, scaling);
        _requisitions.ChangeBudget(ev.Amount * scaling);
    }

    private void OnTechDropshipBudget(TechDropshipBudgetEvent ev)
    {
        _dropshipFabricator.ChangeBudget(ev.Amount);
    }

    private void OnTechWarhead(TechWarheadEvent ev)
    {
        var map = EnsurePurchasesMap();
        var warhead = Spawn(ev.Warhead, new MapCoordinates(Vector2.Zero, map));
        EnsureComp<RequisitionsCustomDeliveryComponent>(warhead);
    }

    private void OnControlConsoleBeforeOpen(Entity<TechControlConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.Tree = _intel.EnsureTechTree().Comp.Tree;
        Dirty(ent);
    }

    private void OnPurchaseOptionMsg(Entity<TechControlConsoleComponent> ent, ref TechPurchaseOptionBuiMsg args)
    {
        if (_net.IsClient)
            return;

        var tree = _intel.EnsureTechTree();
        if (args.Tier < tree.Comp.Tree.Tier ||
            !tree.Comp.Tree.Options.TryGetValue(args.Tier, out var tier))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to buy tech option with invalid tier {args.Tier}");
            return;
        }

        if (args.Index < 0 ||
            !tier.TryGetValue(args.Index, out var option))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to buy tech option with invalid index {args.Index}");
            return;
        }

        if (option.Purchased && !option.Repurchasable)
            return;

        if (!_intel.TryUsePoints(option.CurrentCost))
            return;

        tier[args.Index] = option with
        {
            CurrentCost = option.Cost + option.Increase,
            Purchased = true,
        };
        Dirty(ent);

        foreach (var ev in option.Events)
        {
            RaiseLocalEvent(ev);
        }

        _intel.UpdateTree(tree);
    }

    private MapId EnsurePurchasesMap()
    {
        if (_purchasesMap != null)
            return _purchasesMap.Value;

        _map.CreateMap(out var map);
        _purchasesMap = map;
        return map;
    }
}
