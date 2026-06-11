using Content.Server._RMC14.Announce;
using Content.Server.Ghost.Roles.Events;
using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Banish;

public sealed class XenoServerBanishSystem : EntitySystem
{
    [Dependency] private readonly XenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBanishedEvent>(OnXenoBanished);
        SubscribeLocalEvent<XenoReadmittedEvent>(OnXenoReadmitted);
        SubscribeLocalEvent<GhostRoleMobSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleUsed);
        SubscribeLocalEvent<XenoBanishComponent, ComponentRemove>(OnBanishRemoved);
    }

    private void OnXenoBanished(ref XenoBanishedEvent ev)
    {
        if (!TryComp<XenoBanishComponent>(ev.Banished, out var banishComp) || banishComp.OriginalHive == null)
            return;

        if (!TryComp<HiveComponent>(banishComp.OriginalHive.Value, out var hiveComp))
            return;

        var msg = Loc.GetString("rmc-banish-announcement", ("name", Name(ev.Banished)), ("reason", ev.Reason));
        _announce.AnnounceToHive(ev.Banisher, banishComp.OriginalHive.Value, msg);

        if (TryComp<ActorComponent>(ev.Banished, out var actor))
        {
            var banishedMsg = Loc.GetString("rmc-banish-notification", ("reason", ev.Reason));
            _popup.PopupEntity(banishedMsg, ev.Banished, actor.PlayerSession, PopupType.LargeCaution);
        }
    }

    private void OnXenoReadmitted(ref XenoReadmittedEvent ev)
    {
        if (_hive.GetHive(ev.Readmitted) is not { } hive)
            return;

        var msg = Loc.GetString("rmc-readmit-announcement", ("name", Name(ev.Readmitted)));
        _announce.AnnounceToHive(ev.Readmitter, hive, msg);

        if (TryComp<ActorComponent>(ev.Readmitted, out var actor))
        {
            var readmittedMsg = Loc.GetString("rmc-readmit-notification");
            _popup.PopupEntity(readmittedMsg, ev.Readmitted, actor.PlayerSession, PopupType.Large);
        }
    }

    private void OnGhostRoleUsed(Entity<GhostRoleMobSpawnerComponent> spawner, ref GhostRoleSpawnerUsedEvent args)
    {
        if (!_hive.HasHive(spawner.Owner))
            return;

        var hive = _hive.GetHive(spawner.Owner);
        if (hive == null)
            return;

        if (!TryComp<ActorComponent>(args.Spawned, out var spawnedActor))
            return;

        foreach (var banished in hive.Value.Comp.BanishedXenos)
        {
            if (!TryComp<ActorComponent>(banished, out var banishedActor))
                continue;

            if (banishedActor.PlayerSession.UserId == spawnedActor.PlayerSession.UserId)
            {
                _popup.PopupCursor(Loc.GetString("rmc-banish-ghost-role-blocked"), spawnedActor.PlayerSession, PopupType.LargeCaution);
                _hive.ChangeBurrowedLarva(1);
                QueueDel(args.Spawned);
                return;
            }
        }
    }

    private void OnBanishRemoved(Entity<XenoBanishComponent> ent, ref ComponentRemove args)
    {
        // Removal is handled by XenoBanishSystem.Readmit
    }
}
