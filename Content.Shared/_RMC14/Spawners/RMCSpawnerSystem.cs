using Content.Shared._RMC14.Evacuation;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Spawners;

public sealed class RMCSpawnerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnOnInteractComponent, InteractHandEvent>(OnSpawnOnInteractHand);
    }

    private void OnSpawnOnInteractHand(Entity<SpawnOnInteractComponent> ent, ref InteractHandEvent args)
    {
        if (_net.IsClient)
            return;

        var user = args.User;
        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent))
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, user))
            return;

        if (ent.Comp.RequireEvacuation && !_evacuation.IsEvacuationInProgress())
        {
            // TODO RMC14 code red or above
            _popup.PopupEntity(Loc.GetString("rmc-sentry-not-emergency", ("deployer", ent)), ent, user);
            return;
        }

        var spawned = SpawnAtPosition(ent.Comp.Spawn, ent.Owner.ToCoordinates());
        if (ent.Comp.Popup is { } popup)
            _popup.PopupEntity(Loc.GetString(popup, ("spawned", spawned)), ent, user);

        _audio.PlayPvs(ent.Comp.Sound, spawned);

        QueueDel(ent);
    }
}
