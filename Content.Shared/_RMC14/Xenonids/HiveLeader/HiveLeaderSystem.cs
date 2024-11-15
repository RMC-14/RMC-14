using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.HiveLeader;

public sealed class HiveLeaderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedWatchXenoSystem _watchXeno = default!;

    private EntityQuery<XenoAttachedOvipositorComponent> _attachedOvipositorQuery;
    private EntityQuery<HiveLeaderComponent> _hiveLeaderQuery;
    private EntityQuery<HiveLeaderGranterComponent> _hiveLeaderGranterQuery;
    private EntityQuery<XenoActivePheromonesComponent> _activePheromonesQuery;
    private EntityQuery<XenoPheromonesComponent> _pheromonesQuery;

    public override void Initialize()
    {
        _attachedOvipositorQuery = GetEntityQuery<XenoAttachedOvipositorComponent>();
        _hiveLeaderQuery = GetEntityQuery<HiveLeaderComponent>();
        _hiveLeaderGranterQuery = GetEntityQuery<HiveLeaderGranterComponent>();
        _activePheromonesQuery = GetEntityQuery<XenoActivePheromonesComponent>();
        _pheromonesQuery = GetEntityQuery<XenoPheromonesComponent>();

        SubscribeLocalEvent<NewXenoEvolvedEvent>(OnLeaderNewXenoEvolved);

        SubscribeLocalEvent<HiveLeaderComponent, ComponentRemove>(OnLeaderRemove);
        SubscribeLocalEvent<HiveLeaderComponent, EntityTerminatingEvent>(OnLeaderRemove);
        SubscribeLocalEvent<HiveLeaderComponent, MobStateChangedEvent>(OnLeaderMobStateChanged);

        SubscribeLocalEvent<HiveLeaderGranterComponent, ComponentRemove>(OnGranterRemove);
        SubscribeLocalEvent<HiveLeaderGranterComponent, EntityTerminatingEvent>(OnGranterRemove);
        SubscribeLocalEvent<HiveLeaderGranterComponent, MobStateChangedEvent>(OnGranterMobStateChanged);
        SubscribeLocalEvent<HiveLeaderGranterComponent, HiveLeaderActionEvent>(OnGranterAction);
        SubscribeLocalEvent<HiveLeaderGranterComponent, HiveLeaderWatchEvent>(OnGranterWatch);
        SubscribeLocalEvent<HiveLeaderGranterComponent, XenoPheromonesActivatedEvent>(OnGranterPheromonesActivated);
        SubscribeLocalEvent<HiveLeaderGranterComponent, XenoPheromonesDeactivatedEvent>(OnGranterPheromonesDeactivated);
        SubscribeLocalEvent<HiveLeaderGranterComponent, XenoOvipositorChangedEvent>(OnGranterOvipositorChanged);
    }

    private void OnLeaderRemove<T>(Entity<HiveLeaderComponent> ent, ref T args)
    {
        RemoveLeader(ent);
    }

    private void OnLeaderMobStateChanged(Entity<HiveLeaderComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemoveLeader(ent);
    }

    private void OnLeaderNewXenoEvolved(ref NewXenoEvolvedEvent args)
    {
        if (!_hiveLeaderQuery.TryComp(args.OldXeno, out var oldLeader) ||
            !_hiveLeaderGranterQuery.TryComp(oldLeader.Granter, out var granter) ||
            _hiveLeaderGranterQuery.HasComp(args.NewXeno))
        {
            return;
        }

        var newLeader = EnsureComp<HiveLeaderComponent>(args.NewXeno);
        newLeader.Granter = oldLeader.Granter;
        granter.Leaders.Remove(args.OldXeno);
        granter.Leaders.Add(args.NewXeno);

        SyncPheromones((oldLeader.Granter.Value, granter));
    }

    private void OnGranterRemove<T>(Entity<HiveLeaderGranterComponent> ent, ref T args)
    {
        RemoveLeaders(ent);
    }

    private void OnGranterMobStateChanged(Entity<HiveLeaderGranterComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemoveLeaders(ent);
    }

    private void OnGranterAction(Entity<HiveLeaderGranterComponent> ent, ref HiveLeaderActionEvent args)
    {
        string msg;
        var leaders = ent.Comp.Leaders;
        var max = ent.Comp.MaxLeaders;
        if (!_watchXeno.TryGetWatched(ent.Owner, out var watching))
        {
            if (leaders.Count == 0)
            {
                msg = "There are no Xenonid leaders. Overwatch a Xenonid to make it a leader.";
                _popup.PopupClient(msg, ent, ent, PopupType.MediumCaution);
                return;
            }

            var options = new List<DialogOption>();
            foreach (var leader in leaders)
            {
                options.Add(new DialogOption(Name(leader), new HiveLeaderWatchEvent(GetNetEntity(leader))));
            }

            _dialog.OpenOptions(ent, "Watch with leader?", options, "Target");
            return;
        }

        if (ent.Owner == watching)
            return;

        if (!HasComp<HiveLeaderComponent>(watching) && leaders.Count >= max)
        {
            msg = $"You can't have more than {max} promoted leaders.";
            _popup.PopupClient(msg, ent, PopupType.MediumCaution);
            return;
        }

        if (EnsureComp<HiveLeaderComponent>(watching, out var leaderComp))
        {
            RemCompDeferred<HiveLeaderComponent>(watching);
            ent.Comp.Leaders.Remove(watching);

            msg = $"You've demoted {Name(watching)} from Hive Leader.";
            _popup.PopupClient(msg, ent, PopupType.MediumCaution);

            msg = $"{Name(ent)} has demoted you from Hive Leader. Your leadership rights and abilities have waned.";
            _popup.PopupEntity(msg, watching, watching, PopupType.MediumCaution);
            _rmcChat.ChatMessageToOne(msg, watching);
            var evn = new HiveLeaderStatusChangedEvent(false);
            RaiseLocalEvent(watching, ref evn);
            return;
        }

        leaderComp.Granter = ent;
        Dirty(watching, leaderComp);

        ent.Comp.Leaders.Add(watching);
        Dirty(ent);

        msg = $"You've selected {Name(watching)} as a Hive Leader.";
        _popup.PopupClient(msg, ent, PopupType.Medium);
        msg = $"{Name(ent)} has selected you as a Hive Leader. The other Xenonids must listen to you. You will also act as a beacon for the Queen's pheromones.";
        _popup.PopupClient(msg, watching, watching, PopupType.Medium);
        _rmcChat.ChatMessageToOne(msg, watching);
        var ev = new HiveLeaderStatusChangedEvent(true);
        RaiseLocalEvent(watching, ref ev);
        SyncPheromones(ent);
    }

    private void OnGranterWatch(Entity<HiveLeaderGranterComponent> ent, ref HiveLeaderWatchEvent args)
    {
        if (!TryGetEntity(args.Leader, out var leader) ||
            !_hiveLeaderQuery.HasComp(leader))
        {
            return;
        }

        _watchXeno.Watch(ent.Owner, leader.Value);
    }

    private void OnGranterPheromonesActivated(Entity<HiveLeaderGranterComponent> ent, ref XenoPheromonesActivatedEvent args)
    {
        SyncPheromones(ent);
    }

    private void OnGranterPheromonesDeactivated(Entity<HiveLeaderGranterComponent> ent, ref XenoPheromonesDeactivatedEvent args)
    {
        SyncPheromones(ent, true);
    }

    private void OnGranterOvipositorChanged(Entity<HiveLeaderGranterComponent> ent, ref XenoOvipositorChangedEvent args)
    {
        SyncPheromones(ent);
    }

    private void RemoveLeaders(Entity<HiveLeaderGranterComponent> ent)
    {
        if (_timing.ApplyingState)
            return;

        SyncPheromones(ent, true);

        foreach (var leader in ent.Comp.Leaders)
        {
            RemCompDeferred<HiveLeaderComponent>(leader);
            var ev = new HiveLeaderStatusChangedEvent(false);
            RaiseLocalEvent(leader, ref ev);
        }

        ent.Comp.Leaders.Clear();
    }

    private void SyncPheromones(Entity<HiveLeaderGranterComponent> ent, bool forceDisable = false)
    {
        if (_timing.ApplyingState)
            return;

        if (!_pheromonesQuery.TryComp(ent, out var pheromone))
            return;

        var hasPheromones = _activePheromonesQuery.TryComp(ent, out var active) &&
                            _attachedOvipositorQuery.HasComp(ent) &&
                            !_mobState.IsDead(ent) &&
                            !forceDisable;
        foreach (var leader in ent.Comp.Leaders)
        {
            if (!_hiveLeaderQuery.TryComp(leader, out var leaderComp))
                continue;

            if (!hasPheromones)
            {
                if (!_container.TryGetContainer(leader, leaderComp.PheromonesContainerId, out var container) ||
                    !container.ContainedEntities.TryFirstOrNull(out var first))
                {
                    continue;
                }

                RemComp<XenoActivePheromonesComponent>(first.Value);
                continue;
            }

            var slot = _container.EnsureContainer<ContainerSlot>(leader, leaderComp.PheromonesContainerId);
            EntityUid relayEnt;
            if (slot.ContainedEntity == null)
            {
                if (!TrySpawnInContainer(ent.Comp.PheromoneRelayId,
                        leader,
                        leaderComp.PheromonesContainerId,
                        out var spawnedRelayEnt))
                {
                    continue;
                }

                relayEnt = spawnedRelayEnt.Value;
            }
            else
            {
                relayEnt = slot.ContainedEntity.Value;
            }

            var relayPheromones = EnsureComp<XenoPheromonesComponent>(relayEnt);
            relayPheromones.PheromonesPlasmaCost = 0;
            relayPheromones.PheromonesPlasmaUpkeep = 0;
            relayPheromones.PheromonesRange = pheromone.PheromonesRange;
            relayPheromones.PheromonesMultiplier = pheromone.PheromonesMultiplier;
            Dirty(relayEnt, relayPheromones);

            var relayActive = EnsureComp<XenoActivePheromonesComponent>(relayEnt);
            if (active != null)
                relayActive.Pheromones = active.Pheromones;
        }
    }

    private void RemoveLeader(Entity<HiveLeaderComponent> leader)
    {
        if (_timing.ApplyingState)
            return;

        if (_container.TryGetContainer(leader, leader.Comp.PheromonesContainerId, out var container) &&
            container.ContainedEntities.TryFirstOrNull(out var first))
        {
            RemComp<XenoActivePheromonesComponent>(first.Value);
        }

        RemCompDeferred<HiveLeaderComponent>(leader);

        if (!TryComp(leader.Comp.Granter, out HiveLeaderGranterComponent? granter))
            return;

        granter.Leaders.Remove(leader);
        Dirty(leader.Comp.Granter.Value, granter);
        SyncPheromones((leader.Comp.Granter.Value, granter));
    }
}
