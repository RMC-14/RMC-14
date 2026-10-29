using System.Linq;
using Content.Server.Bed.Cryostorage;
using Content.Server.Hands.Systems;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared._RMC14.Cryostorage;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.UserInterface;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Cryostorage;

/// <summary>
/// RMC requisitions equipment recovery built on top of vanilla cryostorage.
/// Stored bodies remain owned by <see cref="CryostorageSystem"/>; this system only lists and moves existing items.
/// </summary>
public sealed class RMCCryoRecoverySystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly CryostorageSystem _cryostorage = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedWebbingSystem _webbing = default!;

    // Reused during UI builds and bulk recovery to avoid allocating a fresh list for every stored body.
    private readonly List<RecoverableItem> _recoverableItems = new();

    // Prevents the same item from appearing twice if inventory and hand state briefly overlap during storage.
    private readonly HashSet<EntityUid> _seenItems = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCryoRecoveryConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<RMCCryoRecoveryConsoleComponent, RMCCryoRecoveryRecoverItemBuiMsg>(OnRecoverItem);
        SubscribeLocalEvent<RMCCryoRecoveryConsoleComponent, RMCCryoRecoveryRecoverAllBuiMsg>(OnRecoverAll);

        SubscribeLocalEvent<CryostorageContainedComponent, EnteredCryostorageEvent>(OnEnteredCryostorage);
        SubscribeLocalEvent<CryostorageContainedComponent, LeftCryostorageEvent>(OnLeftCryostorage);
    }

    private void OnBeforeUIOpen(Entity<RMCCryoRecoveryConsoleComponent> console, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUI(console);
    }

    private void OnRecoverItem(Entity<RMCCryoRecoveryConsoleComponent> console, ref RMCCryoRecoveryRecoverItemBuiMsg args)
    {
        if (!IsAllowed(args.Actor, console.Owner))
            return;

        // The client sends NetEntity handles from a stale snapshot, so resolve and validate against live cryostorage.
        var player = GetEntity(args.Player);
        var item = GetEntity(args.Item);
        if (!TryFindStoredPlayer(console, player, out var cryostorage))
            return;

        if (TryRecoverItem(console.Owner, args.Actor, player, item, true))
            _cryostorage.RefreshCryostorageUI(cryostorage);

        UpdateAllOpenUIs();
    }

    private void OnRecoverAll(Entity<RMCCryoRecoveryConsoleComponent> console, ref RMCCryoRecoveryRecoverAllBuiMsg args)
    {
        if (!IsAllowed(args.Actor, console.Owner))
            return;

        var player = GetEntity(args.Player);
        if (!TryFindStoredPlayer(console, player, out var cryostorage))
            return;

        _recoverableItems.Clear();
        CollectRecoverableItems(player, _recoverableItems);

        // Bulk recovery drops items at the console. Each item is rechecked so concurrent UI/capsule use cannot duplicate it.
        var recovered = false;
        foreach (var item in _recoverableItems.ToArray())
        {
            recovered |= TryRecoverItem(console.Owner, args.Actor, player, item.Item, false);
        }

        if (recovered)
            _cryostorage.RefreshCryostorageUI(cryostorage);

        UpdateAllOpenUIs();
    }

    private void OnEnteredCryostorage(Entity<CryostorageContainedComponent> ent, ref EnteredCryostorageEvent args)
    {
        if (ent.Comp.Cryostorage is { } cryostorage)
            _cryostorage.RefreshCryostorageUI(cryostorage);

        UpdateAllOpenUIs();
    }

    private void OnLeftCryostorage(Entity<CryostorageContainedComponent> ent, ref LeftCryostorageEvent args)
    {
        UpdateAllOpenUIs();
    }

    private bool IsAllowed(EntityUid actor, EntityUid console)
    {
        if (_accessReader.IsAllowed(actor, console))
            return true;

        _popup.PopupEntity(Loc.GetString("cryostorage-popup-access-denied"), actor, actor);
        return false;
    }

    private void UpdateAllOpenUIs()
    {
        var query = EntityQueryEnumerator<RMCCryoRecoveryConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (_ui.IsUiOpen(uid, RMCCryoRecoveryUiKey.Key))
                UpdateUI((uid, console));
        }
    }

    private void UpdateUI(Entity<RMCCryoRecoveryConsoleComponent> console)
    {
        var players = new List<RMCCryoRecoveryPlayerData>();

        // This is the only full scan. It happens when a console opens or stored items change, not every tick.
        var query = EntityQueryEnumerator<CryostorageComponent>();
        while (query.MoveNext(out var uid, out var cryostorage))
        {
            foreach (var stored in cryostorage.StoredPlayers.ToArray())
            {
                if (!TryFindStoredPlayer(console, stored, out var storedCryostorage) || storedCryostorage != uid)
                    continue;

                _recoverableItems.Clear();
                CollectRecoverableItems(stored, _recoverableItems);
                if (_recoverableItems.Count == 0)
                    continue;

                var items = new List<RMCCryoRecoveryItemData>(_recoverableItems.Count);
                foreach (var recoverable in _recoverableItems)
                {
                    items.Add(new RMCCryoRecoveryItemData(
                        GetNetEntity(recoverable.Item),
                        Name(recoverable.Item),
                        recoverable.Location));
                }

                players.Add(new RMCCryoRecoveryPlayerData(
                    GetNetEntity(stored),
                    Name(stored),
                    GetJobName(stored),
                    items));
            }
        }

        players.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _ui.SetUiState(console.Owner, RMCCryoRecoveryUiKey.Key, new RMCCryoRecoveryBuiState(players));
    }

    private bool TryFindStoredPlayer(
        Entity<RMCCryoRecoveryConsoleComponent> console,
        EntityUid player,
        out EntityUid cryostorage)
    {
        cryostorage = default;

        // Central gate for both UI listing and BUI actions. Anything excluded here is invisible and unrecoverable.
        if (TerminatingOrDeleted(player) ||
            IsExcluded(player, console.Comp) ||
            !TryComp<CryostorageContainedComponent>(player, out var contained) ||
            contained.Cryostorage is not { } storedIn ||
            TerminatingOrDeleted(storedIn) ||
            !TryComp<CryostorageComponent>(storedIn, out var cryostorageComp) ||
            !cryostorageComp.StoredPlayers.Contains(player))
        {
            return false;
        }

        cryostorage = storedIn;
        return true;
    }

    private bool IsExcluded(EntityUid player, RMCCryoRecoveryConsoleComponent console)
    {
        if (!TryComp<OriginalRoleComponent>(player, out var role) ||
            role.Job is not { } job)
        {
            return false;
        }

        if (console.ExcludeWhitelistedRoles &&
            _prototypes.TryIndex(job, out JobPrototype? jobPrototype) &&
            jobPrototype.Whitelisted)
        {
            return true;
        }

        if (console.ExcludedDepartments.Count == 0)
            return false;

        if (!_jobs.TryGetAllDepartments(job, out var departments))
            return false;

        foreach (var department in departments)
        {
            if (console.ExcludedDepartments.Contains(department.ID))
                return true;
        }

        return false;
    }

    private string GetJobName(EntityUid player)
    {
        if (TryComp<OriginalRoleComponent>(player, out var role) &&
            role.Job is { } job &&
            _prototypes.TryIndex(job, out JobPrototype? prototype))
        {
            return prototype.LocalizedName;
        }

        return Loc.GetString("rmc-cryo-recovery-unknown-assignment");
    }

    private void CollectRecoverableItems(EntityUid player, List<RecoverableItem> items)
    {
        _seenItems.Clear();

        var enumerator = _inventory.GetSlotEnumerator(player);
        while (enumerator.NextItem(out var item, out var slotDef))
        {
            var slotName = Loc.GetString(slotDef.DisplayName);
            TryAddRecoverable(item, Loc.GetString("rmc-cryo-recovery-location-slot", ("slot", slotName)), items, _seenItems);
        }

        foreach (var hand in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, hand, out var held))
                continue;

            TryAddRecoverable(held.Value, Loc.GetString("rmc-cryo-recovery-location-hand", ("hand", hand)), items, _seenItems);
        }
    }

    private bool TryAddRecoverable(
        EntityUid item,
        string location,
        List<RecoverableItem> items,
        HashSet<EntityUid> seen)
    {
        // Unavailable markers intentionally hide stock cryo gear without deleting it from the stored body.
        if (TerminatingOrDeleted(item))
        {
            return false;
        }

        if (HasComp<RMCCryoUnavailableOnStoreComponent>(item))
            return TryAddAttachedWebbing(item, location, items, seen);

        if (!seen.Add(item))
            return false;

        items.Add(new RecoverableItem(item, location));
        return true;
    }

    private bool TryAddAttachedWebbing(
        EntityUid clothing,
        string location,
        List<RecoverableItem> items,
        HashSet<EntityUid> seen)
    {
        if (!TryComp<WebbingClothingComponent>(clothing, out var webbingClothing) ||
            !_webbing.HasWebbing((clothing, webbingClothing), out var webbing) ||
            TerminatingOrDeleted(webbing.Owner) ||
            HasComp<RMCCryoUnavailableOnStoreComponent>(webbing.Owner) ||
            !seen.Add(webbing.Owner))
        {
            return false;
        }

        items.Add(new RecoverableItem(
            webbing.Owner,
            Loc.GetString("rmc-cryo-recovery-location-attached-webbing", ("location", location))));
        return true;
    }

    private bool TryGetRecoverableItem(EntityUid player, EntityUid item, out EntityUid? attachedClothing)
    {
        attachedClothing = null;
        if (TerminatingOrDeleted(item) || HasComp<RMCCryoUnavailableOnStoreComponent>(item))
            return false;

        var enumerator = _inventory.GetSlotEnumerator(player);
        while (enumerator.NextItem(out var contained, out _))
        {
            if (contained == item)
                return true;
        }

        foreach (var hand in _hands.EnumerateHands(player))
        {
            if (_hands.TryGetHeldItem(player, hand, out var held) && held == item)
                return true;
        }

        return TryGetAttachedWebbingClothing(player, item, out attachedClothing);
    }

    private bool TryGetAttachedWebbingClothing(EntityUid player, EntityUid webbing, out EntityUid? clothing)
    {
        clothing = null;

        var enumerator = _inventory.GetSlotEnumerator(player);
        while (enumerator.NextItem(out var contained, out _))
        {
            if (IsAttachedWebbingOnHiddenClothing(contained, webbing))
            {
                clothing = contained;
                return true;
            }
        }

        foreach (var hand in _hands.EnumerateHands(player))
        {
            if (!_hands.TryGetHeldItem(player, hand, out var held))
                continue;

            if (IsAttachedWebbingOnHiddenClothing(held.Value, webbing))
            {
                clothing = held.Value;
                return true;
            }
        }

        return false;
    }

    private bool IsAttachedWebbingOnHiddenClothing(EntityUid clothing, EntityUid webbing)
    {
        return HasComp<RMCCryoUnavailableOnStoreComponent>(clothing) &&
               TryComp<WebbingClothingComponent>(clothing, out var webbingClothing) &&
               _webbing.HasWebbing((clothing, webbingClothing), out var attached) &&
               attached.Owner == webbing;
    }

    private bool TryRecoverItem(EntityUid console, EntityUid actor, EntityUid player, EntityUid item, bool tryPickup)
    {
        if (!TryGetRecoverableItem(player, item, out var attachedClothing))
            return false;

        if (attachedClothing is { } clothing)
        {
            if (!TryComp<WebbingClothingComponent>(clothing, out var webbingClothing) ||
                !_webbing.TryDetachWebbing((clothing, webbingClothing), out var detached) ||
                detached.Owner != item)
            {
                return false;
            }
        }
        else
        {
            _container.TryRemoveFromContainer(item);
        }

        _adminLogger.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(actor):player} recovered item {ToPrettyString(item)} from cryostorage-contained player " +
            $"{ToPrettyString(player):player}, using cryo recovery console {ToPrettyString(console)}");

        // Single-item recovery behaves like normal item pickup; bulk recovery uses the console tile for predictability.
        if (tryPickup && HasComp<HandsComponent>(actor))
        {
            _transform.SetCoordinates(item, Transform(actor).Coordinates);
            if (_hands.TryPickupAnyHand(actor, item))
                return true;
        }

        _transform.SetCoordinates(item, Transform(console).Coordinates);
        return true;
    }

    private readonly record struct RecoverableItem(EntityUid Item, string Location);
}
