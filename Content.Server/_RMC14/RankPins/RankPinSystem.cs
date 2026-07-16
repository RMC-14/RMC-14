using Content.Server.Database;
using Content.Server.Hands.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Medal;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;


namespace Content.Server._RMC14.RankPins;

public sealed class RankPinSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedUniformAccessorySystem _uniformAccessory = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.Profile.PlaytimePerks)
            return;

        if (ev.JobId == null ||
            !_prototype.TryIndex(ev.JobId, out JobPrototype? job))
        {
            return;
        }

        if (!TryComp<RankComponent>(ev.Mob, out RankComponent? rankComp))
        {
            return;
        }

        if (!job.Pins.TryGetValue(rankComp.Rank, out var pinId))
        {
            return;
        }

        if (!pinId)
        {
            return;
        }

        var pin = SpawnAtPosition(pinId, ev.Mob.ToCoordinates());

        // Try to insert into a valid accessory slot. Otherwise, inserts it into the player's hands.
        if (!_uniformAccessory.TryInsertToValidSlot(pin, ev.Mob))
            _hands.TryPickupAnyHand(ev.Mob, pin, false);

        var pinComp = EnsureComp<UniformAccessoryComponent>(pin);
        pinComp.User = GetNetEntity(ev.Mob);
        Dirty(pin, pinComp);
    }
}
