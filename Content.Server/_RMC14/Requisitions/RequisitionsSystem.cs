using System.Numerics;
using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chasm;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsElevatorMode;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem : SharedRequisitionsSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChasmSystem _chasm = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private static readonly EntProtoId AccountId = "RMCASRSAccount";
    private static readonly EntProtoId PaperRequisitionInvoice = "RMCPaperRequisitionInvoice";
    private static readonly EntProtoId<IFFFactionComponent> MarineFaction = "FactionMarine";

    private EntityQuery<ChasmComponent> _chasmQuery;
    private EntityQuery<ChasmFallingComponent> _chasmFallingQuery;
    private int _gain;
    private int _freeCratesXenoDivider;

    private readonly HashSet<Entity<MobStateComponent>> _toPit = new();

    public override void Initialize()
    {
        base.Initialize();

        _chasmQuery = GetEntityQuery<ChasmComponent>();
        _chasmFallingQuery = GetEntityQuery<ChasmFallingComponent>();

        SubscribeLocalEvent<RequisitionsComputerComponent, MapInitEvent>(OnComputerMapInit);
        SubscribeLocalEvent<RequisitionsComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeActivatableUIOpen);

        Subs.BuiEvents<RequisitionsComputerComponent>(RequisitionsUIKey.Key, subs =>
        {
            subs.Event<RequisitionsBuyMsg>(OnBuy);
            subs.Event<RequisitionsPlatformMsg>(OnPlatform);
        });

        Subs.CVar(_config, RMCCVars.RMCRequisitionsBalanceGain, v => _gain = v, true);
        Subs.CVar(_config, RMCCVars.RMCRequisitionsFreeCratesXenoDivider, v => _freeCratesXenoDivider = v, true);
    }

    private void OnComputerMapInit(Entity<RequisitionsComputerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Account = GetAccount();
        Dirty(ent);
    }

    private void OnComputerBeforeActivatableUIOpen(Entity<RequisitionsComputerComponent> computer, ref BeforeActivatableUIOpenEvent args)
    {
        SetUILastInteracted(computer);
        SendUIState(computer);
    }

    private void OnBuy(Entity<RequisitionsComputerComponent> computer, ref RequisitionsBuyMsg args)
    {
        var actor = args.Actor;
        if (args.Category >= computer.Comp.Categories.Count)
        {
            Log.Error($"Player {ToPrettyString(actor)} tried to buy out of bounds requisitions order: category {args.Category}");
            return;
        }

        var category = computer.Comp.Categories[args.Category];
        if (args.Order >= category.Entries.Count)
        {
            Log.Error($"Player {ToPrettyString(actor)} tried to buy out of bounds requisitions order: category {args.Category}");
            return;
        }

        var order = category.Entries[args.Order];
        if (!TryComp(computer.Comp.Account, out RequisitionsAccountComponent? account) ||
            account.Balance < order.Cost)
        {
            return;
        }

        if (GetElevator(computer) is not { } elevator)
            return;

        if (IsFull(elevator))
            return;

        account.Balance -= order.Cost;
        elevator.Comp.Orders.Add(order);
        SendUIStateAll();
        _adminLogs.Add(LogType.RMCRequisitionsBuy, $"{ToPrettyString(args.Actor):actor} bought requisitions crate {order.Name} with crate {order.Crate} for {order.Cost}");
    }

    private void OnPlatform(Entity<RequisitionsComputerComponent> computer, ref RequisitionsPlatformMsg args)
    {
        if (GetElevator(computer) is not { } elevator)
            return;

        var comp = elevator.Comp;
        if (comp.NextMode != null || comp.Busy)
            return;

        if (comp.Mode == Lowering || comp.Mode == Raising)
            return;

        if (args.Raise && comp.Mode == Raised)
            return;

        if (!args.Raise && comp.Mode == Lowered)
            return;

        RequisitionsElevatorMode? nextMode = comp.Mode switch
        {
            Lowered => Raising,
            Raised => Lowering,
            _ => null
        };

        if (nextMode == null)
            return;

        if (nextMode == Lowering)
        {
            foreach (var entity in _physics.GetContactingEntities(elevator))
            {
                if (HasComp<MobStateComponent>(entity))
                    return;
            }
        }

        comp.ToggledAt = _timing.CurTime;
        comp.Busy = true;
        SetMode(elevator, Preparing, nextMode);
        Dirty(elevator);
    }

    private Entity<RequisitionsAccountComponent> GetAccount()
    {
        var query = EntityQueryEnumerator<RequisitionsAccountComponent>();
        while (query.MoveNext(out var uid, out var account))
        {
            return (uid, account);
        }

        var newAccount = Spawn(AccountId, MapCoordinates.Nullspace);
        var newAccountComp = EnsureComp<RequisitionsAccountComponent>(newAccount);

        return (newAccount, newAccountComp);
    }

    private void UpdateRailings(Entity<RequisitionsElevatorComponent> elevator, RequisitionsRailingMode mode)
    {
        var coordinates = _transform.GetMapCoordinates(elevator);
        var railings = _lookup.GetEntitiesInRange<RequisitionsRailingComponent>(coordinates, elevator.Comp.Radius + 5);
        foreach (var railing in railings)
        {
            SetRailingMode(railing, mode);
        }
    }

    private void UpdateGears(Entity<RequisitionsElevatorComponent> elevator, RequisitionsGearMode mode)
    {
        var coordinates = _transform.GetMapCoordinates(elevator);
        var railings = _lookup.GetEntitiesInRange<RequisitionsGearComponent>(coordinates, elevator.Comp.Radius + 5);
        foreach (var railing in railings)
        {
            if (railing.Comp.Mode == mode)
                continue;

            railing.Comp.Mode = mode;
            Dirty(railing);
        }
    }

    private void SendUIFeedback(Entity<RequisitionsComputerComponent> computerEnt, string flavorText)
    {
        if (!TryComp(computerEnt, out RequisitionsComputerComponent? computerComp))
            return;

        _chatSystem.TrySendInGameICMessage(computerEnt,
            flavorText,
            InGameICChatType.Speak,
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: Loc.GetString("requisition-paperwork-receiver-name"));

        _audio.PlayPvs(computerComp.IncomingSurplus, computerEnt);
    }

    private void SendUIFeedback(string flavorText)
    {
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out var uid, out var computer))
        {
            if (computer.IsLastInteracted)
                SendUIFeedback((uid, computer), flavorText);
        }
    }

    private void SetUILastInteracted(Entity<RequisitionsComputerComponent> computerEnt)
    {
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out _, out var otherComputer))
        {
            otherComputer.IsLastInteracted = false;
        }

        if (!TryComp(computerEnt, out RequisitionsComputerComponent? selectedComputer))
            return;

        selectedComputer.IsLastInteracted = true;
    }

    private void TryPlayAudio(Entity<RequisitionsElevatorComponent> elevator)
    {
        var comp = elevator.Comp;
        if (comp.Audio != null)
            return;

        var time = _timing.CurTime;
        if (comp.NextMode == Lowering || comp.Mode == Lowering)
        {
            if (time < comp.ToggledAt + comp.LowerSoundDelay)
                return;

            comp.Audio = _audio.PlayPvs(comp.LoweringSound, elevator)?.Entity;
            return;
        }

        if (comp.NextMode == Raising || comp.Mode == Raising)
        {
            if (time < comp.ToggledAt + comp.RaiseSoundDelay)
                return;

            comp.Audio = _audio.PlayPvs(comp.RaisingSound, elevator)?.Entity;
        }
    }

    private void SetMode(Entity<RequisitionsElevatorComponent> elevator, RequisitionsElevatorMode mode, RequisitionsElevatorMode? nextMode)
    {
        elevator.Comp.Mode = mode;
        elevator.Comp.NextMode = nextMode;
        Dirty(elevator);

        RequisitionsGearMode? gearMode = mode switch
        {
            Lowered or Raised or Preparing => RequisitionsGearMode.Static,
            Lowering or Raising => RequisitionsGearMode.Moving,
            _ => null
        };

        if (gearMode != null)
            UpdateGears(elevator, gearMode.Value);

        RequisitionsRailingMode? railingMode = (mode, nextMode) switch
        {
            (Lowered, _) => RequisitionsRailingMode.Raised,
            (Raised, _) => RequisitionsRailingMode.Lowering,
            (_, Lowering) => RequisitionsRailingMode.Raising,
            _ => null
        };

        if (railingMode != null)
            UpdateRailings(elevator, railingMode.Value);

        SendUIStateAll();
    }

    private void SpawnOrders(Entity<RequisitionsElevatorComponent> elevator)
    {
        var comp = elevator.Comp;
        if (comp.Mode == Raised)
        {
            var coordinates = _transform.GetMoverCoordinates(elevator);
            var xOffset = comp.Radius;
            var yOffset = comp.Radius;
            int remainingDeliveries = GetElevatorCapacity(elevator);
            foreach (var order in comp.Orders)
            {
                var crate = SpawnAtPosition(order.Crate, coordinates.Offset(new Vector2(xOffset, yOffset)));
                remainingDeliveries--;

                foreach (var prototype in order.Entities)
                {
                    var entity = Spawn(prototype, MapCoordinates.Nullspace);
                    _entityStorage.Insert(entity, crate);
                }

                PrintInvoice(crate, coordinates, PaperRequisitionInvoice);

                yOffset--;
                if (yOffset < -comp.Radius)
                {
                    yOffset = comp.Radius;
                    xOffset--;
                }

                if (xOffset < -comp.Radius)
                    xOffset = comp.Radius;
            }

            comp.Orders.Clear();

            var query = EntityQueryEnumerator<RequisitionsCustomDeliveryComponent>();

            while (query.MoveNext(out var entityUid, out _))
            {
                // If elevator is full, abort and break out of the loop. Any remaining custom deliveries will be on
                // the next elevator shipment.
                if (remainingDeliveries <= 0)
                    break;

                // Remove the component so it doesn't get "delivered" again next elevator cycle.
                RemCompDeferred<RequisitionsCustomDeliveryComponent>(entityUid);

                // Teleport to the spot.
                _transform.SetCoordinates(entityUid, coordinates.Offset(new Vector2(xOffset, yOffset)));
                remainingDeliveries--; // Decrement available delivery slots count.

                // Update the next spot to teleport to.
                yOffset--;
                if (yOffset < -comp.Radius)
                {
                    yOffset = comp.Radius;
                    xOffset--;
                }

                if (xOffset < -comp.Radius)
                    xOffset = comp.Radius;
            }
        }
    }

    private bool Sell(Entity<RequisitionsElevatorComponent> elevator)
    {
        var account = GetAccount();
        var entities = _lookup.GetEntitiesIntersecting(elevator);
        var soldAny = false;
        var rewards = 0;
        foreach (var entity in entities)
        {
            if (entity == elevator.Comp.Audio)
                continue;

            if (HasComp<CargoSellBlacklistComponent>(entity))
                continue;

            rewards += SubmitInvoices(entity);

            if (TryComp(entity, out RequisitionsCrateComponent? crate))
            {
                rewards += crate.Reward;
                soldAny = true;
            }

            QueueDel(entity);
        }

        if (rewards > 0)
            SendUIFeedback(Loc.GetString("requisition-paperwork-reward-message", ("amount", rewards)));

        account.Comp.Balance += rewards;

        if (soldAny)
            Dirty(account);

        return soldAny;
    }

    private void GetCrateWeight(Entity<RequisitionsAccountComponent> account, Dictionary<EntProtoId, float> crates, out Entity<RequisitionsComputerComponent> computer)
    {
        // TODO RMC14 price scaling
        computer = default;
        var computers = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (computers.MoveNext(out var uid, out var comp))
        {
            if (comp.Account != account)
                continue;

            computer = (uid, comp);
            foreach (var category in comp.Categories)
            {
                foreach (var entry in category.Entries)
                {
                    if (crates.ContainsKey(entry.Crate))
                        crates[entry.Crate] = 10000f / entry.Cost;
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var updateUI = false;
        var accounts = EntityQueryEnumerator<RequisitionsAccountComponent>();
        while (accounts.MoveNext(out var uid, out var account))
        {
            if (time > account.NextGain)
            {
                account.NextGain = time + account.GainEvery;
                account.Balance += _gain;
                Dirty(uid, account);

                updateUI = true;
            }

            var xenos = _xeno.GetGroundXenosAlive();
            var randomCrates = CollectionsMarshal.AsSpan(account.RandomCrates);
            foreach (ref var pool in randomCrates)
            {
                if (pool.Next == default)
                    pool.Next = time + pool.Every;

                if (pool.Next >= time)
                    continue;

                var crates = Math.Max(0, Math.Sqrt((float) xenos / _freeCratesXenoDivider));

                if (crates < pool.Minimum && pool.Given < pool.MinimumFor)
                    crates = pool.Minimum;

                pool.Next = time + pool.Every;
                pool.Given++;
                pool.Fraction = crates - (int) crates;

                if (pool.Fraction >= 1)
                {
                    var add = (int) pool.Fraction;
                    pool.Fraction = pool.Fraction - add;
                    crates += add;
                }

                if (crates < 1)
                    continue;

                var crateCosts = new Dictionary<EntProtoId, float>();
                foreach (var choice in pool.Choices)
                {
                    crateCosts[choice] = 0;
                }

                if (crateCosts.Count == 0)
                    continue;

                GetCrateWeight((uid, account), crateCosts, out var computer);
                if (computer == default)
                    continue;

                if (GetElevator(computer) is not { } elevator)
                    continue;

                for (var i = 0; i < crates; i++)
                {
                    var crate = _random.Pick(crateCosts);
                    elevator.Comp.Orders.Add(new RequisitionsEntry { Crate = crate });
                }
            }
        }

        var elevators = EntityQueryEnumerator<RequisitionsElevatorComponent>();
        while (elevators.MoveNext(out var uid, out var elevator))
        {
            if (ProcessElevator((uid, elevator)))
                updateUI = true;

            if (!_chasmQuery.TryComp(uid, out var chasm))
                continue;

            if (time < elevator.NextChasmCheck)
                continue;

            elevator.NextChasmCheck = time + elevator.ChasmCheckEvery;

            if (_net.IsClient)
                continue;

            if (elevator.Mode != Raised && elevator.Mode != Preparing)
            {
                _toPit.Clear();
                _lookup.GetEntitiesInRange(uid.ToCoordinates(), elevator.Radius + 0.25f, _toPit);

                foreach (var toPit in _toPit)
                {
                    if (_chasmFallingQuery.HasComp(toPit))
                        continue;

                    _chasm.StartFalling(uid, chasm, toPit);
                    _audio.PlayEntity(chasm.FallingSound, toPit, uid);
                }
            }
        }

        if (updateUI)
            SendUIStateAll();
    }

    private bool ProcessElevator(Entity<RequisitionsElevatorComponent> ent)
    {
        var time = _timing.CurTime;
        var elevator = ent.Comp;
        if (time > elevator.ToggledAt + elevator.ToggleDelay)
        {
            elevator.ToggledAt = null;
            elevator.Busy = false;
            Dirty(ent);
            SendUIStateAll();
            return false;
        }

        if (elevator.ToggledAt == null)
            return false;

        TryPlayAudio(ent);

        var delay = elevator.NextMode == Raising ? elevator.RaiseDelay : elevator.LowerDelay;
        if (elevator.Mode == Preparing &&
            elevator.NextMode != null &&
            time > elevator.ToggledAt + delay)
        {
            SetMode(ent, elevator.NextMode.Value, null);
            return false;
        }

        if (elevator.Mode != Lowering && elevator.Mode != Raising)
            return false;

        var startDelay = delay + elevator.NextMode switch
        {
            Lowering => elevator.LowerDelay,
            Raising => elevator.RaiseDelay,
            _ => TimeSpan.Zero,
        };

        var moveDelay = startDelay + elevator.Mode switch
        {
            Lowering => elevator.LowerDelay,
            Raising => elevator.RaiseDelay,
            _ => TimeSpan.Zero,
        };

        if (time > elevator.ToggledAt + moveDelay)
        {
            elevator.Audio = null;

            var mode = elevator.Mode switch
            {
                Raising => Raised,
                Lowering => Lowered,
                _ => elevator.Mode,
            };
            SetMode(ent, mode, elevator.NextMode);

            SpawnOrders(ent);

            return true;
        }

        if (elevator.Mode == Lowering &&
            time > elevator.ToggledAt + delay)
        {
            if (Sell(ent))
                return true;
        }

        return false;
    }
}
