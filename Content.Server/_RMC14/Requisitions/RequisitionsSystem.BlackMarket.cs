using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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
            account.BlackMarketStatus != RequisitionsBlackMarketStatus.Available)
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

        if (oldHeat < 100 && account.BlackMarketHeat >= 100)
            BlackMarketInvestigation(actor, account.BlackMarketHeat);

        SendUIStateAll();
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(actor):actor} bought {totalAmount} black market crate(s) for WY${totalBlackMarketCost} and ${totalCost}");
    }

    private void AddBlackMarketHeat(RequisitionsAccountComponent account, int heat)
    {
        if (heat <= 0)
            return;

        var adjusted = Math.Max(1, (int) MathF.Round(heat * _random.NextFloat(0.75f, 1.25f)));
        account.BlackMarketHeat = Math.Clamp(account.BlackMarketHeat + adjusted, 0, 100);
    }

    private void BlackMarketInvestigation(EntityUid actor, int heat)
    {
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(actor):actor} pushed black market heat to {heat}/100. Investigation hook fired.");
    }

    private void OnBlackMarketInteractUsing(Entity<RequisitionsComputerComponent> computer, ref InteractUsingEvent args)
    {
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

        // CMSS13 writes this lock to the ASRS board; RMC14 keeps the shared lockout on the requisitions account.
        var accountUid = computer.Comp.Account;
        if (accountUid is { } lockedAccount &&
            TryComp(lockedAccount, out RequisitionsAccountComponent? account))
        {
            account.BlackMarketStatus = RequisitionsBlackMarketStatus.LockedOut;
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
            account.BlackMarketStatus == RequisitionsBlackMarketStatus.Available)
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
            account.BlackMarketStatus == RequisitionsBlackMarketStatus.LockedOut;
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

    private bool IsBlackMarketEnabled(RequisitionsAccountComponent account)
    {
        if (account.BlackMarketStatus != RequisitionsBlackMarketStatus.Available)
            return false;

        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out _, out var computer))
        {
            if (computer.BlackMarketUnlocked)
                return true;
        }

        return false;
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
            mobState.CurrentState == MobState.Alive)
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

        var key = MetaData(uid).EntityPrototype?.ID ?? MetaData(uid).EntityName;
        var sold = account.BlackMarketSoldItems.GetValueOrDefault(key);
        var modifier = Math.Max(0, 1 - sold * 0.5f);
        var adjusted = (int) MathF.Round(value * modifier);

        if (recordSale)
            account.BlackMarketSoldItems[key] = sold + 1;

        return adjusted;
    }

    private void KillMendoza(Entity<RequisitionsAccountComponent> account, EntityUid sold)
    {
        if (account.Comp.BlackMarketStatus == RequisitionsBlackMarketStatus.MendozaDead)
            return;

        account.Comp.BlackMarketStatus = RequisitionsBlackMarketStatus.MendozaDead;
        Dirty(account);

        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(sold):entity} was sold alive through the black market and Mendoza was killed.");
        SendUIFeedback(Loc.GetString("rmc-requisitions-black-market-mendoza-dead-message"));
    }

    private readonly record struct BlackMarketSaleResult(int Value, bool KillsMendoza);
}
