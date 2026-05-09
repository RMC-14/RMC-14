using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem
{
    private static readonly string SimpleHostileFaction = "SimpleHostile";
    private readonly HashSet<EntityUid> _pendingMendozaDeathAftermath = new();

    private static readonly (TimeSpan Delay, SoundSpecifier Sound)[] MendozaDeathSounds =
    [
        (TimeSpan.FromSeconds(0.5), new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_growl1.ogg")),
        (TimeSpan.FromSeconds(1), new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/male_scream_2.ogg")),
        (TimeSpan.FromSeconds(2), new SoundCollectionSpecifier("RCMXenoClaw")),
        (TimeSpan.FromSeconds(2.5), new SoundCollectionSpecifier("RCMXenoClaw")),
        (TimeSpan.FromSeconds(3), new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/male_scream_3.ogg")),
        (TimeSpan.FromSeconds(4), new SoundCollectionSpecifier("RMCTacticalShotgunShoot")),
        (TimeSpan.FromSeconds(5), new SoundCollectionSpecifier("RMCTacticalShotgunShoot")),
        (TimeSpan.FromSeconds(6), new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/gun_m1911.ogg")),
        (TimeSpan.FromSeconds(6.5), new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/gun_m1911.ogg")),
        (TimeSpan.FromSeconds(7), new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/gun_m1911.ogg")),
        (TimeSpan.FromSeconds(7.5), new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/gun_m1911.ogg")),
        (TimeSpan.FromSeconds(8.5), new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/male_scream_4.ogg")),
        (TimeSpan.FromSeconds(9), new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/male_scream_5.ogg")),
        (TimeSpan.FromSeconds(10), new SoundCollectionSpecifier("XenoBite")),
        (TimeSpan.FromSeconds(11), new SoundPathSpecifier("/Audio/_RMC14/Handling/click_2.ogg")),
    ];

    private static readonly TimeSpan MendozaDeathAftermathDelay = TimeSpan.FromSeconds(11.25);

    private const string MendozaRemainsEntity = "RMCRemains";

    private static readonly string[] MendozaHumanBloodAftermath =
    [
        "RMCBlackMarketMendozaHumanBlood1",
        "RMCBlackMarketMendozaHumanBlood2",
        "RMCBlackMarketMendozaHumanBlood3",
        "RMCBlackMarketMendozaHumanBlood4",
        "RMCBlackMarketMendozaHumanBlood5",
        "RMCBlackMarketMendozaHumanBlood6",
        "RMCBlackMarketMendozaHumanBlood7",
    ];

    private static readonly string[] MendozaXenoBloodAftermath =
    [
        "RMCBlackMarketMendozaXenoBlood1",
        "RMCBlackMarketMendozaXenoBlood2",
        "RMCBlackMarketMendozaXenoBlood3",
        "RMCBlackMarketMendozaXenoBlood4",
        "RMCBlackMarketMendozaXenoBlood5",
        "RMCBlackMarketMendozaXenoBlood6",
        "RMCBlackMarketMendozaXenoBlood7",
    ];

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    private void InitializeBlackMarket()
    {
        SubscribeLocalEvent<RequisitionsComputerComponent, InteractUsingEvent>(OnBlackMarketInteractUsing);
        SubscribeLocalEvent<RequisitionsComputerComponent, RMCBlackMarketConsoleHackProbeDoAfterEvent>(OnBlackMarketHackProbe);
        SubscribeLocalEvent<RequisitionsComputerComponent, RMCBlackMarketConsoleHackTuneDoAfterEvent>(OnBlackMarketHackTune);
        SubscribeLocalEvent<RequisitionsComputerComponent, RMCBlackMarketTradebandDoAfterEvent>(OnBlackMarketTradebandDoAfter);
        SubscribeLocalEvent<RMCBlackMarketScannerComponent, AfterInteractEvent>(OnBlackMarketScannerAfterInteract);
    }

    private void OnBuyBlackMarketCart(Entity<RequisitionsComputerComponent> computer, ref RequisitionsBuyBlackMarketCartMsg args)
    {
        TryBuyBlackMarketCart(computer, args.Actor, args.Items);
    }

    private void TryBuyBlackMarketCart(Entity<RequisitionsComputerComponent> computer, EntityUid actor, List<RequisitionsCartItem> items)
    {
        if (items.Count == 0 ||
            !computer.Comp.BlackMarketUnlocked)
        {
            return;
        }

        if (GetElevator(computer) is not { } elevator)
            return;

        var remainingCapacity = GetElevatorCapacity(elevator) - elevator.Comp.Orders.Count;
        if (remainingCapacity <= 0)
            return;

        if (computer.Comp.Account is not { } accountUid ||
            !TryComp(accountUid, out RequisitionsAccountComponent? account) ||
            !CanUseBlackMarket(account))
        {
            return;
        }

        var orders = new List<RequisitionsEntry>();
        var totalCost = 0;
        var totalBlackMarketCost = 0;
        var totalAmount = 0;

        foreach (var item in items)
        {
            if (item.Amount <= 0)
                return;

            if (item.Category < 0 || item.Category >= computer.Comp.BlackMarketCategories.Count)
            {
                Log.Error($"Player {ToPrettyString(actor)} tried to buy out of bounds black market order: category {item.Category}");
                return;
            }

            var category = computer.Comp.BlackMarketCategories[item.Category];
            if (item.Order < 0 || item.Order >= category.Entries.Count)
            {
                Log.Error($"Player {ToPrettyString(actor)} tried to buy out of bounds black market order: category {item.Category}, order {item.Order}");
                return;
            }

            var order = category.Entries[item.Order];
            if (!order.BlackMarket)
            {
                Log.Error($"Player {ToPrettyString(actor)} tried to buy a non-black-market order from the black market catalog: category {item.Category}, order {item.Order}");
                return;
            }

            if (order.Cost > 0 && item.Amount > (int.MaxValue - totalCost) / order.Cost)
                return;

            if (order.BlackMarketCost > 0 && item.Amount > (int.MaxValue - totalBlackMarketCost) / order.BlackMarketCost)
                return;

            if (item.Amount > int.MaxValue - totalAmount)
                return;

            totalCost += order.Cost * item.Amount;
            totalBlackMarketCost += order.BlackMarketCost * item.Amount;
            totalAmount += item.Amount;

            if (totalAmount > remainingCapacity)
                return;

            for (var i = 0; i < item.Amount; i++)
            {
                orders.Add(order);
            }
        }

        if (account.Balance < totalCost ||
            account.BlackMarketBalance < totalBlackMarketCost)
        {
            return;
        }

        account.Balance -= totalCost;
        account.BlackMarketBalance -= totalBlackMarketCost;

        var oldHeat = account.BlackMarketHeat;
        foreach (var order in orders)
        {
            AddBlackMarketHeat(account, order.BlackMarketHeat);
        }

        elevator.Comp.Orders.AddRange(orders);
        Dirty(accountUid, account);
        Dirty(elevator);

        if (!account.BlackMarketHeatDisabled &&
            oldHeat < 100 &&
            account.BlackMarketHeat >= 100)
        {
            BlackMarketInvestigation((accountUid, account), actor);
        }

        SendUIStateAll();
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(actor):actor} bought {totalAmount} black market crate(s) for WY${totalBlackMarketCost} and ${totalCost}");
    }

    private void AddBlackMarketHeat(RequisitionsAccountComponent account, int heat)
    {
        if (heat == 0 ||
            account.BlackMarketHeatDisabled)
            return;

        var adjusted = (int) MathF.Round(heat * _random.NextFloat(0.75f, 1.25f));
        if (adjusted == 0)
            adjusted = Math.Sign(heat);

        account.BlackMarketHeat = Math.Clamp(account.BlackMarketHeat + adjusted, 0, 100);
    }

    private void BlackMarketInvestigation(Entity<RequisitionsAccountComponent> account, EntityUid actor)
    {
        var heat = account.Comp.BlackMarketHeat;
        account.Comp.BlackMarketHeatDisabled = true;
        Dirty(account);

        var ev = new RMCBlackMarketInvestigationEvent(actor, heat);
        RaiseLocalEvent(account.Owner, ev);
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(actor):actor} pushed black market heat to {heat}/100. Investigation hook fired and further heat changes were disabled.");
    }

    private void OnBlackMarketInteractUsing(Entity<RequisitionsComputerComponent> computer, ref InteractUsingEvent args)
    {
        if (HasComp<CashComponent>(args.Used))
        {
            args.Handled = true;
            TryInsertBlackMarketCash(computer, args.User, args.Used);
            return;
        }

        if (TryComp(args.Used, out RMCBlackMarketTradebandDeviceComponent? tradeband))
        {
            args.Handled = true;

            if (!_skills.HasSkill(args.User, tradeband.Skill, tradeband.SkillLevel))
            {
                _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-no-skill", ("tool", args.Used)), computer, args.User, PopupType.SmallCaution);
                return;
            }

            if (IsBlackMarketTradebandLockedOut(computer))
            {
                _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-already-reset", ("target", computer.Owner)), computer, args.User);
                return;
            }

            var tradebandDoAfter = new DoAfterArgs(EntityManager, args.User, tradeband.Delay, new RMCBlackMarketTradebandDoAfterEvent(), computer, computer, args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };
            if (!_doAfter.TryStartDoAfter(tradebandDoAfter))
            {
                _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-busy"), computer, args.User, PopupType.SmallCaution);
                return;
            }

            _audio.PlayPvs(tradeband.StartSound, args.Used);
            SetBlackMarketTradebandActive(args.Used, true);
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-start-others", ("user", args.User), ("tool", args.Used), ("target", computer.Owner)), computer, Filter.PvsExcept(args.User), true, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-start", ("target", computer.Owner)), computer, args.User);
            return;
        }

        if (!TryComp(args.Used, out RMCBlackMarketHackingDeviceComponent? device))
            return;

        args.Handled = true;

        if (IsBlackMarketHackUnavailable(computer, args.User))
            return;

        // CMSS13 hacks the ASRS circuit board; RMC14 keeps the black market state on the ASRS console.
        var doAfter = CreateBlackMarketHackDoAfter(args.User, device.ProbeDelay, new RMCBlackMarketConsoleHackProbeDoAfterEvent(), computer, args.Used);
        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-hack-start", ("target", computer.Owner)), computer, args.User);
    }

    private void TryInsertBlackMarketCash(Entity<RequisitionsComputerComponent> computer, EntityUid user, EntityUid cash)
    {
        if (!computer.Comp.BlackMarketUnlocked ||
            computer.Comp.Account is not { } accountUid ||
            !TryComp(accountUid, out RequisitionsAccountComponent? account) ||
            !CanUseBlackMarket(account))
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-cash-blocked", ("cash", cash)), computer, user);
            return;
        }

        if (HasComp<RMCBlackMarketCounterfeitCashComponent>(cash))
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-cash-counterfeit", ("cash", cash)), computer, user);
            return;
        }

        var price = (int) _pricing.GetPrice(cash);
        if (price <= 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-cash-blocked", ("cash", cash)), computer, user);
            return;
        }

        account.BlackMarketBalance = Math.Min(int.MaxValue, account.BlackMarketBalance + price);
        Dirty(accountUid, account);

        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(user):user} fed {ToPrettyString(cash):cash} into the black market for WY${price}");
        QueueDel(cash);
        SendUIStateAll();
        _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-cash-insert", ("cash", cash)), computer, user);
    }

    private void OnBlackMarketHackProbe(Entity<RequisitionsComputerComponent> computer, ref RMCBlackMarketConsoleHackProbeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } used ||
            !TryComp(used, out RMCBlackMarketHackingDeviceComponent? device) ||
            IsBlackMarketHackUnavailable(computer, args.User))
        {
            return;
        }

        if (!_skills.HasSkill(args.User, device.Skill, device.SkillLevel))
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-hack-no-skill"), computer, args.User, PopupType.SmallCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-hack-bus"), computer, args.User);

        var doAfter = CreateBlackMarketHackDoAfter(args.User, device.TuneDelay, new RMCBlackMarketConsoleHackTuneDoAfterEvent(), computer, used);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private DoAfterArgs CreateBlackMarketHackDoAfter(EntityUid user, TimeSpan delay, DoAfterEvent ev, EntityUid computer, EntityUid used)
    {
        return new DoAfterArgs(EntityManager, user, delay, ev, computer, computer, used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
    }

    private void OnBlackMarketHackTune(Entity<RequisitionsComputerComponent> computer, ref RMCBlackMarketConsoleHackTuneDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } used ||
            !HasComp<RMCBlackMarketHackingDeviceComponent>(used) ||
            IsBlackMarketHackUnavailable(computer, args.User))
        {
            return;
        }

        computer.Comp.BlackMarketUnlocked = !computer.Comp.BlackMarketUnlocked;
        Dirty(computer);
        SendUIState(computer);

        var message = computer.Comp.BlackMarketUnlocked
            ? Loc.GetString("rmc-requisitions-black-market-hack-enable", ("tool", used))
            : Loc.GetString("rmc-requisitions-black-market-hack-disable", ("tool", used));
        _popup.PopupEntity(message, computer, args.User);
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(args.User):user} toggled the black market tradeband on {ToPrettyString(computer):console} to {computer.Comp.BlackMarketUnlocked}");
    }

    private void OnBlackMarketTradebandDoAfter(Entity<RequisitionsComputerComponent> computer, ref RMCBlackMarketTradebandDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } used ||
            !TryComp(used, out RMCBlackMarketTradebandDeviceComponent? tradeband))
        {
            return;
        }

        SetBlackMarketTradebandActive(used, false);

        if (args.Cancelled)
            return;

        if (!_skills.HasSkill(args.User, tradeband.Skill, tradeband.SkillLevel))
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-no-skill", ("tool", used)), computer, args.User, PopupType.SmallCaution);
            return;
        }

        if (IsBlackMarketTradebandLockedOut(computer))
        {
            _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-already-reset", ("target", computer.Owner)), computer, args.User);
            return;
        }

        // tradeband reset locks the board and clears its contraband tuning.
        var accountUid = computer.Comp.Account;
        if (accountUid is { } lockedAccount &&
            TryComp(lockedAccount, out RequisitionsAccountComponent? account))
        {
            account.BlackMarketLockedOut = true;
            Dirty(lockedAccount, account);
        }

        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out var uid, out var requisitions))
        {
            if (accountUid is { } linkedAccount)
            {
                if (requisitions.Account != linkedAccount)
                    continue;
            }
            else if (uid != computer.Owner)
            {
                continue;
            }

            requisitions.BlackMarketUnlocked = false;
            Dirty(uid, requisitions);
        }

        _audio.PlayPvs(tradeband.FinishSound, used);
        SendUIStateAll();
        _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-tradeband-finish", ("target", computer.Owner)), computer, args.User);
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(args.User):user} reset and locked the black market tradeband on {ToPrettyString(computer):console}");
    }

    private bool IsBlackMarketHackUnavailable(Entity<RequisitionsComputerComponent> computer, EntityUid user)
    {
        if (computer.Comp.Account is not { } accountUid ||
            !TryComp(accountUid, out RequisitionsAccountComponent? account) ||
            CanUseBlackMarket(account))
        {
            return false;
        }

        _popup.PopupEntity(Loc.GetString("rmc-requisitions-black-market-hack-unavailable"), computer, user, PopupType.SmallCaution);
        return true;
    }

    private bool IsBlackMarketTradebandLockedOut(Entity<RequisitionsComputerComponent> computer)
    {
        return computer.Comp.Account is { } accountUid &&
            TryComp(accountUid, out RequisitionsAccountComponent? account) &&
            account.BlackMarketLockedOut;
    }

    private void SetBlackMarketTradebandActive(EntityUid device, bool active)
    {
        _appearance.SetData(device, RMCBlackMarketTradebandVisuals.Active, active);
    }

    private void OnBlackMarketScannerAfterInteract(Entity<RMCBlackMarketScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var account = GetAccount();
        var sale = GetBlackMarketSaleValue(args.Target.Value, account.Comp, false);
        var visualValue = sale.KillsMendoza ? 0 : sale.Value;
        SetBlackMarketScannerVisual(scanner, true, GetBlackMarketScannerValueVisual(visualValue));
        _audio.PlayPvs(scanner.Comp.ScanSound, scanner);

        var msg = sale.KillsMendoza
            ? Loc.GetString("rmc-requisitions-black-market-scan-danger")
            : sale.Value > 0
                ? Loc.GetString("rmc-requisitions-black-market-scan-value", ("value", sale.Value))
                : Loc.GetString("rmc-requisitions-black-market-scan-no-value");

        _popup.PopupEntity(msg, scanner, args.User, PopupType.Medium);
        args.Handled = true;
    }

    private void UpdateBlackMarketScanners(TimeSpan time)
    {
        var query = EntityQueryEnumerator<RMCBlackMarketScannerComponent>();
        while (query.MoveNext(out var uid, out var scanner))
        {
            if (!scanner.Scanning || time < scanner.ScanEndsAt)
                continue;

            SetBlackMarketScannerVisual((uid, scanner), false);
        }
    }

    private void SetBlackMarketScannerVisual(
        Entity<RMCBlackMarketScannerComponent> scanner,
        bool scanning,
        RMCBlackMarketScannerValueVisual value = RMCBlackMarketScannerValueVisual.Red)
    {
        scanner.Comp.Scanning = scanning;
        scanner.Comp.ScanEndsAt = scanning
            ? _timing.CurTime + scanner.Comp.ScanDuration
            : TimeSpan.Zero;

        _appearance.SetData(scanner, RMCBlackMarketScannerVisuals.Value, value);
        _appearance.SetData(scanner, RMCBlackMarketScannerVisuals.Scanning, scanning);
    }

    private static RMCBlackMarketScannerValueVisual GetBlackMarketScannerValueVisual(int value)
    {
        if (value <= 0)
            return RMCBlackMarketScannerValueVisual.Red;

        if (value <= 15)
            return RMCBlackMarketScannerValueVisual.Orange;

        if (value <= 20)
            return RMCBlackMarketScannerValueVisual.Yellow;

        if (value <= 30)
            return RMCBlackMarketScannerValueVisual.Green;

        if (value <= 49)
            return RMCBlackMarketScannerValueVisual.Cyan;

        return RMCBlackMarketScannerValueVisual.White;
    }

    private bool IsBlackMarketEnabled(Entity<RequisitionsAccountComponent> account)
    {
        return CanUseBlackMarket(account.Comp) && HasBlackMarketTradebandEnabled(account);
    }

    private bool HasBlackMarketTradebandEnabled(Entity<RequisitionsAccountComponent> account)
    {
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out _, out var computer))
        {
            if (computer.Account == account.Owner &&
                computer.BlackMarketUnlocked)
            {
                return true;
            }
        }

        return false;
    }

    private RequisitionsEntry GetBlackMarketDeliveryOrder(RequisitionsEntry order)
    {
        if (!order.BlackMarket ||
            _random.NextFloat() >= 0.05f)
        {
            return order;
        }

        var entries = new List<RequisitionsEntry>();
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out _, out var computer))
        {
            foreach (var category in computer.BlackMarketCategories)
            {
                foreach (var entry in category.Entries)
                {
                    if (entry.BlackMarket)
                        entries.Add(entry);
                }
            }
        }

        if (entries.Count == 0)
            return order;

        var replacement = entries[_random.Next(entries.Count)];
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"Mendoza loaded the wrong black market order. Expected {order.Crate}, delivered {replacement.Crate}.");
        return replacement;
    }

    private void MarkBlackMarketUnsellableRecursive(EntityUid uid)
    {
        EnsureComp<BlackMarketUnsellableComponent>(uid);

        if (!TryComp(uid, out ContainerManagerComponent? container))
            return;

        foreach (var contained in container.Containers.Values)
        {
            foreach (var entity in contained.ContainedEntities)
            {
                MarkBlackMarketUnsellableRecursive(entity);
            }
        }
    }

    private BlackMarketSaleResult GetBlackMarketSaleValue(EntityUid uid, RequisitionsAccountComponent account, bool recordSale)
    {
        return GetBlackMarketSaleValue(uid, account, recordSale, new HashSet<EntityUid>());
    }

    private BlackMarketSaleResult GetBlackMarketSaleValue(EntityUid uid, RequisitionsAccountComponent account, bool recordSale, HashSet<EntityUid> visited)
    {
        if (!visited.Add(uid) ||
            HasComp<BlackMarketUnsellableComponent>(uid))
        {
            return default;
        }

        var value = 0;
        var killsMendoza = false;

        if (TryComp(uid, out BlackMarketValueComponent? blackMarketValue))
        {
            value += GetOwnBlackMarketValue(uid, account, blackMarketValue, recordSale, out var kills);
            killsMendoza |= kills;
        }
        else
        {
            var hostileSale = GetHostileBlackMarketSaleValue(uid, account, recordSale);
            value += hostileSale.Value;
            killsMendoza |= hostileSale.KillsMendoza;
        }

        if (TryComp(uid, out ContainerManagerComponent? container))
        {
            foreach (var contained in container.Containers.Values)
            {
                foreach (var entity in contained.ContainedEntities)
                {
                    var result = GetBlackMarketSaleValue(entity, account, recordSale, visited);
                    value += result.Value;
                    killsMendoza |= result.KillsMendoza;
                }
            }
        }

        return new BlackMarketSaleResult(value, killsMendoza);
    }

    private int GetOwnBlackMarketValue(
        EntityUid uid,
        RequisitionsAccountComponent account,
        BlackMarketValueComponent blackMarketValue,
        bool recordSale,
        out bool killsMendoza)
    {
        killsMendoza = false;
        if (blackMarketValue.KillsMendozaWhenSoldAlive &&
            TryComp(uid, out MobStateComponent? mobState) &&
            mobState.CurrentState is MobState.Alive or MobState.Critical)
        {
            killsMendoza = true;
            return 0;
        }

        var value = blackMarketValue.Value;
        if (blackMarketValue.DeadValue != null &&
            TryComp(uid, out mobState) &&
            mobState.CurrentState == MobState.Dead)
        {
            value = blackMarketValue.DeadValue.Value;
        }

        if (value <= 0)
            return 0;

        if (blackMarketValue.UseStackCount &&
            TryComp(uid, out StackComponent? stack))
        {
            if (stack.Count > 1 && value > int.MaxValue / stack.Count)
                value = int.MaxValue;
            else
                value *= Math.Max(1, stack.Count);
        }

        return AdjustBlackMarketSaleValue(uid, account, value, recordSale);
    }

    private BlackMarketSaleResult GetHostileBlackMarketSaleValue(EntityUid uid, RequisitionsAccountComponent account, bool recordSale)
    {
        if (!TryComp(uid, out NpcFactionMemberComponent? faction) ||
            !_npcFaction.IsMember((uid, faction), SimpleHostileFaction) ||
            !TryComp(uid, out MobStateComponent? mobState))
        {
            return default;
        }

        if (mobState.CurrentState != MobState.Dead)
            return new BlackMarketSaleResult(0, true);

        return new BlackMarketSaleResult(AdjustBlackMarketSaleValue(uid, account, 25, recordSale), false);
    }

    private int AdjustBlackMarketSaleValue(EntityUid uid, RequisitionsAccountComponent account, int value, bool recordSale)
    {
        if (value <= 0)
            return 0;

        var metadata = MetaData(uid);
        var key = metadata.EntityPrototype?.ID ?? metadata.EntityName;
        var sold = account.BlackMarketSoldItems.GetValueOrDefault(key);
        var modifier = Math.Max(0, 1 - sold * 0.5f);
        var adjusted = (int) MathF.Round(value * modifier);

        if (recordSale)
            account.BlackMarketSoldItems[key] = sold + 1;

        return adjusted;
    }

    private void KillMendoza(Entity<RequisitionsAccountComponent> account, EntityUid sold, Entity<RequisitionsElevatorComponent> elevator)
    {
        if (account.Comp.BlackMarketMendozaDead)
            return;

        account.Comp.BlackMarketMendozaDead = true;
        Dirty(account);

        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(sold):entity} was sold alive through the black market and Mendoza was killed.");
        PlayMendozaDeathSounds(GetBlackMarketFeedbackSource(account, sold));
        Timer.Spawn(MendozaDeathAftermathDelay, () =>
        {
            if (TerminatingOrDeleted(elevator.Owner) ||
                !TryComp(elevator.Owner, out RequisitionsElevatorComponent? elevatorComp))
            {
                return;
            }

            QueueMendozaDeathAftermath((elevator.Owner, elevatorComp));
        });
        SendUIFeedback(Loc.GetString("rmc-requisitions-black-market-mendoza-dead-message"));
    }

    private void QueueMendozaDeathAftermath(Entity<RequisitionsElevatorComponent> elevator)
    {
        if (elevator.Comp.Mode == RequisitionsElevatorMode.Raised &&
            elevator.Comp.ToggledAt == null)
        {
            SpawnMendozaDeathAftermath(elevator);
            return;
        }

        _pendingMendozaDeathAftermath.Add(elevator.Owner);
    }

    private void SpawnPendingMendozaDeathAftermath(Entity<RequisitionsElevatorComponent> elevator, EntityCoordinates coordinates)
    {
        if (!_pendingMendozaDeathAftermath.Remove(elevator.Owner))
            return;

        SpawnMendozaDeathAftermath(elevator, coordinates);
    }

    private void SpawnMendozaDeathAftermath(Entity<RequisitionsElevatorComponent> elevator)
    {
        SpawnMendozaDeathAftermath(elevator, _transform.GetMoverCoordinates(elevator));
    }

    private void SpawnMendozaDeathAftermath(Entity<RequisitionsElevatorComponent> elevator, EntityCoordinates coordinates)
    {
        var positions = GetElevatorAftermathCoordinates(elevator, coordinates);
        if (positions.Count == 0)
            return;

        var chosen = PickMendozaAftermathCoordinate(positions);
        AddMendozaDeathEntity(MendozaRemainsEntity, chosen);
        AddMendozaDeathBlood(MendozaHumanBloodAftermath, chosen);

        for (var i = 0; i < 2; i++)
            AddMendozaDeathBlood(MendozaHumanBloodAftermath, PickMendozaAftermathCoordinate(positions));

        AddMendozaDeathBlood(MendozaXenoBloodAftermath, PickMendozaAftermathCoordinate(positions));
    }

    private List<EntityCoordinates> GetElevatorAftermathCoordinates(Entity<RequisitionsElevatorComponent> elevator, EntityCoordinates coordinates)
    {
        var radius = (int) MathF.Floor(elevator.Comp.Radius);
        var result = new List<EntityCoordinates>();

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                result.Add(coordinates.Offset(new Vector2(x, y)));
            }
        }

        return result;
    }

    private EntityCoordinates PickMendozaAftermathCoordinate(List<EntityCoordinates> coordinates)
    {
        return coordinates[_random.Next(coordinates.Count)];
    }

    private void AddMendozaDeathBlood(string[] options, EntityCoordinates coordinate)
    {
        AddMendozaDeathEntity(options[_random.Next(options.Length)], coordinate);
    }

    private void AddMendozaDeathEntity(string id, EntityCoordinates coordinate)
    {
        var ent = SpawnAtPosition(id, coordinate);
        _transform.SetLocalRotation(ent, _random.NextAngle());
    }

    private EntityUid GetBlackMarketFeedbackSource(Entity<RequisitionsAccountComponent> account, EntityUid fallback)
    {
        EntityUid? first = null;
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out var uid, out var computer))
        {
            if (computer.Account != account.Owner)
                continue;

            if (computer.IsLastInteracted)
                return uid;

            first ??= uid;
        }

        return first ?? fallback;
    }

    private void PlayMendozaDeathSounds(EntityUid source)
    {
        foreach (var (delay, sound) in MendozaDeathSounds)
        {
            Timer.Spawn(delay, () =>
            {
                if (TerminatingOrDeleted(source))
                    return;

                _audio.PlayPvs(sound, source);
            });
        }
    }

    private readonly record struct BlackMarketSaleResult(int Value, bool KillsMendoza);
}
